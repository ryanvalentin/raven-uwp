using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RavenUWP.Helpers;
using RavenUWP.Models;
using RavenUWP.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Web.Http;

namespace RavenUWP
{
    public class RavenClient
    {
        public readonly Dsn Dsn;

        private const int _sentryVersion = 4;
        
        private readonly HttpClient _httpClient;

        private readonly RavenStorageClient _storage;

        private static RavenClient _instance = null;

        protected RavenClient(Dsn dsn, bool captureUnhandled = true)
        {
            Dsn = dsn;

            _storage = new RavenStorageClient();
            _httpClient = BuildHttpClient();
            
            if (captureUnhandled)
                Application.Current.UnhandledException += Application_UnhandledException;
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

        /// <summary>
        /// Gets or sets the user to be sent with every Sentry request.
        /// </summary>
        public string User { get; set; }

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

            RavenPayload payload = await GetBasePayloadAsync(level, tags, extra);
            payload.Message = String.Format("{0}: {1}", exceptionName, ex.Message);
            payload.Exception = exceptionName;
            payload.Stacktrace = new RavenStacktrace()
            {
                Frames = ex.ToRavenFrames().ToList()
            };

            var lastFrame = payload.Stacktrace.Frames.LastOrDefault();
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
                Platform = "uwp",
                Logger = Logger,
                User = User,
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
            string jsonString = JsonConvert.SerializeObject(payload);
            IHttpContent content = new HttpStringContent(jsonString, UnicodeEncoding.Utf8, "application/json");
            
            System.Diagnostics.Debug.WriteLine("[RAVEN] Sending exception to: " + Dsn.SentryUri);
            System.Diagnostics.Debug.WriteLine("[RAVEN] Payload: " + jsonString);

            var response = await _httpClient.PostAsync(Dsn.SentryUri, content);

            // Extract the ID and delete the stored exception so it doesn't get sent again. 
            // This will just return if no exception is stored with this ID.
            JObject responseJson = JObject.Parse(await response.Content.ReadAsStringAsync());
            string resultId = (string)responseJson["id"];

            await _storage.DeleteStoredExceptionAsync(resultId);

            return resultId;
        }

        private async Task<IDictionary<string, string>> SetDefaultTagsAsync(IDictionary<string, string> tags = null)
        {
            if (tags == null)
                tags = new Dictionary<string, string>();

            if (DefaultTags != null)
            {
                foreach (var defaultTag in DefaultTags)
                    tags[defaultTag.Key] = defaultTag.Value;
            }

            Frame currentFrame = Window.Current?.Content as Frame;
            Type sourcePageType = currentFrame?.SourcePageType;
            string sourcePageName = sourcePageType?.FullName;
            tags["Source Page"] = !String.IsNullOrEmpty(sourcePageName) ? sourcePageName : "Unknown";

            string osVersion = await SystemInformationHelper.GetOperatingSystemVersionAsync();
            tags["OS Version"] = !String.IsNullOrEmpty(osVersion) ? osVersion : "Unknown";

#if WINDOWS_UWP
            tags["Device Family Version"] = SystemInformationHelper.GetDeviceFamilyVersion();
            tags["Device Family"] = SystemInformationHelper.GetDeviceFamily();
#endif
            tags["Device Manufacturer"] = await SystemInformationHelper.GetDeviceManufacturerAsync();
            tags["Device Model"] = await SystemInformationHelper.GetDeviceModelAsync();

            tags["Language"] = Windows.Globalization.ApplicationLanguages.Languages?.FirstOrDefault();
            tags["App Version"] = SystemInformationHelper.GetAppVersion();

            return tags;
        }

        private IDictionary<string, object> SetDefaultExtra(IDictionary<string, object> extra = null)
        {
            if (extra == null)
                extra = new Dictionary<string, object>();

            if (DefaultExtra != null)
            {
                foreach (var defaultExtra in DefaultExtra)
                    extra[defaultExtra.Key] = defaultExtra.Value;
            }
            
            try
            {
                // Adds the page and parameters in the application's current back state
                Frame currentFrame = Window.Current.Content as Frame;
                extra["Back Stack"] = currentFrame.BackStack.Select(s => new KeyValuePair<string, object>(s.SourcePageType.FullName, s.Parameter));

                // Adds internet connectivity information
                ConnectionProfile internetConnectionProfile = NetworkInformation.GetInternetConnectionProfile();
                extra["Connection Type"] = SystemInformationHelper.GetInternetConnectivityStatus(internetConnectionProfile);
                extra["Internet Provider ID"] = SystemInformationHelper.GetServiceProviderGuid(internetConnectionProfile);
                extra["Signal Strength"] = SystemInformationHelper.GetSignalStrength(internetConnectionProfile);
            }
            catch (Exception ex)
            {
                HandleInternalException(ex);
            }

            return extra;
        }

        private HttpClient BuildHttpClient()
        {
            string sentryAuthHeader = String.Format(
                "Sentry sentry_version={0}, sentry_client={1}, sentry_timestamp={2}, sentry_key={3}, sentry_secret={4}",
                _sentryVersion,
                SystemInformationHelper.GetLibraryUserAgent(),
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
            System.Diagnostics.Debug.WriteLine(String.Format("[RAVEN] Error: {0}", ex.Message));
        }

#endregion
    }
}
