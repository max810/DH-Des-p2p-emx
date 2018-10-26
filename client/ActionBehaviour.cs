using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace BPiDLab2
{
    class ActionBehaviour: WebSocketBehavior
    {
        private Action<MessageEventArgs> action;
        public ActionBehaviour(Action<MessageEventArgs> action)
        {
            this.action = action;
        }
        public ActionBehaviour()
        {

        }
        protected override void OnMessage(MessageEventArgs e)
        {
            action?.Invoke(e);
        }
    }
}
