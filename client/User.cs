using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPiDLab2
{
    class User
    {
        public string UserName;
        public int Port;

        public User(string userName, int port)
        {
            UserName = userName;
            Port = port;
        }
    }
}
