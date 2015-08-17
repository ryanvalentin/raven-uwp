using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using RavenUWP;
using RavenUWP.Models;

namespace TestApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private void handledExceptionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                throw new Exception("This is a test exception");
            }
            catch (Exception ex)
            {
                RavenClient.Instance.CaptureExceptionAsync(ex, false, RavenLogLevel.Debug);
            }
        }

        private async void refreshAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var ravenStorage = new RavenUWP.Storage.RavenStorageClient();

            storedExceptionsListView.ItemsSource = await ravenStorage.ListStoredExceptionsAsync();
        }
    }
}
