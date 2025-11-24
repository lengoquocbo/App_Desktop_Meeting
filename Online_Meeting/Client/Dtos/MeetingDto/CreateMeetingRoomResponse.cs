using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Online_Meeting.Client.Dtos.MeetingDto
{
    public class CreateMeetingRoomResponse
    {
        public Guid Id { get; set; }
        public string RoomKey { get; set; }
        public string RoomName { get; set; }
        public int Max { get; set; }
        public DateTime CreateAt { get; set; }
        public string JoinUrl { get; set; }
        public string CreatedBy { get; set; }
    }
}
