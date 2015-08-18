using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Devices.Enumeration.Pnp;
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

        private const string ItemNameKey = "System.ItemNameDisplay";
        private const string ModelNameKey = "System.Devices.ModelName";
        private const string ManufacturerKey = "System.Devices.Manufacturer";
        private const string DeviceClassKey = "{A45C254E-DF1C-4EFD-8020-67D146A850E0},10";
        private const string PrimaryCategoryKey = "{78C34FC8-104A-4ACA-9EA4-524D52996E57},97";
        private const string DeviceDriverVersionKey = "{A8B865DD-2E3D-4094-AD97-E593A70C75D6},3";
        private const string RootContainer = "{00000000-0000-0000-FFFF-FFFFFFFFFFFF}";
        private const string RootQuery = "System.Devices.ContainerId:=\"" + RootContainer + "\"";
        private const string HalDeviceClass = "4d36e966-e325-11ce-bfc1-08002be10318";

        internal static Task<string> GetDeviceManufacturerAsync()
        {
            return GetRootDeviceInfoAsync(ManufacturerKey);
        }

        internal static Task<string> GetDeviceModelAsync()
        {
            return GetRootDeviceInfoAsync(ModelNameKey);
        }

        internal static Task<string> GetDeviceCategoryAsync()
        {
            return GetRootDeviceInfoAsync(PrimaryCategoryKey);
        }

        internal static async Task<string> GetWindowsVersionAsync()
        {
            // There is no good place to get this.
            // The HAL driver version number should work unless you're using a custom HAL... 
            var hal = await GetHalDevice(DeviceDriverVersionKey);
            if (hal == null || !hal.Properties.ContainsKey(DeviceDriverVersionKey))
                return null;

            var versionParts = hal.Properties[DeviceDriverVersionKey].ToString().Split('.');
            return string.Join(".", versionParts.Take(2).ToArray());
        }

        private static async Task<string> GetRootDeviceInfoAsync(string propertyKey)
        {
            var pnp = await PnpObject.CreateFromIdAsync(PnpObjectType.DeviceContainer,
                        RootContainer, new[] { propertyKey });
            return (string)pnp.Properties[propertyKey];
        }

        private static async Task<PnpObject> GetHalDevice(params string[] properties)
        {
            var actualProperties = properties.Concat(new[] { DeviceClassKey });
            var rootDevices = await PnpObject.FindAllAsync(PnpObjectType.Device,
                actualProperties, RootQuery);

            foreach (var rootDevice in rootDevices.Where(d => d.Properties != null && d.Properties.Any()))
            {
                var lastProperty = rootDevice.Properties.Last();
                if (lastProperty.Value != null)
                    if (lastProperty.Value.ToString().Equals(HalDeviceClass))
                        return rootDevice;
            }
            return null;
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
