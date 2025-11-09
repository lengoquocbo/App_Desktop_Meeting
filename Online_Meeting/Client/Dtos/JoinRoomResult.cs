using Online_Meeting.Client.Dtos.MeetingSignalRDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Online_Meeting.Client.Dtos
{
    internal class JoinRoomResult
    {
        public Guid RoomId { get; set; }
        public string RoomName { get; set; }
        public string Role { get; set; }
        public List<ParticipantInfo> Participants { get; set; }

    }
}
