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
        public string[] message_blocks = { };
        public Dictionary<string, string> data = new Dictionary<string, string> { };

        public Message()
        {

        }

        public Message(string _event_type)
        {
            event_type = _event_type;
        }


        //public Message(string _event_type, IEnumerable<string> _message_blocks, Dictionary<string, string> _data)
        //{
        //    event_type = _event_type;
        //    message_blocks = _message_blocks.ToArray();
        //    data = _data;
        //}
    }
}
