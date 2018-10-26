using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace BPiDLab2
{
    /// <summary>
    /// Interaction logic for BRWindow.xaml
    /// </summary>
    public partial class BRWindow : Window
    {
        private WebSocket socket;
        private WebSocketServer socketServer;
        private JavaScriptSerializer json = new JavaScriptSerializer();
        public BRWindow()
        {
            InitializeComponent();
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(socket.IsAlive.ToString());
            socket.Send(T1.Text);
        }

        private void ServerButtonClick(object sender, RoutedEventArgs e)
        {
            int port = int.Parse(PortInput.Text);
            socketServer = new WebSocketServer(port);
            socketServer.AddWebSocketService("/", () => new Xbehaviour() { window = this });
            socketServer.Start();
        }

        private void ClientButtonClick(object sender, RoutedEventArgs e)
        {
            string port = ClientPortInput.Text;
            socket = new WebSocket($"ws://localhost:{port}/");
            socket.Connect();
        }

        private void Print(string text)
        {
            this.Dispatcher.BeginInvoke((Action)(() => OutTextBox.Text = text));
        }
    }

    class Xbehaviour : WebSocketBehavior
    {
        public BRWindow window;
        public Xbehaviour()
        {

        }
        protected override void OnMessage(MessageEventArgs e)
        {
            MessageBox.Show(e.Data);
            string msg = e.Data; /*json.Deserialize<string>(e.Data);*/
            window.Dispatcher.BeginInvoke((Action)(() => window.OutTextBox.Text = msg + new Random().Next(0, 10)));
        }
    }
}
