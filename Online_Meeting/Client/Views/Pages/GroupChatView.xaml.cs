using Microsoft.Extensions.DependencyInjection;
using Online_Meeting.Client.Interfaces;
using Online_Meeting.Client.Models;
using Online_Meeting.Client.Services;
using Online_Meeting.Client.ViewModels;
using Online_Meeting.Client.Views.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Online_Meeting.Client.Views.Pages
{
    public partial class GroupChatView : Page
    {
        private readonly ITokenService _token;
        private readonly IGroupService _groupService;
        private readonly ChatViewModel _chatViewModel;


        public GroupChatView(TokenService tokenService, IGroupService groupService, ChatViewModel chatViewModel)
        {
            InitializeComponent();
            _token = tokenService;
            _groupService = groupService;
            _chatViewModel = chatViewModel;

            // ✅ Subscribe event từ ViewModel
            _chatViewModel.Messages.CollectionChanged += Messages_CollectionChanged;

            // ✅ Load khi Page Loaded
            Loaded += async (s, e) =>
            {
                await LoadGroupsAsync();
                await InitializeSignalRAsync();
            };
        }

        private async Task InitializeSignalRAsync()
        {
            try
            {
                var username = _token.GetUsername();
                if (string.IsNullOrEmpty(username))
                {
                    System.Diagnostics.Debug.WriteLine("[GroupChatView]  Username not found");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[GroupChatView]  Connecting SignalR for user: {username}");

                await _chatViewModel.InitializeAsync(Guid.Empty, username);

                System.Diagnostics.Debug.WriteLine($"[GroupChatView]  SignalR connected!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GroupChatView]  SignalR failed: {ex.Message}");
                MessageBox.Show($"Lỗi kết nối: {ex.Message}");
            }
        }

        // ==========================================================
        //  XỬ LÝ HIỂN THỊ TIN NHẮN - RENDER LẠI TOÀN BỘ
        // ==========================================================
        private void Messages_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[UI] Messages_CollectionChanged: {e.Action}");

            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                // ✅ CHỈ thêm tin nhắn mới
                foreach (ChatMessage msg in e.NewItems)
                {
                    System.Diagnostics.Debug.WriteLine($"[UI] Adding message: {msg.Content} | IsMyMessage: {msg.IsMyMessage}");
                    var bubble = CreateMessageBubble(msg);
                    ChatMessagesPanel.Children.Add(bubble);
                }
                ScrollToBottom();
            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            {
                // ✅ Clear và render lại toàn bộ
                System.Diagnostics.Debug.WriteLine($"[UI] Clearing and re-rendering all messages");
                RenderAllMessages();
            }
        }

        // ✅ HÀM MỚI: Render toàn bộ messages từ ViewModel
        private void RenderAllMessages()
        {
            ChatMessagesPanel.Children.Clear();

            System.Diagnostics.Debug.WriteLine($"[UI] Rendering {_chatViewModel.Messages.Count} messages");

            foreach (var msg in _chatViewModel.Messages)
            {
                System.Diagnostics.Debug.WriteLine($"[UI] Render: {msg.Content} | IsMyMessage: {msg.IsMyMessage}");
                var bubble = CreateMessageBubble(msg);
                ChatMessagesPanel.Children.Add(bubble);
            }

            ScrollToBottom();
        }
        // ==========================================================
        // LOAD GROUPS
        // ==========================================================
        public async Task LoadGroupsAsync()
        {
            try
            {
                GroupsList.Children.Clear();

                var response = await _groupService.GetMyGroupsAsync();

                if (response != null && response.Success && response.Data != null)
                {
                    foreach (var group in response.Data)
                    {
                        var button = CreateGroupButton(group);
                        GroupsList.Children.Add(button);
                    }
                }
                else
                {
                    MessageBox.Show("Không thể tải danh sách nhóm!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải nhóm: {ex.Message}");
            }
        }

        private Button CreateGroupButton(ChatGroup group)
        {
            var button = new Button
            {
                Style = (Style)FindResource("GroupItemButton"),
                Tag = group.Id,
                Margin = new Thickness(0, 0, 0, 8)
            };
            button.Click += GroupItem_Click;

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Avatar
            var border = new Border
            {
                Width = 56,
                Height = 56,
                CornerRadius = new CornerRadius(28),
                Background = System.Windows.Media.Brushes.MediumSlateBlue,
                Margin = new Thickness(0, 0, 12, 0)
            };
            var initials = new TextBlock
            {
                Text = GetInitials(group.GroupName),
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            border.Child = initials;
            grid.Children.Add(border);

            // Group info
            var infoPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(infoPanel, 1);
            infoPanel.Children.Add(new TextBlock
            {
                Text = group.GroupName,
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = (System.Windows.Media.Brush)FindResource("TextPrimaryBrush")
            });
            infoPanel.Children.Add(new TextBlock
            {
                Text = $"Trạng thái: {group.Status}",
                FontSize = 13,
                Foreground = (System.Windows.Media.Brush)FindResource("TextSecondaryBrush"),
                Margin = new Thickness(0, 4, 0, 0)
            });
            grid.Children.Add(infoPanel);

            // Time
            var timeText = new TextBlock
            {
                Text = group.Id.ToString().Substring(0, 8),
                FontSize = 11,
                Foreground = (System.Windows.Media.Brush)FindResource("TextSecondaryBrush"),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(timeText, 2);
            grid.Children.Add(timeText);

            button.Content = grid;
            return button;
        }

        private string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            var parts = name.Split(' ');
            if (parts.Length == 1) return parts[0].Substring(0, 1).ToUpper();
            return (parts[0][0].ToString() + parts[^1][0].ToString()).ToUpper();
        }

        // ==========================================================
        //  CLICK VÀO GROUP - CHỈ GỌI VIEWMODEL
        // ==========================================================
        private async void GroupItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Guid groupId)
            {
              //  System.Diagnostics.Debug.WriteLine($"[UI] Group clicked: {groupId}");

                // ✅ CHỈ GỌI VIEWMODEL - KHÔNG GỌI LoadMessagesAsync() NỮA!
                await _chatViewModel.LoadGroupAsync(groupId);

                // Update header UI
                var groupNameTextBlock = ((button.Content as Grid)?.Children[1] as StackPanel)?.Children[0] as TextBlock;
                if (groupNameTextBlock != null)
                    ChatHeaderTitle.Text = groupNameTextBlock.Text;

                EmptyChatState.Visibility = Visibility.Collapsed;
                ChatContainer.Visibility = Visibility.Visible;

              //  System.Diagnostics.Debug.WriteLine($"[UI] Group loaded. Messages count: {_chatViewModel.Messages.Count}");
            }
        }

        // ==========================================================
        // CONTEXT MENU
        // ==========================================================
        private void PlusButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                btn.ContextMenu.IsOpen = true;
            }
        }

        private void CreateGroupMenu_Click(object sender, RoutedEventArgs e)
        {
            var token = _token.GetAccessToken();
            if (string.IsNullOrEmpty(token))
            {
                MessageBox.Show("Please login first!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = App.Services.GetService<CreateGroupDialog>();
            if (dialog == null)
            {
                MessageBox.Show("Dialog service not registered in DI container.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            dialog.OnGroupCreated = async () => await LoadGroupsAsync();
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
        }

        private void JoinGroupMenu_Click(object sender, RoutedEventArgs e)
        {
            var token = _token.GetAccessToken();
            if (string.IsNullOrEmpty(token))
            {
                MessageBox.Show("Please login first!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = App.Services.GetService<JoinGroupDialog>();
            if (dialog == null)
            {
                MessageBox.Show("Dialog service not registered in DI container.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            dialog.OnGroupJoined = async () => await LoadGroupsAsync();
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
        }

        // ==========================================================
        // GỬI TIN NHẮN
        // ==========================================================
        private async void SendMessageButton_Click(object sender, RoutedEventArgs e)
        {
            await SendMessageAsync(sender);
        }

        private async void MessageInputBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (!System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift))
                {
                    e.Handled = true;
                    await SendMessageAsync(SendMessageButton);
                }
            }
            else
            {
                await _chatViewModel.SendTypingAsync();
            }
        }

        private async Task SendMessageAsync(object sender)
        {
            var message = MessageInputBox.Text.Trim();

            if (string.IsNullOrEmpty(message))
            {
                MessageInputBox.Focus();
                return;
            }

            if (_chatViewModel.CurrentGroupId == Guid.Empty)
            {
                MessageBox.Show("Vui lòng chọn nhóm để chat!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var btn = sender as Button;

            try
            {
                if (btn != null) btn.IsEnabled = false;

                MessageInputBox.Clear();
                MessageInputBox.Focus();


                // 🔥 Nếu nội dung là file → gửi file
                if (IsValidFile(message))
                {
                    await _chatViewModel.SendFileAsync(message);
                }
                else
                {
                    // 🔥 Không phải file → gửi text
                    await _chatViewModel.SendTextAsync(message);
                }
            }
            catch (Exception ex)
            {
                MessageInputBox.Text = message;
                MessageInputBox.CaretIndex = message.Length;
                MessageBox.Show($"Gửi thất bại: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }
        private bool IsValidFile(string path)
        {
            if (!File.Exists(path)) return false;

            string ext = Path.GetExtension(path).ToLower();
            return _allowedExtensions.Contains(ext);
        }

        private static readonly string[] _allowedExtensions =
        {
             ".jpg", ".jpeg", ".png", ".gif",".mp4",".pdf",".docx"
        };


        // ==========================================================
        // UI HELPERS
        // ==========================================================
        private void ScrollToBottom()
        {
            var scrollViewer = FindVisualChild<ScrollViewer>(ChatMessagesPanel.Parent as DependencyObject);
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToEnd();
            }
        }

        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;

                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }

        private UIElement CreateMessageBubble(ChatMessage msg)
        {
            // Màu sắc giao diện
            var myBubbleColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0084FF"));
            var otherBubbleColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0F0F0"));
            var myTextColor = Brushes.White;
            var otherTextColor = Brushes.Black;

            var container = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = msg.IsMyMessage ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                Margin = new Thickness(0, 4, 0, 4)
            };

            // 1. Hiện tên người gửi (nếu là người khác)
            if (!msg.IsMyMessage)
            {
                var nameBlock = new TextBlock
                {
                    Text = msg.UserName,
                    FontSize = 11,
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(10, 0, 0, 2)
                };
                container.Children.Add(nameBlock);
            }

            // =========================================================
            // 2. XỬ LÝ IMAGE (Đã sửa lại để dùng HttpClient + Ngrok)
            // =========================================================
            if (msg.TypeMessage == "IMAGE")
            {
                Debug.WriteLine($"[UI] This is an IMAGE message");

                //  CÓ FileUrl → Hiển thị ảnh
                if (!string.IsNullOrEmpty(msg.FileUrl))
                {
                    var imageBorder = new Border
                    {
                        CornerRadius = new CornerRadius(12),
                        MaxWidth = 300,
                        MaxHeight = 300,
                        Background = Brushes.Transparent, // Hoặc màu xám nhạt làm placeholder
                        Margin = new Thickness(5, 2, 5, 2),
                        Cursor = Cursors.Hand,
                        HorizontalAlignment = msg.IsMyMessage ? HorizontalAlignment.Right : HorizontalAlignment.Left
                    };

                    var imageControl = new Image
                    {
                        Stretch = Stretch.Uniform,
                        MaxWidth = 300,
                        MaxHeight = 300
                    };

                    // Thêm Image vào Border ngay lập tức (dù chưa có dữ liệu)
                    imageBorder.Child = imageControl;
                    container.Children.Add(imageBorder);

                    // --- BẮT ĐẦU TẢI ẢNH BẤT ĐỒNG BỘ (FIRE & FORGET) ---
                    // Gọi hàm async nhưng không await để không chặn UI thread
                    _ = LoadImageAsync(imageControl, msg.FileUrl);

                    // Sự kiện click mở ảnh
                    imageBorder.MouseLeftButtonDown += (s, e) => OpenLink(msg.FileUrl);

                    // Thêm caption nếu có (logic cũ của bạn)
                    AddCaptionIfExist(container, msg, myBubbleColor, otherBubbleColor, myTextColor, otherTextColor);

                    return container;
                }
                else
                {
                    // ❌ KHÔNG CÓ FileUrl → Hiển thị placeholder
                    Debug.WriteLine($"[UI]  No FileUrl, showing placeholder");

                    var placeholderBubble = new Border
                    {
                        CornerRadius = new CornerRadius(18),
                        Background = msg.IsMyMessage ? myBubbleColor : otherBubbleColor,
                        Padding = new Thickness(12, 8, 12, 8),
                        Margin = new Thickness(5, 2, 5, 2),
                        MaxWidth = 300,
                        HorizontalAlignment = msg.IsMyMessage ? HorizontalAlignment.Right : HorizontalAlignment.Left
                    };

                    var placeholderPanel = new StackPanel();

                    // Icon + Text
                    var iconText = new TextBlock
                    {
                        Text = "📷 Image",
                        FontSize = 16,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = msg.IsMyMessage ? myTextColor : otherTextColor,
                        Margin = new Thickness(0, 0, 0, 4)
                    };
                    placeholderPanel.Children.Add(iconText);

                    // Filename
                    var fileNameText = new TextBlock
                    {
                        Text = msg.FileName ?? msg.Content ?? "Unknown",
                        FontSize = 13,
                        Foreground = msg.IsMyMessage ? myTextColor : otherTextColor,
                        TextWrapping = TextWrapping.Wrap
                    };
                    placeholderPanel.Children.Add(fileNameText);

                    // Warning
                    var warningText = new TextBlock
                    {
                        Text = " URL not available",
                        FontSize = 11,
                        Foreground = Brushes.Orange,
                        Margin = new Thickness(0, 4, 0, 0)
                    };
                    placeholderPanel.Children.Add(warningText);

                    placeholderBubble.Child = placeholderPanel;
                    container.Children.Add(placeholderBubble);
                    return container;
                }
            }

            //  KIỂM TRA FILE KHÁC (VIDEO, DOCUMENT)
            if (msg.TypeMessage != "TEXT" && !string.IsNullOrEmpty(msg.FileUrl))
            {
                var fileBubble = new Border
                {
                    CornerRadius = new CornerRadius(18),
                    Background = msg.IsMyMessage ? myBubbleColor : otherBubbleColor,
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(5, 2, 5, 2),
                    MaxWidth = 300,
                    HorizontalAlignment = msg.IsMyMessage ? HorizontalAlignment.Right : HorizontalAlignment.Left
                };

                var filePanel = new StackPanel { Orientation = Orientation.Horizontal };

                var icon = msg.TypeMessage switch
                {
                    "VIDEO" => "🎥",
                    "DOCUMENT" => "📄",
                    _ => "📎"
                };

                filePanel.Children.Add(new TextBlock
                {
                    Text = icon,
                    FontSize = 24,
                    Margin = new Thickness(0, 0, 12, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });

                var textStack = new StackPanel();
                textStack.Children.Add(new TextBlock
                {
                    Text = msg.FileName ?? msg.Content ?? "File",
                    FontWeight = FontWeights.SemiBold,
                    Foreground = msg.IsMyMessage ? myTextColor : otherTextColor,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxWidth = 200
                });

                var downloadBtn = new TextBlock
                {
                    Text = "⬇ Download / Open",
                    FontSize = 11,
                    Foreground = msg.IsMyMessage ? myTextColor : new SolidColorBrush(Colors.Blue),
                    TextDecorations = TextDecorations.Underline,
                    Cursor = Cursors.Hand,
                    Margin = new Thickness(0, 4, 0, 0)
                };

                downloadBtn.MouseLeftButtonDown += (s, e) => OpenLink(msg.FileUrl);

                textStack.Children.Add(downloadBtn);
                filePanel.Children.Add(textStack);
                fileBubble.Child = filePanel;
                container.Children.Add(fileBubble);

                return container;
            }

            // ✅ TIN NHẮN TEXT BÌNH THƯỜNG
            Debug.WriteLine($"[UI] Creating text bubble");

            var bubble = new Border
            {
                CornerRadius = new CornerRadius(18),
                Background = msg.IsMyMessage ? myBubbleColor : otherBubbleColor,
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(5, 2, 5, 2),
                MaxWidth = 400,
                HorizontalAlignment = msg.IsMyMessage ? HorizontalAlignment.Right : HorizontalAlignment.Left
            };

            var textBlock = new TextBlock
            {
                Text = msg.Content ?? "",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14,
                Foreground = msg.IsMyMessage ? myTextColor : otherTextColor
            };
            bubble.Child = textBlock;

            container.Children.Add(bubble);
            return container;

            // =========================================================
            // LOCAL HELPER FUNCTIONS (HÀM HỖ TRỢ NỘI BỘ)
            // =========================================================

            // Hàm tải ảnh chạy ngầm để không chặn UI
            async Task LoadImageAsync(Image imgTarget, string url)
            {
                try
                {
                    string cleanUrl = url?.Trim();
                    if (string.IsNullOrEmpty(cleanUrl)) return;

                    // Lấy Client từ DI (đã cấu hình header Ngrok)
                    var httpClientFactory = App.Services.GetService<IHttpClientFactory>();
                    var client = httpClientFactory.CreateClient("AuthorizedClient");

                    // Tải dữ liệu
                    byte[] imageBytes = await client.GetByteArrayAsync(cleanUrl);

                    // Quay lại UI Thread để gán ảnh
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        using (var stream = new MemoryStream(imageBytes))
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad; // Tải xong đóng stream ngay
                            bitmap.StreamSource = stream;
                            bitmap.EndInit();
                            bitmap.Freeze(); // Đóng băng để dùng được trên UI thread

                            imgTarget.Source = bitmap;
                        }
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[UI] Failed to load image: {ex.Message}");
                    // Optional: Set ảnh lỗi mặc định tại đây nếu muốn
                }
            }

            // Hàm mở link (tránh lặp code)
            void OpenLink(string url)
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Cannot open link: " + ex.Message);
                }
            }

            // Hàm thêm caption cho ảnh
            void AddCaptionIfExist(StackPanel parent, ChatMessage message, Brush myBg, Brush otherBg, Brush myTx, Brush otherTx)
            {
                if (!string.IsNullOrEmpty(message.Content) &&
                    message.Content != message.FileName &&
                    !message.Content.StartsWith("📎"))
                {
                    var captionBubble = new Border
                    {
                        CornerRadius = new CornerRadius(18),
                        Background = message.IsMyMessage ? myBg : otherBg,
                        Padding = new Thickness(12, 8, 12, 8),
                        Margin = new Thickness(5, 2, 5, 2),
                        MaxWidth = 300,
                        HorizontalAlignment = message.IsMyMessage ? HorizontalAlignment.Right : HorizontalAlignment.Left
                    };

                    var captionText = new TextBlock
                    {
                        Text = message.Content,
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 14,
                        Foreground = message.IsMyMessage ? myTx : otherTx
                    };
                    captionBubble.Child = captionText;
                    parent.Children.Add(captionBubble);
                }
            }

            // Hàm tạo bubble lỗi/placeholder
            Border CreatePlaceholderBubble(ChatMessage message, Brush myBg, Brush otherBg, Brush myTx, Brush otherTx)
            {
                var placeholderBubble = new Border
                {
                    CornerRadius = new CornerRadius(18),
                    Background = message.IsMyMessage ? myBg : otherBg,
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(5, 2, 5, 2),
                    MaxWidth = 300,
                    HorizontalAlignment = message.IsMyMessage ? HorizontalAlignment.Right : HorizontalAlignment.Left
                };

                var placeholderPanel = new StackPanel();
                placeholderPanel.Children.Add(new TextBlock
                {
                    Text = "📷 Image",
                    FontSize = 16,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = message.IsMyMessage ? myTx : otherTx,
                    Margin = new Thickness(0, 0, 0, 4)
                });

                placeholderPanel.Children.Add(new TextBlock
                {
                    Text = message.FileName ?? message.Content ?? "Unknown",
                    FontSize = 13,
                    Foreground = message.IsMyMessage ? myTx : otherTx,
                    TextWrapping = TextWrapping.Wrap
                });
                placeholderPanel.Children.Add(new TextBlock
                {
                    Text = " URL not available",
                    FontSize = 11,
                    Foreground = Brushes.Orange,
                    Margin = new Thickness(0, 4, 0, 0)
                });

                placeholderBubble.Child = placeholderPanel;
                return placeholderBubble;
            }
        }



        private async void click_sendfile(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_chatViewModel.CurrentGroupId == Guid.Empty)
                {
                    MessageBox.Show("Vui lòng chọn nhóm để gửi file!", "Thông báo",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                //  CHỈ CHO PHÉP 7 LOẠI FILE
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Select file to send",
                    Filter = "Images (*.jpg;*.jpeg;*.png;*.gif)|*.jpg;*.jpeg;*.png;*.gif|" +
                             "Videos (*.mp4)|*.mp4|" +
                             "PDF Documents (*.pdf)|*.pdf|" +
                             "Word Documents (*.docx)|*.docx|" +
                             "All Supported Files|*.jpg;*.jpeg;*.png;*.gif;*.mp4;*.pdf;*.docx",
                    FilterIndex = 5, // Default: All Supported Files
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var filePath = openFileDialog.FileName;
                    var fileInfo = new System.IO.FileInfo(filePath);

                    // Kiểm tra kích thước file (max 50MB)
                    const long maxFileSize = 50 * 1024 * 1024; // 50MB
                    if (fileInfo.Length > maxFileSize)
                    {
                        MessageBox.Show(
                            $"File quá lớn! Kích thước tối đa: 50MB\nFile của bạn: {fileInfo.Length / 1024 / 1024}MB",
                            "Lỗi",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    // Disable button và hiển thị loading
                    var btn = sender as Button;
                    if (btn != null)
                    {
                        btn.IsEnabled = false;
                        btn.Content = "⏳"; // Loading icon
                    }

                    try
                    {
                        // Gửi file qua ViewModel
                        await _chatViewModel.SendFileAsync(filePath);
                    }
                    finally
                    {
                        // Re-enable button
                        if (btn != null)
                        {
                            btn.IsEnabled = true;
                            btn.Content = "📎";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi gửi file: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"[UI] Send file error: {ex.Message}");
            }
        }

        //  Helper
        private bool IsImageFile(string extension)
        {
            string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
            return imageExtensions.Contains(extension.ToLower());
        }
    }
}