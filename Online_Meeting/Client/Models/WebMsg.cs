using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Online_Meeting.Client.Models
{
    public class WebMsg
    {
        public string type { get; set; }
        public string msg { get; set; }
        public string toConnectionId { get; set; }
        public object offer { get; set; }
        public object answer { get; set; }
        public object candidate { get; set; }
        public bool audio { get; set; }
        public bool video { get; set; }
        public bool isSharingScreen { get; set; }
        public string content { get; set; }
    }
}
