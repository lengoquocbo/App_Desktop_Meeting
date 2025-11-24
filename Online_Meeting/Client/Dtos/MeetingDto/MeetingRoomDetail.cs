using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Online_Meeting.Client.Dtos.MeetingDto
{
    public class MeetingRoomDetail
    {
        public Guid Id { get; set; }
        public string RoomKey { get; set; }
        public string RoomName { get; set; }
        public int Max { get; set; }
        public bool IsActive { get; set; }
        public int CurrentParticipants { get; set; }
        public bool? IsFull { get; set; }
        public bool? CanJoin { get; set; }
        public DateTime? CreateAt { get; set; }

    }
}
