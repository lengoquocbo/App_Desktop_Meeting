using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Online_Meeting.Client.Views.Pages
{
    public partial class GroupChatView : Page
    {
        public GroupChatView()
        {
            InitializeComponent();
        }

        private void CreateGroupButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Create new group clicked!");
        }

        private void GroupItem_Click(object sender, RoutedEventArgs e)
        {
         //   EmptyChatState.Visibility = Visibility.Collapsed;
          //  ChatContainer.Visibility = Visibility.Visible;
        }

        private void GroupInfoButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Group info clicked!");
        }

       // private void SendMessageButton_Click(object sender, RoutedEventArgs e)
       // {
        //    if (!string.IsNullOrWhiteSpace(MessageInputBox.Text))
          //  {
                // TODO: Send message logic
           //     MessageInputBox.Clear();
           // }
      //  }
    }
}