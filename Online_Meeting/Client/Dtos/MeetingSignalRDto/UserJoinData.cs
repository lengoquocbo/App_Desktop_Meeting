using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Online_Meeting.Client.Dtos.MeetingSignalRDto
{
    internal class UserJoinData
    {
        public string ConnectionId { get; set; }
        public Guid UserId { get; set; }
        public string Username { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}
