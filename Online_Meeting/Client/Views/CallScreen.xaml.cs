using Microsoft.Web.WebView2.Core;
using System.Text.Json;
using System.Windows;


namespace Online_Meeting.Client.Views
{
    /// <summary>
    /// Interaction logic for CallScreen.xaml
    /// </summary>
    public partial class CallScreen : Window
    {
        public CallScreen()
        {
            InitializeComponent();
            Loaded += async (_, __) =>
            {
                await Web.EnsureCoreWebView2Async();
                Web.CoreWebView2.PermissionRequested += (s, e) =>
                {
                    if (e.PermissionKind is CoreWebView2PermissionKind.Microphone
                                          or CoreWebView2PermissionKind.Camera)
                    {
                        e.State = CoreWebView2PermissionState.Allow;
                        e.Handled = true;
                    }
                };

                // Nhận sự kiện từ JS (ví dụ: xin bật/tắt camera, chọn device…)
                Web.CoreWebView2.WebMessageReceived += (s, e) =>
                {
                    var msg = e.TryGetWebMessageAsString();
                    Console.WriteLine($"From JS: {msg}");

                    // Xử lý message từ JS
                };

                // Tải file local
                var path = System.IO.Path.GetFullPath("CallScreenView/index.html");
                Web.Source = new Uri(path);
            };
        }

        // Ví dụ: nút bật camera từ WPF gọi JS
        private void ToggleCamera_Click(object sender, RoutedEventArgs e)
        {
            Web.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new
            {
                type = "toggleCamera"
            }));
        }

        private void ToggleMic_Click(object sender, RoutedEventArgs e)
        {
            Web.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new
            {
                type = "toggleMic"
            }));
        }

        private void SelectDevice_Click(object sender, RoutedEventArgs e)
        {
            // Chọn deviceId cụ thể (ví dụ lấy từ combobox WPF)
            var deviceId = "your-camera-device-id";
            Web.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new
            {
                type = "setCamera",
                deviceId
            }));
        }
    }
}
