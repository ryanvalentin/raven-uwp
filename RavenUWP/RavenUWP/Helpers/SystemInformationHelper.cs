using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Networking.Connectivity;
using Windows.System.Profile;
using Windows.UI.Xaml.Controls;

namespace RavenUWP.Helpers
{
    internal static class SystemInformationHelper
    {
        internal static string GetAppVersion()
        {
            PackageVersion version = Package.Current.Id.Version;

            return String.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
        }

        internal static async Task<string> GetOperatingSystemVersionAsync()
        {
            string userAgent = await GetBrowserUserAgent();

            string result = String.Empty;

            //Parse user agent
            int startIndex = userAgent.ToLower().IndexOf("windows");
            if (startIndex > 0)
            {
                int endIndex = userAgent.IndexOf(";", startIndex);

                if (endIndex > startIndex)
                    result = userAgent.Substring(startIndex, endIndex - startIndex);
            }

            return result;
        }

        internal static string GetInternetConnectivityStatus(ConnectionProfile connectionProfile)
        {
            if (connectionProfile == null)
            {
                return "No Connection";
            }
            else
            {
                try
                {
                    return connectionProfile.GetNetworkConnectivityLevel().ToString();
                }
                catch
                {
                    return "Unknown";
                }
            }
        }

        internal static string GetServiceProviderGuid(ConnectionProfile connectionProfile)
        {
            return connectionProfile.ServiceProviderGuid.HasValue ? connectionProfile.ServiceProviderGuid.ToString() : "Unknown";
        }

        internal static string GetSignalStrength(ConnectionProfile connectionProfile)
        {
            return connectionProfile.GetSignalBars().HasValue ? connectionProfile.GetSignalBars().ToString() : "Unknown";
        }

        private static string _userAgent = null;

        internal static Task<string> GetBrowserUserAgent()
        {
            var taskCompletionSource = new TaskCompletionSource<string>();

            if (_userAgent != null)
            {
                taskCompletionSource.TrySetResult(_userAgent);

                return taskCompletionSource.Task;
            }

            WebView webView = new WebView();
            string htmlPage = "<html><head><script type='text/javascript'>function GetUserAgent(){return navigator.userAgent;}</script></head></html>";
            webView.NavigationCompleted += async (sender, e) =>
            {
                try
                {
                    string userAgent = await webView.InvokeScriptAsync("GetUserAgent", null);
                    taskCompletionSource.TrySetResult(userAgent);
                }
                catch (Exception ex)
                {
                    taskCompletionSource.TrySetException(ex);
                }
            };

            webView.NavigateToString(htmlPage);

            return taskCompletionSource.Task;
        }

#if WINDOWS_UWP

        internal static string GetDeviceFamilyVersion()
        {
            return AnalyticsInfo.VersionInfo.DeviceFamilyVersion;
        }

        internal static string GetDeviceFamily()
        {
            return AnalyticsInfo.VersionInfo.DeviceFamily;
        }
#endif
    }
}
