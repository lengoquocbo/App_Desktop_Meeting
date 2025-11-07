using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Online_Meeting.Client.Dtos.MeetingDto
{
    public class MeetingRoom
    {
        public Guid Id { get; set; }
        public string RoomKey { get; set; }
        public string RoomName { get; set; }
        public int Max { get; set; }
        public DateTime CreateAt { get; set; }
        public bool IsActive { get; set; }
        public int CurrentParticipants { get; set; }
        public int AvailableSlots { get; set; }
    }
}
