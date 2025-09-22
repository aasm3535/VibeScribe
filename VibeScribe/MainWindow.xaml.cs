using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Windows.AppNotifications;
using Microsoft.UI.Xaml.Controls;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VibeScribe
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            AppWindow.Resize(new Windows.Graphics.SizeInt32(800, 500));

            //AppWindow.Move(new Windows.Graphics.PointInt32(50, 50));

            AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;

        }

    public void RecordButton_Click(object sender, RoutedEventArgs e)
    {
        AppNotification notification = new AppNotificationBuilder()
            .AddText("Началась запись")
            .BuildNotification();

        AppNotificationManager.Default.Show(notification);
    }

    public void NewRecordingButton_Click(object sender, RoutedEventArgs e)
    {
        AppNotification notification = new AppNotificationBuilder()
            .AddText("Новая запись создана")
            .BuildNotification();

        AppNotificationManager.Default.Show(notification);
    }
    }
}
