using System.ComponentModel;       // Chứa INotifyPropertyChanged
using System.Runtime.CompilerServices; // Chứa CallerMemberName

namespace Online_Meeting.Client.ViewModels
{
    // Đây là class base cho tất cả các ViewModel
    // Nó giúp các property của ViewModel "thông báo" cho UI khi giá trị thay đổi
    public class ViewModelBase : INotifyPropertyChanged
    {
        // Event quan trọng của INotifyPropertyChanged
        // UI (Binding) sẽ subscribe event này để biết khi nào property thay đổi
        public event PropertyChangedEventHandler PropertyChanged;

        // Phương thức này dùng để raise event PropertyChanged
        // [CallerMemberName] tự động lấy tên property gọi OnPropertyChanged
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            // Nếu có UI subscribe event, nó sẽ được gọi
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Helper method để set giá trị property
        // Nó gán giá trị mới và gọi OnPropertyChanged nếu giá trị thực sự thay đổi
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            // Nếu giá trị mới bằng giá trị cũ thì không làm gì
            if (Equals(field, value)) return false;

            // Gán giá trị mới cho field
            field = value;

            // Thông báo cho UI biết property này đã thay đổi
            OnPropertyChanged(propertyName);

            // Trả về true để biết giá trị đã thay đổi thành công
            return true;
        }
    }
}
