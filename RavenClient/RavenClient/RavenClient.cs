using RavenClient.Models;
using RavenClient.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.Web.Http;
using RavenClient.Helpers;
using Windows.UI.Xaml.Controls;
using Newtonsoft.Json;
using Windows.Storage.Streams;
using Newtonsoft.Json.Linq;

namespace RavenClient
{
    public class RavenClient
    {
        #region Constants, static methods and properties

        private const int _sentryVersion = 4;

        private static RavenClient _instance = null;

        /// <summary>
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

        public static RavenClient Instance
        {
            get
            {
                if (_instance == null)
                    throw new InvalidOperationException("RavenClient must be initialized before the instance is called.");

                return _instance;
            }
        }

        #endregion

        #region Constructors

        public readonly Dsn Dsn;

        private readonly HttpClient _httpClient;

        private readonly RavenStorageClient _storage;

        protected RavenClient(Dsn dsn, bool captureUnhandled = true)
        {
            _httpClient = BuildHttpClient();
            
            _storage = new RavenStorageClient();

            Dsn = dsn;

            if (captureUnhandled)
                Application.Current.UnhandledException += Application_UnhandledException;
        }

        #endregion

        #region Application unhandled exception handler

        private void Application_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            CaptureExceptionAsync(e.Exception, RavenLogLevel.Fatal, null, null);
        }

        #endregion

        #region Public properties        

        public string User { get; set; }

        public string Logger { get; set; }

        #endregion

        #region Public methods

        public void CaptureMessageAsync(string message, RavenLogLevel level, IDictionary<string, string> tags, IDictionary<string, object> extra)
        {
            ProcessMessageAsync(message, level, tags, extra).ContinueWith(t => HandleInternalException(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
        }

        public void CaptureExceptionAsync(Exception ex, RavenLogLevel level, IDictionary<string, string> tags, IDictionary<string, object> extra)
        {
            ProcessExceptionAsync(ex, level, tags, extra).ContinueWith(t => HandleInternalException(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>
        /// Only call this in the "OnLaunched" and "OnActivated" handlers of App.xaml.cs
        /// </summary>
        public void RegisterAsyncContextHandler()
        {

        }

        #endregion

        #region Helper methods

        private async Task ProcessMessageAsync(string message, RavenLogLevel level, IDictionary<string, string> tags, IDictionary<string, object> extra)
        {
            RavenJsonPayload payload = await GeneratePayloadAsync(message, level, tags, extra);

#if DEBUG
            await SendPayloadAsync(payload);
#else
            await StorePayloadAsync(payload);
#endif
        }

        private async Task ProcessExceptionAsync(Exception ex, RavenLogLevel level, IDictionary<string, string> tags, IDictionary<string, object> extra)
        {
            RavenJsonPayload payload = await GeneratePayloadAsync(ex, level, tags, extra);

#if DEBUG
            await SendPayloadAsync(payload);
#else
            await StorePayloadAsync(payload);
#endif
        }

        private async Task FlushStoredPayloadsAsync()
        {
            List<Task> tasks = new List<Task>();

            var payloads = await _storage.ListStoredExceptionsAsync();
            foreach (var p in payloads)
                tasks.Add(SendPayloadAsync(p));

            foreach (var t in tasks)
                await t;
        }

        private async Task<RavenJsonPayload> GeneratePayloadAsync(string message, RavenLogLevel level, IDictionary<string, string> tags, IDictionary<string, object> extra)
        {
            RavenJsonPayload payload = await GetBasePayloadAsync(level, tags, extra);
            payload.Message = message;

            return payload;
        }

        private async Task<RavenJsonPayload> GeneratePayloadAsync(Exception ex, RavenLogLevel level, IDictionary<string, string> tags, IDictionary<string, object> extra)
        {
            RavenJsonPayload payload = await GetBasePayloadAsync(level, tags, extra);
            payload.Message = ex.Message;
            payload.Exception = ex.GetBaseException().GetType().FullName;
            payload.Stacktrace = new RavenJsonStacktrace()
            {
                Frames = ex.ToRavenFrames().ToList()
            };

            var lastFrame = payload.Stacktrace.Frames.LastOrDefault();
            if (lastFrame != null)
                payload.Culprit = String.Format("{0} in {1}", lastFrame.Method, lastFrame.Filename);

            return payload;
        }

        private async Task<RavenJsonPayload> GetBasePayloadAsync(RavenLogLevel level, IDictionary<string, string> tags, IDictionary<string, object> extra)
        {
            RavenJsonPayload payload = new RavenJsonPayload()
            {
                EventID = new Guid().ToString("n"),
                Project = Dsn.ProjectID,
                Level = level,
                Timestamp = DateTime.UtcNow,
                Platform = "uwp",
                Logger = this.Logger,
                User = this.User,
                Tags = await AppendDefaultTags(tags),
                Extra = await AppendDefaultExtra(extra)
            };

            return payload;
        }

        private async Task StorePayloadAsync(RavenJsonPayload payload)
        {
            await _storage.StoreExceptionAsync(payload);
        }

        private async Task SendPayloadAsync(RavenJsonPayload payload)
        {
            string jsonString = JsonConvert.SerializeObject(payload);
            IHttpContent content = new HttpStringContent(jsonString, UnicodeEncoding.Utf8, "application/json");
            var response = await _httpClient.PostAsync(Dsn.SentryUri, content);

            // Extract the ID and delete the stored exception so
            // it doesn't get sent again. This will just return
            // if no exception is stored with this ID.
            JObject responseJson = JObject.Parse(await response.Content.ReadAsStringAsync());
            string resultId = (string)responseJson["id"];
            await _storage.DeleteStoredExceptionAsync(resultId);
        }

        private async Task<IDictionary<string, string>> AppendDefaultTags(IDictionary<string, string> tags = null)
        {
            if (tags == null)
                tags = new Dictionary<string, string>();

            Frame currentFrame = Window.Current.Content as Frame;
            tags["Page"] = currentFrame?.CurrentSourcePageType.FullName;

            // TODO add project, device info, system info, and app info

            return tags;
        }

        private async Task<IDictionary<string, object>> AppendDefaultExtra(IDictionary<string, object> extra = null)
        {
            if (extra == null)
                extra = new Dictionary<string, object>();

            Frame currentFrame = Window.Current.Content as Frame;
            extra["BackStack"] = currentFrame.BackStack.Select(s => new KeyValuePair<string, object>(s.SourcePageType.FullName, s.Parameter));

            return extra;
        }

        private HttpClient BuildHttpClient()
        {
            string sentryAuthHeader = String.Format(
                "Sentry sentry_version={0}, sentry_client={1}, sentry_timestamp={2}, sentry_key={3}, sentry_secret={4}",
                _sentryVersion,
                "nativehost",
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
            System.Diagnostics.Debug.WriteLine(ex.Message);
        }

        #endregion
    }
}
