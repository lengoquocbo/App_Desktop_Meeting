using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Online_Meeting.Client.Dtos.MeetingDto
{
    public class JoinMeetingResponse
    {
        public Guid Id { get; set; }
        public Guid RoomId { get; set; }
        public Guid UserId { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
        public DateTime JoinAt { get; set; }
        public RoomInfo Room { get; set; }
    }
}
