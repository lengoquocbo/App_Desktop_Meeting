using Microsoft.Extensions.DependencyInjection;
using Online_Meeting.Client.Interfaces;
using Online_Meeting.Client.Models;
using Online_Meeting.Client.Services;
using Online_Meeting.Client.Views.Dialogs;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Online_Meeting.Client.Views.Pages
{
    public partial class GroupChatView : Page
    {
        private readonly ITokenService _token;
        private readonly IGroupService _groupService;

        public GroupChatView(TokenService tokenService, IGroupService groupService)
        {
            InitializeComponent();
            _token = tokenService;
            _groupService = groupService;

            // gọi khi Page Loaded
            Loaded += async (s, e) => await LoadGroupsAsync();
        }

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
            // Button
            var button = new Button
            {
                Style = (Style)FindResource("GroupItemButton"),
                Tag = group.GroupKey,
                Margin = new Thickness(0, 0, 0, 8)
            };
            button.Click += GroupItem_Click;

            // Grid layout
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Avatar (2 ký tự đầu từ GroupName)
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

            // Time (tạm thời để trống hoặc có thể hiển thị Id ngắn)
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

        private void GroupItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string groupKey)
            {
                MessageBox.Show($"Bạn đã chọn nhóm có GroupKey = {groupKey}");
            }
        }

        private void CreateGroupButton_Click(object sender, RoutedEventArgs e)
        {
            var token = _token.GetAccessToken();
            if (string.IsNullOrEmpty(token))
            {
                MessageBox.Show("Please login first!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Lấy dialog từ DI container
            var dialog = App.Services.GetService<CreateGroupDialog>();

            if (dialog == null)
            {
                MessageBox.Show("Dialog service not registered in DI container.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Gán callback reload danh sách nhóm sau khi tạo thành công
            dialog.OnGroupCreated = async () => await LoadGroupsAsync();

            // Gán owner để dialog hiển thị trung tâm cửa sổ chính
            dialog.Owner = Window.GetWindow(this);

            // Hiển thị dialog
            dialog.ShowDialog();
        }

    }
}
