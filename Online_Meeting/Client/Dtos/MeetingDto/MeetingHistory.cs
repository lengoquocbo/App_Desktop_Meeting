using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Online_Meeting.Client.Dtos.MeetingDto
{
    public class MeetingHistory
    {
        public Guid Id { get; set; }
        public Guid RoomId { get; set; }
        public string RoomName { get; set; }
        public string RoomKey { get; set; }
        public string Role { get; set; }
        public DateTime JoinAt { get; set; }
        public DateTime? LeaveAt { get; set; }
        public double? Duration { get; set; }
    }
}
