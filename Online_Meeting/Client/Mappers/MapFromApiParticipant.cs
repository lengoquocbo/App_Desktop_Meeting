using Online_Meeting.Client.Dtos.MeetingDto;
using Online_Meeting.Client.Dtos.MeetingSignalRDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Online_Meeting.Client.Mappers
{
    internal class MapFromApiParticipant
    {
        public List<ParticipantInfo> MapParticipant(ParticipantsResponse ps)
        {
            var result = new List<ParticipantInfo>();
            foreach (var p in ps.Data)
            {
                result.Add(new ParticipantInfo
                {
                    ConnectionId = null,
                    UserId = p.UserId,
                    Username = p.Username
                });
            }
            return result;
        }
    }
}
