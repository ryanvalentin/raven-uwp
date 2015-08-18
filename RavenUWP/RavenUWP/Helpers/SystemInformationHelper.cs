using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Networking.Connectivity;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

[assembly: InternalsVisibleTo("RavenUWP.Tests")]

namespace RavenUWP.Helpers
{
    internal static class SystemInformationHelper
    {
        internal static string GetAppVersion()
        {
            PackageVersion version = Package.Current.Id.Version;

            return String.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
        }

        internal static string GetLibraryUserAgent()
        {
            string assemblyQualifiedName = typeof(SystemInformationHelper).AssemblyQualifiedName;
            string[] assemblyArray = assemblyQualifiedName.Split(',');
            string clientName = assemblyArray[1].Trim();
            string[] clientVersion = assemblyArray[2].Split('=')[1].Split('.');

            return String.Format("{0}/{1}.{2}", clientName, clientVersion[0], clientVersion[1]);
        }

        internal static async Task<string> GetOperatingSystemVersionAsync()
        {
            string userAgent = null;

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, async () =>
            {
                userAgent = await GetBrowserUserAgent();
            });

            string result = String.Empty;
            
            if (userAgent != null)
            {
                int startIndex = userAgent.ToLower().IndexOf("windows");
                if (startIndex > 0)
                {
                    int endIndex = userAgent.IndexOf(";", startIndex);

                    if (endIndex > startIndex)
                        result = userAgent.Substring(startIndex, endIndex - startIndex);
                }
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

            if (_userAgent == null)
            {
                WebView webView = new WebView();
                string htmlPage = "<html><head><script type='text/javascript'>function GetUserAgent(){return navigator.userAgent;}</script></head></html>";
                webView.NavigationCompleted += async (sender, e) =>
                {
                    try
                    {
                        // Make sure we cache the user agent so we don't have to re-run this entire method
                        _userAgent = await webView.InvokeScriptAsync("GetUserAgent", null);
                    }
                    catch (Exception ex)
                    {
                        taskCompletionSource.TrySetException(ex);
                    }
                };

                webView.NavigateToString(htmlPage);
            }

            taskCompletionSource.TrySetResult(_userAgent);

            return taskCompletionSource.Task;
        }

#if WINDOWS_UWP

        internal static string GetDeviceFamilyVersion()
        {
            return AnalyticsInfo.VersionInfo?.DeviceFamilyVersion;
        }

        internal static string GetDeviceFamily()
        {
            return AnalyticsInfo.VersionInfo?.DeviceFamily;
        }
#endif
    }
}
