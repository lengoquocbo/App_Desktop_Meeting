using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Online_Meeting.Client.Dtos.MeetingDto
{
    internal class ParticipantsResponse
    {
        public List<Participant> Data { get; set; }
        public int TotalCount { get; set; }
    }
}
