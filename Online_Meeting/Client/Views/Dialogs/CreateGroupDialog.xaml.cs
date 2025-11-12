using Online_Meeting.Client.Interfaces;
using Online_Meeting.Client.Services;
using Online_Meeting.Client.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Online_Meeting.Client.Views.Dialogs
{
    public partial class CreateGroupDialog : Window
    {
        public Func<Task>? OnGroupCreated { get; set; } // ✅ Callback reload

        private readonly CreateGroupViewModel _viewModel;
        private readonly Random _rand = new Random();
        private readonly ITokenService _token;
        private readonly IGroupService _groupService;

        private readonly string[] colors =
        {
            "#4A90E2", "#E67E22", "#27AE60", "#9B59B6", "#E74C3C", "#16A085", "#8E44AD"
        };

        public CreateGroupDialog(TokenService token, IGroupService groupService)
        {
            InitializeComponent();

            _token = token ?? throw new ArgumentNullException(nameof(token));
            _groupService = groupService ?? throw new ArgumentNullException(nameof(groupService));

            if (string.IsNullOrEmpty(_token.GetAccessToken()))
            {
                MessageBox.Show("Token not found. Please login again.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            _viewModel = new CreateGroupViewModel(_groupService);
            DataContext = _viewModel;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
        private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();

        private async void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            string groupName = GroupNameTextBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(groupName))
            {
                MessageBox.Show("Please enter a group name.", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var button = sender as Button;
            if (button != null)
            {
                button.IsEnabled = false;
                button.Content = "Creating...";
            }

            try
            {
                var result = await _viewModel.CreateGroupAsync(groupName);

                if (result != null)
                {
                    MessageBox.Show(
                        $"✅ Group '{result.GroupName}' created successfully!\nGroup ID: {result.GroupId}",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // ✅ Gọi callback để GroupChatView reload danh sách
                    if (OnGroupCreated != null)
                        await OnGroupCreated.Invoke();

                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show(
                        _viewModel.ErrorMessage ?? "Failed to create group. Please try again.",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An unexpected error occurred: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                if (button != null)
                {
                    button.IsEnabled = true;
                    button.Content = "Create Group";
                }
            }
        }

        private void GroupNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string name = GroupNameTextBox.Text.Trim();

            if (string.IsNullOrEmpty(name))
            {
                AvatarText.Text = "📷";
                AvatarBorder.Background = new SolidColorBrush(Color.FromRgb(227, 242, 253));
                return;
            }

            string initials = GetInitials(name);
            AvatarText.Text = initials;
            AvatarBorder.Background = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(colors[_rand.Next(colors.Length)]));
        }

        private string GetInitials(string name)
        {
            var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) return "?";
            if (words.Length == 1) return words[0][0].ToString().ToUpper();
            if (name.ToLower().Contains("thương mại")) return "TM";
            return (words[0][0].ToString() + words[1][0]).ToUpper();
        }
    }
}
