using System;
using System.Windows;
using Online_Meeting.Client.Interfaces;
using Online_Meeting.Client.ViewModels;
using Online_Meeting.Client.Services;

namespace Online_Meeting.Client.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for JoinGroupDialog.xaml
    /// </summary>
    public partial class JoinGroupDialog : Window
    {
        private readonly JoinGroupViewModel _viewModel;
        private readonly ITokenService _token;

        /// <summary>
        /// Callback được gọi khi join group thành công
        /// </summary>
        public Action OnGroupJoined { get; set; }

        public JoinGroupDialog(TokenService token , JoinGroupViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _token = token;
            
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void JoinButton_Click(object sender, RoutedEventArgs e)
        {
            // Lấy text từ TextBox đưa vào ViewModel
            _viewModel.GroupId = GroupCodeTextBox.Text;

            // Gọi hàm join group
            var success = await _viewModel.JoinGroupAsync();

            if (success)
            {
                MessageBox.Show(
                    "Joined group successfully!",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                // Reload danh sách nhóm sau khi join
                OnGroupJoined?.Invoke();

                DialogResult = true;
                Close();
            }
        }

    }
}