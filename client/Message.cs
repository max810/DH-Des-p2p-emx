using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPiDLab2
{
    public class Message
    {
        public string event_type;
        public Dictionary<string, string> data;


        public Message()
        {

        }

        public Message(string _event_type, Dictionary<string, string> _data)
        {
            event_type = _event_type;
            data = _data;
        }
    }
}
