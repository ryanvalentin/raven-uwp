using System;
using System.Collections.Generic;
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
    /// <summary>
    /// Most of these methods based on this helper https://github.com/AttackPattern/CSharpAnalytics/blob/master/Source/CSharpAnalytics/SystemInfo/WindowsStoreSystemInfo.cs
    /// </summary>
    internal static class SystemInformationHelper
    {
        private const string ItemNameKey = "System.ItemNameDisplay";
        private const string ModelNameKey = "System.Devices.ModelName";
        private const string ManufacturerKey = "System.Devices.Manufacturer";
        private const string DeviceClassKey = "{A45C254E-DF1C-4EFD-8020-67D146A850E0},10";
        private const string PrimaryCategoryKey = "{78C34FC8-104A-4ACA-9EA4-524D52996E57},97";
        private const string DeviceDriverVersionKey = "{A8B865DD-2E3D-4094-AD97-E593A70C75D6},3";
        private const string DeviceDriverProviderKey = "{A8B865DD-2E3D-4094-AD97-E593A70C75D6},9";
        private const string RootContainer = "{00000000-0000-0000-FFFF-FFFFFFFFFFFF}";
        private const string RootQuery = "System.Devices.ContainerId:=\"" + RootContainer + "\"";
        private const string HalDeviceClass = "4d36e966-e325-11ce-bfc1-08002be10318";

        private static string _operatingSystemVersion = null;
        internal static async Task<string> GetOperatingSystemVersionAsync()
        {
            if (_operatingSystemVersion == null)
            {
                try
                {
                    // There is no good place to get this so we're going to use the most popular
                    // Microsoft driver version number from the device tree.
                    var requestedProperties = new[] { DeviceDriverVersionKey, DeviceDriverProviderKey };

                    var microsoftVersionedDevices = (await PnpObject.FindAllAsync(PnpObjectType.Device, requestedProperties, RootQuery))
                        .Select(d => new
                        {
                            Provider = (string)d.Properties.GetValueOrDefault(DeviceDriverProviderKey),
                            Version = (string)d.Properties.GetValueOrDefault(DeviceDriverVersionKey)
                        })
                        .Where(d => d.Provider == "Microsoft" && d.Version != null)
                        .ToList();

                    var versionNumbers = microsoftVersionedDevices
                        .GroupBy(d => d.Version.Substring(0, d.Version.IndexOf('.', d.Version.IndexOf('.') + 1)))
                        .OrderByDescending(d => d.Count())
                        .ToList();

                    var confidence = (versionNumbers[0].Count() * 100 / microsoftVersionedDevices.Count);
                    _operatingSystemVersion = versionNumbers.Count > 0 ? versionNumbers[0].Key : "";
                }
                catch
                {
                    _operatingSystemVersion = "Unknown";
                }
            }

            return _operatingSystemVersion;
        }

        private static TValue GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key)
        {
            TValue value;
            return dictionary.TryGetValue(key, out value) ? value : default(TValue);
        }


        private static string _deviceManufacturer = null;
        internal static async Task<string> GetDeviceManufacturerAsync()
        {
            if (_deviceManufacturer == null)
                _deviceManufacturer = await GetRootDeviceInfoAsync(ManufacturerKey);

            return _deviceManufacturer;
        }

        private static string _deviceModel = null;
        internal static async Task<string> GetDeviceModelAsync()
        {
            if (_deviceModel == null)
                _deviceModel = await GetRootDeviceInfoAsync(ModelNameKey);

            return _deviceModel;
        }

        private static string _deviceCategory = null;
        internal static async Task<string> GetDeviceCategoryAsync()
        {
            if (_deviceCategory == null)
                _deviceCategory = await GetRootDeviceInfoAsync(PrimaryCategoryKey);

            return _deviceCategory;
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
