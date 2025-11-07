using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Online_Meeting.Client.Dtos.MeetingDto
{
    public class UpdateRoomRequest
    {
        public string? RoomName { get; set; }
        public int? MaxParticipants { get; set; }
        public bool? IsActive { get; set; }
    }
}
