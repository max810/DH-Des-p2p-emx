using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Security.Cryptography;
using System.Net.Http;
using System.Numerics;
using System.Collections.Generic;
using System.Windows.Documents;
using System.Windows.Media;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Web.Script.Serialization;
using System.Threading.Tasks;
using System.Threading;

namespace BPiDLab2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int KeyLength = 2048;
        private WebSocket socketMiddle;
        private WebSocketServer socketServer;
        private JavaScriptSerializer json = new JavaScriptSerializer();
        private RSACryptoServiceProvider rsa;
        private DESCryptoServiceProvider des;
        private string serverUrl = "ws://localhost:5000";
        private string broadcastUrlF = "ws://localhost:{0}/msg";
        private string MyName;
        private List<User> Users = new List<User>();
        private Dictionary<string, Action<Message>> MessageHandlers;
        //private Dictionary<string, string> UserAddresses;
        private Dictionary<string, WebSocket> UserSockets = new Dictionary<string, WebSocket>();


        public MainWindow()
        {
            // 1 client, 1 server, 1 app ))
            InitializeComponent();
            MessageHandlers = new Dictionary<string, Action<Message>>()
            {
                { "user_name_available", OnUsernameAvailable },
                { "registration_success", OnRegistrationSuccess },
                { "client_joined_chat", OnClientJoinedChat },
                { "client_left_chat", OnClientLeftChat },
                { "chat_message", OnChatMessageReceived },
                { "user_name_taken", OnUsernameTaken }
            };

            socketMiddle = new WebSocket(serverUrl);
            InitializeSocket(socketMiddle);
            socketMiddle.Connect();
        }

        private void InitializeSocket(WebSocket socket)
        {
            socket.OnMessage += (sender, e) => OnMessage(e);
        }

        private void OnMessage(MessageEventArgs e)
        {
            Message msg = json.Deserialize<Message>(e.Data);
            var handler = MessageHandlers[msg.event_type];
            handler(msg);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            socketMiddle.Close(CloseStatusCode.Normal);
        }

        private void CheckUsernameButtonClick(object sender, RoutedEventArgs e)
        {
            string userName = UserNameInput.Text;
            UserNameInput.IsReadOnly = true;
            CheckUsernameButton.IsEnabled = false;
            MyName = UserNameInput.Text;

            UsernameLabel.Content = "ПРОВЕРЯЕМ НИК...";

            SendCheckUsername(userName);
        }

        private void PrintMessage(string userName, string message, bool useSystemColor = false)
        {
            Paragraph p = new Paragraph();
            Run user = new Run($"@{userName}: ")
            {
                FontWeight = FontWeights.Bold,
                Foreground = useSystemColor ? Brushes.DarkRed : Brushes.DarkGreen,
            };

            Run msg = new Run(message);

            p.Inlines.Add(user);
            p.Inlines.Add(msg);
            Output.Document.Blocks.Add(p);
            Output.ScrollToEnd();
        }

        private void SendMessageButtonClick(object sender, EventArgs e)
        {
            string userName = MyName;
            string message = Input.Text;

            SendChatMessage(userName, message);
            ChangeUIThreadSafe(() => Input.Text = "");
        }

        private void SendChatMessage(string userName, string chatTextMessage)
        {
            //List<string> messagesb64 = new List<string>();
            //byte[] textDecoded = Encoding.UTF8.GetBytes(chatTextMessage);
            //while (textDecoded.Any())
            //{
            //    byte[] block = textDecoded.Take(245).ToArray();
            //    byte[] blockEncrypted = Encrypt(block, serverPbkParameters);
            //    messagesb64.Add(Convert.ToBase64String(blockEncrypted));
            //    textDecoded = textDecoded.Skip(245).ToArray();
            //}

            //string totalTextb64 = string.Join(Environment.NewLine, messagesb64);
            //ChangeUIThreadSafe(() => ClientEncryptedOutput.Text = totalTextb64);

            //clientPrkParameters = GenerateNewPrk();
            //string[] clientPbkB64 = PbkToBase64(clientPrkParameters);
            var message = new Message("chat_message")
            {
                data = new Dictionary<string, string>
                {
                    { "user_name",  userName },
                    { "message", chatTextMessage },
                }
            };

            foreach(var kv in UserSockets)
            {
                var socket = kv.Value;
                SendMessage(message, socket);
            }
            ChangeUIThreadSafe(() => PrintMessage(userName + "[YOU]", chatTextMessage, useSystemColor: true));
        }

        private void SendCheckUsername(string userName)
        {
            var message = new Message("check_user_name")
            {
                data = new Dictionary<string, string>
                {
                    { "user_name", userName }
                }
            };

            SendMessage(message, socketMiddle);
        }

        private void SendMessageBroadcast(Message message)
        {
            string data = json.Serialize(message);
            //foreach(var socket in UserSockets.Values)
            //{
            //    socket.Send(data);
            //}
            socketServer.WebSocketServices.Broadcast(data);

        }

        private void SendMessage(Message message, WebSocket socket)
        {
            string data = json.Serialize(message);
            socket.Send(data);
        }

        private void OnUsernameAvailable(Message msg)
        {
            Message message = new Message("register")
            {
                data = new Dictionary<string, string>
                {
                    { "user_name", MyName },
                }
            };

            SendMessage(message, socketMiddle);
        }


        private void OnUsernameTaken(Message msg)
        {
            ChangeUIThreadSafe(() =>
            {
                UserNameInput.IsReadOnly = false;
                UsernameLabel.Content = "НИК ЗАНЯТ. Другой избери";
                UserNameInput.SelectAll();
                CheckUsernameButton.IsEnabled = true;
            });
        }

        private void ShowMainUI()
        {
            UsernameUI.Visibility = Visibility.Hidden;
            MainUI.Visibility = Visibility.Visible;
            UserNameInput.Visibility = Visibility.Visible;
            MyName = UserNameInput.Text;
            UsernameBoxReadonly.Text = MyName;
        }

        private void OnRegistrationSuccess(Message msg)
        {
            string port = msg.data["port"];
            //string myServerUrl = string.Format(broadcastUrlF, port);
            socketServer = new WebSocketServer($"ws://localhost:{port}");
            socketServer.AddWebSocketService("/msg", () => new ActionBehaviour(OnMessage));
            socketServer.Start();

            var users = json.Deserialize<UserServerFormatted[]>(msg.data["users"]);
            foreach (var user in users)
            {
                User usr = user;
                var socket = new WebSocket(string.Format(broadcastUrlF, usr.Port));
                InitializeSocket(socket);
                UserSockets[usr.UserName] = socket;
                socket.OnOpen += (sender, e) =>
                {
                    var message = new Message("client_joined_chat")
                    {
                        data = new Dictionary<string, string>
                        {
                            { "user_name", MyName },
                            { "port", port }
                        }
                    };

                    SendMessage(message, socket);
                };
                socket.Connect();
                // TODO DIFFIE HELLMAN HERE
            }
            ChangeUIThreadSafe(ShowMainUI);
        }


        private void OnClientJoinedChat(Message msg)
        {
            string userName = msg.data["user_name"];
            string port = msg.data["port"];
            var socket = new WebSocket(string.Format(broadcastUrlF, port));
            UserSockets[userName] = socket;
            socket.Connect();
            ChangeUIThreadSafe(() => PrintMessage(userName, " has joined chat.", useSystemColor: true));
        }


        private void OnClientLeftChat(Message msg)
        {
            string userName = msg.data["user_name"];
            ChangeUIThreadSafe(() => PrintMessage(userName, " has left chat.", useSystemColor: true));
        }


        private void OnChatMessageReceived(Message msg)
        {
            string userName = msg.data["user_name"];
            string message = msg.data["message"];
            ChangeUIThreadSafe(() => PrintMessage(userName, message));
        }

        private void ChangeUIThreadSafe(Action action)
        {
            this.Dispatcher.BeginInvoke(action);
        }
    }
}
