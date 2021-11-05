using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sentry.Helpers;
using Sentry.Models;
using Sentry.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Web.Http;
using System.Threading.Tasks;
using Windows.Storage.Streams;

#if NETFX_CORE
using Windows.UI.Xaml;
#endif

namespace Sentry
{
    public class RavenClient
    {
        private const int _defaultTimeout = 5000;

        private const int _sentryVersion = 4;

        private readonly HttpClient _httpClient;

        private readonly IRavenStorageClient _storage;

        private readonly IPlatformClient _platform;

        private static RavenClient _instance = null;

        public readonly Dsn Dsn;

        protected RavenClient(Dsn dsn, bool captureUnhandled = true)
        {
            Dsn = dsn;

#if NETFX_CORE
            _storage = new RavenStorageClient();
            _platform = new WindowsPlatformClient();
#endif

            _httpClient = BuildHttpClient();

            if (captureUnhandled)
            {
#if NETFX_CORE
                Application.Current.UnhandledException += Application_UnhandledException;
#endif
            }
        }

        #region Public properties and methods

        /// <summary>
        /// Initializes the <see cref="RavenClient"/> class which you can then access through <see cref="Instance"/>. 
        /// Call this in your App() constructor before InitializeComponent()
        /// </summary>
        /// <param name="dsn"></param>
        /// <param name="captureUnhandled"></param>
        public static void InitializeAsync(Dsn dsn, bool captureUnhandled = true)
        {
            if (_instance != null)
                return;

            _instance = new RavenClient(dsn, captureUnhandled);

            _instance.FlushStoredPayloadsAsync().ContinueWith(t => _instance.HandleInternalException(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>
        /// Gets the <see cref="RavenClient"/> instance if it has been initialized.
        /// </summary>
        /// <exception cref="InvalidOperationException">Will be thrown if accessed before <see cref="InitializeAsync(Dsn, bool)"/></exception>
        public static RavenClient Instance
        {
            get
            {
                if (_instance == null)
                    throw new InvalidOperationException("RavenClient must be initialized before the instance is called.");

                return _instance;
            }
        }

        private RavenUser _user { get; set; }
        /// <summary>
        /// Instantiates or updates the <see cref="RavenUser"/> to be sent with every Sentry request.
        /// </summary>
        /// <param name="id">The unique identifier of this user.</param>
        public void SetUser(string id)
        {
            SetUser(id, null, null);
        }

        /// <summary>
        /// Instantiates or updates the <see cref="RavenUser"/> to be sent with every Sentry request.
        /// </summary>
        /// <param name="id">The unique identifier of this user.</param>
        /// <param name="username">The username of the this user.</param>
        public void SetUser(string id, string username)
        {
            SetUser(id, username, null);
        }

        /// <summary>
        /// Instantiates or updates the <see cref="RavenUser"/> to be sent with every Sentry request.
        /// </summary>
        /// <param name="id">The unique identifier of this user.</param>
        /// <param name="username">The username of the this user.</param>
        /// <param name="email">The email address of this user.</param>
        public void SetUser(string id, string username, string email)
        {
            if (_user == null)
                _user = new RavenUser();

            _user.Id = id;
            _user.Username = username;
            _user.Email = email;
        }

        /// <summary>
        /// Gets or sets the logger name to be sent with every Sentry request.
        /// </summary>
        public string Logger { get; set; }

        /// <summary>
        /// Gets or sets the tags that will be sent with every Sentry request.
        /// </summary>
        public Dictionary<string, string> DefaultTags { get; set; }

        /// <summary>
        /// Gets or sets the extra data that will be sent with every Sentry request.
        /// </summary>
        public Dictionary<string, object> DefaultExtra { get; set; }

        /// <summary>
        /// Captures a message.
        /// </summary>
        /// <param name="message">The message text to be logged.</param>
        /// <param name="forceSend">Whether to send the request immediately upon capture.</param>
        public void CaptureMessageAsync(string message, bool forceSend = false)
        {
            ProcessMessageAsync(message, RavenLogLevel.Info, null, null, forceSend).ContinueWith(t => HandleInternalException(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>
        /// Captures a message.
        /// </summary>
        /// <param name="message">The message text to be logged.</param>
        /// <param name="forceSend">Whether to send the request immediately upon capture.</param>
        /// <param name="level">The level this message should be logged at.</param>
        /// <param name="tags">Any additional tags to be sent with this message.</param>
        /// <param name="extra">Any additional extra data to be sent with this message.</param>
        public void CaptureMessageAsync(string message, bool forceSend = false, RavenLogLevel level = RavenLogLevel.Info, IDictionary<string, string> tags = null, IDictionary<string, object> extra = null)
        {
            ProcessMessageAsync(message, level, tags, extra, forceSend).ContinueWith(t => HandleInternalException(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>
        /// Captures an exception.
        /// </summary>
        /// <param name="ex">The <see cref="System.Exception"/> to capture.</param>
        /// <param name="forceSend">Whether to send the request immediately upon capture.</param>
        public void CaptureExceptionAsync(Exception ex, bool forceSend = false)
        {
            ProcessExceptionAsync(ex, RavenLogLevel.Error, null, null, forceSend).ContinueWith(t => HandleInternalException(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>
        /// Captures an exception.
        /// </summary>
        /// <param name="ex">The <see cref="System.Exception"/> to capture.</param>
        /// <param name="forceSend">Whether to send the request immediately upon capture.</param>
        /// <param name="level">The level this exception should be logged at.</param>
        /// <param name="tags">Any additional tags to be sent with this exception.</param>
        /// <param name="extra">Any additional extra data to be sent with this exception.</param>
        public void CaptureExceptionAsync(Exception ex, bool forceSend = false, RavenLogLevel level = RavenLogLevel.Error, IDictionary<string, string> tags = null, IDictionary<string, object> extra = null)
        {
            ProcessExceptionAsync(ex, level, tags, extra, forceSend).ContinueWith(t => HandleInternalException(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>
        /// Call this in the "OnLaunched" and "OnActivated" handlers of App.xaml.cs
        /// to capture unobserved async void errors.
        /// </summary>
        public void RegisterAsyncContextHandler()
        {
            AsyncSynchronizationContext.Register();

            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        #endregion

        #region Helper methods

        private void Application_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            e.Handled = true;

            CaptureExceptionAsync(e.Exception, true, RavenLogLevel.Fatal);
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();

            CaptureExceptionAsync(e.Exception, true, RavenLogLevel.Error);
        }

        internal async Task ProcessMessageAsync(string message, RavenLogLevel level, IDictionary<string, string> tags, IDictionary<string, object> extra, bool forceSend)
        {
            RavenPayload payload = await GeneratePayloadAsync(message, level, tags, extra);

            if (forceSend)
                await SendPayloadAsync(payload);
            else
                await StorePayloadAsync(payload);
        }

        internal async Task ProcessExceptionAsync(Exception ex, RavenLogLevel level, IDictionary<string, string> tags, IDictionary<string, object> extra, bool forceSend)
        {
            RavenPayload payload = await GeneratePayloadAsync(ex, level, tags, extra);

            if (forceSend)
                await SendPayloadAsync(payload);
            else
                await StorePayloadAsync(payload);
        }

        internal async Task FlushStoredPayloadsAsync()
        {
            List<Task> tasks = new List<Task>();

            var payloads = await _storage.ListStoredExceptionsAsync();
            foreach (var p in payloads)
                tasks.Add(SendPayloadAsync(p));

            foreach (var t in tasks)
                await t;
        }

        internal async Task<RavenPayload> GeneratePayloadAsync(string message, RavenLogLevel level, IDictionary<string, string> tags, IDictionary<string, object> extra)
        {
            RavenPayload payload = await GetBasePayloadAsync(level, tags, extra);
            payload.Message = message;

            return payload;
        }

        internal async Task<RavenPayload> GeneratePayloadAsync(Exception ex, RavenLogLevel level, IDictionary<string, string> tags, IDictionary<string, object> extra)
        {
            string exceptionName = ex.GetBaseException().GetType().FullName;
            string exceptionMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;

            RavenPayload payload = await GetBasePayloadAsync(level, tags, extra);
            payload.Message = String.Format("{0}: {1}", exceptionName, exceptionMessage);
            payload.Exceptions = ex.EnumerateAllExceptions().ToList();
            payload.Stacktrace = payload.Exceptions?.LastOrDefault()?.Stacktrace;
            var lastFrame = payload.Stacktrace?.Frames?.LastOrDefault();
            if (lastFrame != null)
                payload.Culprit = String.Format("{0} in {1}", lastFrame.Method, lastFrame.Filename);

            return payload;
        }

        private async Task<RavenPayload> GetBasePayloadAsync(RavenLogLevel level, IDictionary<string, string> tags, IDictionary<string, object> extra)
        {
            RavenPayload payload = new RavenPayload()
            {
                EventID = Guid.NewGuid().ToString("n"),
                Project = Dsn.ProjectID,
                Level = level,
                Timestamp = DateTime.UtcNow,
                Platform = _platform.PlatformTag,
                Logger = String.IsNullOrEmpty(Logger) ? "root" : Logger,
                User = _user,
                Tags = await SetDefaultTagsAsync(tags),
                Extra = SetDefaultExtra(extra)
            };

            return payload;
        }

        internal async Task StorePayloadAsync(RavenPayload payload)
        {
            await _storage.StoreExceptionAsync(payload);
        }

        internal async Task<string> SendPayloadAsync(RavenPayload payload)
        {
            try
            {
                string jsonString = JsonConvert.SerializeObject(payload);
                HttpStringContent content = new HttpStringContent(jsonString, UnicodeEncoding.Utf8, "application/json");

                System.Diagnostics.Debug.WriteLine("[SENTRY] Sending exception to: " + Dsn.SentryUri);
                System.Diagnostics.Debug.WriteLine("[SENTRY] Payload: " + jsonString);

                var response = await _httpClient.PostAsync(Dsn.SentryUri, content);
                response.EnsureSuccessStatusCode();

                // Extract the ID and delete the stored exception so it doesn't get sent again. 
                // This will just return if no exception is stored with this ID.
                JObject responseJson = JObject.Parse(await response.Content.ReadAsStringAsync());
                string resultId = (string)responseJson["id"];

                await _storage.DeleteStoredExceptionAsync(resultId);

                return resultId;
            }
            catch (Exception ex)
            {
                HandleInternalException(ex);

                // Store this payload if there's an error sending the exception
                // e.g. server offline or client has no internet connection
                await StorePayloadAsync(payload);
            }

            return null;
        }

        private async Task<IDictionary<string, string>> SetDefaultTagsAsync(IDictionary<string, string> tags = null)
        {
            if (tags == null)
                tags = new Dictionary<string, string>();

            if (DefaultTags != null)
                foreach (var defaultTag in DefaultTags.ToList())
                    tags[defaultTag.Key] = defaultTag.Value;

            return await _platform.AppendPlatformTagsAsync(tags);
        }

        private IDictionary<string, object> SetDefaultExtra(IDictionary<string, object> extra = null)
        {
            if (extra == null)
                extra = new Dictionary<string, object>();

            if (DefaultExtra != null)
                foreach (var defaultExtraItem in DefaultExtra.ToList())
                    extra[defaultExtraItem.Key] = defaultExtraItem.Value;

            return _platform.AppendPlatformExtra(extra);
        }

        private HttpClient BuildHttpClient()
        {
            string sentryAuthHeader = String.Format(
                "Sentry sentry_version={0}, sentry_client={1}, sentry_timestamp={2}, sentry_key={3}, sentry_secret={4}",
                _sentryVersion,
                _platform.GetPlatformUserAgent(),
                (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds,
                Dsn.PublicKey,
                Dsn.PrivateKey
            );

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-Sentry-Auth", sentryAuthHeader);

            return client;
        }

        private void HandleInternalException(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(String.Format("[SENTRY] Error: {0}", ex.Message));
        }

        #endregion
    }
}
