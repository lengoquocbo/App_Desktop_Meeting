using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Online_Meeting.Client.Dtos.MeetingSignalRDto
{
    public class IceCandidateData
    {
        public string FromConnectionId { get; set; }
        public object Candidate { get; set; }
    }
}
