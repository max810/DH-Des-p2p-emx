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
        public Dictionary<string, string> data = new Dictionary<string, string> { };

        public Message()
        {

        }

        public Message(string _event_type)
        {
            event_type = _event_type;
        }

        public Message WithUserName(string userName)
        {
            data["user_name"] = userName;

            return this;
        }

        public Message WithTextMessage(string message)
        {
            data["message"] = message;

            return this;
        }

        public Message With(string key, string value)
        {
            data[key] = value;

            return this;
        }

        public static Message CreateChatMessage(string messageType, string userName, string messageText)
        {
            return new Message(messageType)
                .WithUserName(userName)
                .WithTextMessage(messageText);
        }
    }
}
