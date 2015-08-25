using System;

namespace Sentry
{
    /// <summary>
    /// The Data Source Name of a given project in Sentry.
    /// </summary>
    public class Dsn
    {
        private readonly string path;
        private readonly int port;
        private readonly string privateKey;
        private readonly string projectID;
        private readonly string publicKey;
        private readonly Uri sentryUri;
        private readonly Uri uri;

        /// <summary>
        /// Initializes a new instance of the <see cref="Dsn"/> class.
        /// </summary>
        /// <param name="dsn">The Data Source Name.</param>
        public Dsn(string dsn)
        {
            if (String.IsNullOrWhiteSpace(dsn))
                throw new ArgumentNullException("dsn");

            try
            {
                this.uri = new Uri(dsn);
                this.privateKey = GetPrivateKey(this.uri);
                this.publicKey = GetPublicKey(this.uri);
                this.port = this.uri.Port;
                this.projectID = GetProjectID(this.uri);
                this.path = GetPath(this.uri);
                var sentryUriString = String.Format(
                    "{0}://{1}:{2}{3}/api/{4}/store/",
                    this.uri.Scheme,
                    this.uri.DnsSafeHost,
                    Port,
                    Path,
                    ProjectID);
                this.sentryUri = new Uri(sentryUriString);
            }
            catch (Exception exception)
            {
                throw new ArgumentException("Invalid DSN", exception);
            }
        }


        /// <summary>
        /// Sentry path.
        /// </summary>
        public string Path
        {
            get { return this.path; }
        }

        /// <summary>
        /// The sentry server port.
        /// </summary>
        public int Port
        {
            get { return this.port; }
        }

        /// <summary>
        /// Project private key.
        /// </summary>
        public string PrivateKey
        {
            get { return this.privateKey; }
        }

        /// <summary>
        /// Project identification.
        /// </summary>
        public string ProjectID
        {
            get { return this.projectID; }
        }

        /// <summary>
        /// Project public key.
        /// </summary>
        public string PublicKey
        {
            get { return this.publicKey; }
        }

        /// <summary>
        /// Sentry Uri for sending reports.
        /// </summary>
        public Uri SentryUri
        {
            get { return this.sentryUri; }
        }

        /// <summary>
        /// Absolute Dsn Uri
        /// </summary>
        public Uri Uri
        {
            get { return this.uri; }
        }


        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.uri.ToString();
        }


        /// <summary>
        /// Get a path from a Dsn uri
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        private static string GetPath(Uri uri)
        {
            int lastSlash = uri.AbsolutePath.LastIndexOf("/", StringComparison.Ordinal);
            return uri.AbsolutePath.Substring(0, lastSlash);
        }


        /// <summary>
        /// Get a private key from a Dsn uri.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        private static string GetPrivateKey(Uri uri)
        {
            return uri.UserInfo.Split(':')[1];
        }


        /// <summary>
        /// Get a project ID from a Dsn uri.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        private static string GetProjectID(Uri uri)
        {
            int lastSlash = uri.AbsoluteUri.LastIndexOf("/", StringComparison.Ordinal);
            return uri.AbsoluteUri.Substring(lastSlash + 1);
        }


        /// <summary>
        /// Get a public key from a Dsn uri.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        private static string GetPublicKey(Uri uri)
        {
            return uri.UserInfo.Split(':')[0];
        }
    }
}
