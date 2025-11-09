using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Online_Meeting.Client.ViewModels
{
    internal class ItemMeetingViewModel : INotifyPropertyChanged
    {

        private string _participantText;

        public Guid Id { get; set; }
        public string RoomName { get; set; }

        public string ParticipantText
        {
            get => _participantText;
            set { _participantText = value; OnPropertyChanged(nameof(ParticipantText)); }
        }

        public bool IsFull { get; set; }
        public bool CanJoin { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
