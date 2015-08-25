using Sentry.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Sentry
{
    public class WindowsPlatformClient : IPlatformClient
    {
        public async Task<IDictionary<string, string>> AppendPlatformTagsAsync(IDictionary<string, string> tags)
        {
            if (tags == null)
                tags = new Dictionary<string, string>();

            Frame currentFrame = Window.Current?.Content as Frame;
            Type sourcePageType = currentFrame?.SourcePageType;
            string sourcePageName = sourcePageType?.FullName;
            tags["Source Page"] = !String.IsNullOrEmpty(sourcePageName) ? sourcePageName : "Unknown";

            string osVersion = await WindowsSystemInformationHelper.GetOperatingSystemVersionAsync();
            tags["OS Version"] = !String.IsNullOrEmpty(osVersion) ? osVersion : "Unknown";
            tags["Device Category"] = await WindowsSystemInformationHelper.GetDeviceCategoryAsync();
            tags["Device Manufacturer"] = await WindowsSystemInformationHelper.GetDeviceManufacturerAsync();
            tags["Device Model"] = await WindowsSystemInformationHelper.GetDeviceModelAsync();

            tags["Language"] = Windows.Globalization.ApplicationLanguages.Languages?.FirstOrDefault();
            tags["App Version"] = WindowsSystemInformationHelper.GetAppVersion();

#if WINDOWS_UWP
            tags["Device Family Version"] = WindowsSystemInformationHelper.GetDeviceFamilyVersion();
            tags["Device Family"] = WindowsSystemInformationHelper.GetDeviceFamily();
#endif

            return tags;
        }

        public IDictionary<string, object> AppendPlatformExtra(IDictionary<string, object> extra)
        {
            if (extra == null)
                extra = new Dictionary<string, object>();

            // Adds the page and parameters in the application's current back state
            var currentWindow = Window.Current;
            if (currentWindow != null)
            {
                Frame currentFrame = currentWindow.Content as Frame;
                if (currentFrame != null)
                    extra["Back Stack"] = currentFrame.BackStack?.Select(s => new KeyValuePair<string, object>(s.SourcePageType.FullName, s.Parameter));
                
                var bounds = currentWindow.Bounds;
                if (bounds != null)
                    extra["Window Size"] = String.Format("{0}x{1}", bounds.Width, bounds.Height);
            }

            // Adds internet connectivity information
            ConnectionProfile internetConnectionProfile = NetworkInformation.GetInternetConnectionProfile();
            extra["Connection Type"] = WindowsSystemInformationHelper.GetInternetConnectivityStatus(internetConnectionProfile);
            extra["Internet Provider ID"] = WindowsSystemInformationHelper.GetServiceProviderGuid(internetConnectionProfile);
            extra["Signal Strength"] = WindowsSystemInformationHelper.GetSignalStrength(internetConnectionProfile);

            try
            {
                var currentView = ApplicationView.GetForCurrentView();
                if (currentView != null)
                {
                    extra["Screen Orientation"] = currentView.Orientation;
                    extra["Is Full Screen"] = currentView.IsFullScreen;
                }
            }
            catch
            {
                // Don't worry if this fails, it seems to mostly happen in tests.
            }

            return extra;
        }

        public string GetPlatformUserAgent()
        {
            return WindowsSystemInformationHelper.GetLibraryUserAgent();
        }

        public string PlatformTag
        {
            get
            {
#if WINDOWS_UWP
                return "uwp";
#elif NETFX_CORE
                return "winrt";
#endif
            }
        }
    }
}
