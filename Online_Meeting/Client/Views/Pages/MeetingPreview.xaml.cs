using Microsoft.Web.WebView2.Core;
using Online_Meeting.Client.Models;
using Online_Meeting.Client.ViewModels;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace Online_Meeting.Client.Views.Dialogs
{
    public partial class MeetingPreview : Window
    {
        public string SelectedCameraId { get; set; }
        public string SelectedMicId { get; set; }
        public bool AudioEnabled { get; set; }
        public bool VideoEnabled { get; set; }
        public bool AlwaysShowPreview { get; set; }
        public bool IsPermissionGranted { get; set; }

        // Virtual host name cho WebView2 secure origin
        private const string VirtualHost = "appassets.example";
        public event EventHandler<MeetingStartEventArgs>? PreviewCompleted;
        public MeetingPreview()
        {
            InitializeComponent();

            InitializeWebView();
        }

        // WebView2 Initialization
        private async void InitializeWebView()
        {
            try
            {
                // Ensure CoreWebView2 ready
                await PreviewWebView.EnsureCoreWebView2Async(null);

                // Map thư mục chứa file HTML thành một virtual host (secure origin).
                string exeFolder = AppDomain.CurrentDomain.BaseDirectory;
                string webFolder = Path.Combine(exeFolder, "Client", "Views", "CallScreenView");

                try
                {
                    // Mapping phải được thực hiện trước khi Navigate để trang được coi là secure origin
                    PreviewWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                        VirtualHost,
                        webFolder,
                        CoreWebView2HostResourceAccessKind.Allow);
                    System.Diagnostics.Debug.WriteLine($"Mapped {VirtualHost} -> {webFolder}");
                }
                catch (Exception mapEx)
                {
                    System.Diagnostics.Debug.WriteLine($"SetVirtualHostNameToFolderMapping failed: {mapEx.Message}");
                }

                // Đăng ký permission handler để cho phép camera/microphone
                PreviewWebView.CoreWebView2.PermissionRequested += CoreWebView2_PermissionRequested;

                // Đăng ký WebMessageReceived để nhận message từ JS
                PreviewWebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

                // Navigate qua host đã map -> secure origin
                string url = $"https://{VirtualHost}/MeetingPreview.html";
                PreviewWebView.CoreWebView2.Navigate(url);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Cannot initialize WebView2: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void PreviewWebView_NavigationCompleted(
            object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
            {
                MessageBox.Show($"Navigation failed: {e.WebErrorStatus}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Navigation completed: " + PreviewWebView.Source);
            }
        }

        // Permission handler: Allow Camera / Microphone
        private void CoreWebView2_PermissionRequested(object sender, CoreWebView2PermissionRequestedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"PermissionRequested: {e.PermissionKind} from {e.Uri}");

                // Nếu origin là virtual host của bạn thì cho phép camera/microphone
                if ((e.PermissionKind == CoreWebView2PermissionKind.Camera ||
                     e.PermissionKind == CoreWebView2PermissionKind.Microphone) &&
                    (e.Uri?.Contains(VirtualHost, StringComparison.OrdinalIgnoreCase) ?? false))
                {
                    e.State = CoreWebView2PermissionState.Allow;
                    System.Diagnostics.Debug.WriteLine($"Permission allowed for {e.PermissionKind} at {e.Uri}");
                }
                else
                {
                    // giữ default cho các permission khác hoặc origin không tin cậy
                    e.State = CoreWebView2PermissionState.Default;
                    System.Diagnostics.Debug.WriteLine($"Permission default for {e.PermissionKind} at {e.Uri}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("PermissionRequested handler error: " + ex.Message);
            }
        }

        // JavaScript → C# Communication
        private void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string json = e.WebMessageAsJson;
                System.Diagnostics.Debug.WriteLine("WebMessageReceived: " + json);

                var message = JsonSerializer.Deserialize<WebPreviewMsg>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (message == null) return;

                // CHỈ xử lý một số message types
                switch (message.type)
                {
                    case "start-meeting":
                        OnPreviewCompleted(message);
                        break;

                    case "preview-cancel":
                        DialogResult = false;
                        Close();
                        break;

                    case "permission-granted":
                        IsPermissionGranted = true;
                        break;

                    case "permission-denied":
                        IsPermissionGranted = false;
                        MessageBox.Show(
                            "Camera/Microphone permission denied.\nPlease grant permission to continue.",
                            "Permission Required",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        break;

                    case "devices":
                        // optional: log devices received from page
                        System.Diagnostics.Debug.WriteLine("Devices message received.");
                        break;

                    default:
                        System.Diagnostics.Debug.WriteLine("Unhandled message type: " + message.type);
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing message: {ex.Message}");
            }
        }

        // ============================================
        // Event Handler
        // ============================================
        
        private void OnPreviewCompleted(WebPreviewMsg message)
        {
            var result = new MeetingStartEventArgs
            {
                CameraId = message.cameraId,
                MicId = message.micId,
                AlwaysShowPreview = message.alwaysPreview,
                AudioEnabled = message.audio,
                VideoEnabled = message.video,
                IsPermissionGranted = IsPermissionGranted
            };

            PreviewCompleted?.Invoke(this, result);
            DialogResult = true;
            Close();
        }

        // Cleanup
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            try
            {
                if (PreviewWebView?.CoreWebView2 != null)
                {
                    // Unsubscribe tất cả sự kiện ta đã đăng ký
                    try { PreviewWebView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived; } catch { }
                    try { PreviewWebView.CoreWebView2.PermissionRequested -= CoreWebView2_PermissionRequested; } catch { }
                }
            }
            catch { /* swallow */ }
        }
    }
}
