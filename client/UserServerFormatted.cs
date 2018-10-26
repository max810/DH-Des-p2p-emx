using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPiDLab2
{
    class UserServerFormatted
    {
        public string user_name;
        public int port;

        public static implicit operator User(UserServerFormatted userServer)
        {
            return new User(userServer.user_name, userServer.port);
        }
    }
}
