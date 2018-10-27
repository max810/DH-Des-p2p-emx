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
        private JavaScriptSerializer json = new JavaScriptSerializer();
        private DHprovider dh = new DHprovider();
        private int keyCounts = 0;
        private DES des;

        private WebSocket socketMiddle;
        private WebSocketServer socketServer;

        private string serverUrl = "ws://localhost:5000";
        private string broadcastUrlF = "ws://localhost:{0}/msg";
        private string MyName;

        private LinkedList<User> Users = new LinkedList<User>();
        private LinkedListNode<User> MyUserPosition;
        private Dictionary<string, Action<Message>> MessageHandlers;
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
                { "user_name_taken", OnUsernameTaken },
                { "key", OnKeyReceived }
            };

            socketMiddle = new WebSocket(serverUrl);
            socketMiddle.WaitTime = TimeSpan.FromMinutes(60.0);
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
            socketServer.Stop();
            socketMiddle.Close(CloseStatusCode.Normal);
            foreach(var socket in UserSockets.Values)
            {
                socket.Close(CloseStatusCode.Normal);
            }
            Environment.Exit(0);
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

            SendBroadcastMessage("chat_message", message);
            ChangeUIThreadSafe(() => Input.Text = "");
        }

        // RESERVED FOR DMs
        private void SendPersonalMessage(string messageType, string userName, string text)
        {
            WebSocket socket = UserSockets[userName];
            var message = new Message(messageType)
                .WithUserName(userName)
                .WithTextMessage(text);
            //    new Message(messageType)
            //{
            //    data = new Dictionary<string, string>
            //    {
            //        { "user_name",  MyName },
            //        { "message", text },
            //    }
            //};

            SendMessage(message, socket);
        }

        private void SendBroadcastMessage(string messageType, string text)
        {
            string textEncB64 = EncryptText(text);
            ChangeUIThreadSafe(() => EncryptedMessageSent.Text = textEncB64);
            var message = Message.CreateChatMessage(messageType, MyName, textEncB64);
            //var message = new Message(messageType)
            //{
            //    data = new Dictionary<string, string>
            //    {
            //        { "user_name",  MyName },
            //        { "message", text },
            //    }
            //};
            foreach (var kv in UserSockets)
            {
                var socket = kv.Value;
                SendMessage(message, socket);
            }

            ChangeUIThreadSafe(() => PrintMessage(MyName + "[YOU]", text, useSystemColor: true));
        }

        private void SendCheckUsername(string userName)
        {
            var message = new Message("check_user_name")
                .WithUserName(userName);
            //    new Message("check_user_name")
            //{
            //    data = new Dictionary<string, string>
            //    {
            //        { "user_name", userName }
            //    }
            //};

            SendMessage(message, socketMiddle);
        }

        private void SendMessage(Message message, WebSocket socket)
        {
            string data = json.Serialize(message);
            socket.SendAsync(data, x => { if (!x) MessageBox.Show("PIZDEC"); } );
        }

        private void OnUsernameAvailable(Message msg)
        {
            Message message = new Message("register").WithUserName(MyName);
            //{
            //    data = new Dictionary<string, string>
            //    {
            //        { "user_name", MyName },
            //    }
            //};

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
            socketServer = new WebSocketServer($"ws://localhost:{port}");
            socketServer.WaitTime = TimeSpan.FromMinutes(60.0);
            socketServer.AddWebSocketService("/msg", () => new ActionBehaviour(OnMessage));
            socketServer.Start();

            var users = json.Deserialize<UserServerFormatted[]>(msg.data["users"]);
            foreach (var user in users)
            {
                User usr = user;
                Users.AddLast(usr);
                var socket = new WebSocket(string.Format(broadcastUrlF, usr.Port));
                socket.WaitTime = TimeSpan.FromMinutes(60.0);
                //InitializeSocket(socket);
                UserSockets[usr.UserName] = socket;
                //socket.OnOpen += (sender, e) =>
                //{
                //    var message = new Message("client_joined_chat")
                //    {
                //        data = new Dictionary<string, string>
                //        {
                //            { "user_name", MyName },
                //            { "port", port }
                //        }
                //    };

                //    SendMessage(message, socket);
                //};
                socket.Connect();
            }
            var joinedMessage = new Message("client_joined_chat")
                .WithUserName(MyName)
                .With("port", port);
            foreach(var socket in UserSockets.Values)
            {
                SendMessage(joinedMessage, socket); 
            }
            MyUserPosition = Users.AddLast(new User(MyName, socketServer.Port));
            ChangeUIThreadSafe(ShowMainUI);
            if (Users.Count > 1)
            {
                ChangeUIThreadSafe(() => SetReadyIndicator(false));
                ExchangeInitialKey();
            }
        }

        private void OnKeyReceived(Message msg)
        {
            string keyPartString = msg.data["message"];
            var keyPart = BigInteger.Parse(keyPartString);
            var nextKeyPart = dh.Compute(keyPart);
            keyCounts++;
            if (keyCounts == Users.Count)
            {
                des = new DES((int)nextKeyPart);
                ChangeUIThreadSafe(() => SetReadyIndicator(true));
                keyCounts = 0;
                string keyString = Convert.ToBase64String(des.Key);
                ChangeUIThreadSafe(() => KeyBox.Text = keyString);
                return;
            }

            var nextUser = (MyUserPosition.Next ?? Users.First).Value;
            SendPersonalMessage("key", nextUser.UserName, nextKeyPart.ToString());
        }

        private void SetReadyIndicator(bool isReady)
        {
            if (isReady)
            {
                ReadyIndicator.Fill = new SolidColorBrush(Color.FromRgb(0, 238, 0));

                ReadyMessage.Text = "Ready";
                SendMessageButton.IsEnabled = true;
            }
            else
            {
                ReadyIndicator.Fill = new SolidColorBrush(Color.FromRgb(238, 0, 0));

                ReadyMessage.Text = "Key exchange";
                SendMessageButton.IsEnabled = false;
            }
        }

        private void OnClientJoinedChat(Message msg)
        {
            // на 4 ебнулось
            string userName = msg.data["user_name"];
            string port = msg.data["port"];
            string url = string.Format(broadcastUrlF, port);
            var socket = new WebSocket(url);
            socket.WaitTime = TimeSpan.FromMinutes(60.0);
            var user = new User(userName, int.Parse(port));
            Users.AddLast(user);
            UserSockets[userName] = socket;
            socket.OnOpen += (sender, e) =>
            {
                ChangeUIThreadSafe(() => PrintMessage(userName, " has joined chat.", useSystemColor: true));
                ChangeUIThreadSafe(() => SetReadyIndicator(false));
                ExchangeInitialKey();
            };

            socket.Connect();
        }

        private void ExchangeInitialKey()
        {
            var initialKeyPart = dh.ComputeInitial();
            var nextUser = (MyUserPosition.Next ?? Users.First).Value;
            SendPersonalMessage("key", nextUser.UserName, initialKeyPart.ToString());
            keyCounts++;
        }

        private void OnClientLeftChat(Message msg)
        {
            string userName = msg.data["user_name"];
            User userLeft = Users.First(x => x.UserName == userName);
            Users.Remove(userLeft);

            ChangeUIThreadSafe(() => PrintMessage(userName, " has left chat.", useSystemColor: true));
        }


        private void OnChatMessageReceived(Message msg)
        {
            string userName = msg.data["user_name"];
            string messageEncB64 = msg.data["message"];
            ChangeUIThreadSafe(() => EncryptedMessageReceived.Text = messageEncB64);
            string messageDec = DecryptText(messageEncB64);
            //byte[] messageBytes = des.Decrypt(Convert.FromBase64String(messageEncB64));
            //string messageDec = Encoding.UTF8.GetString(messageBytes);

            ChangeUIThreadSafe(() => PrintMessage(userName, messageDec));
        }

        private string EncryptText(string text)
        {
            return Convert.ToBase64String(des.Encrypt(Encoding.UTF8.GetBytes(text)));
        }

        private string DecryptText(string textEncB64)
        {
            return Encoding.UTF8.GetString(des.Decrypt(Convert.FromBase64String(textEncB64)));
        }

        private void ChangeUIThreadSafe(Action action)
        {
            this.Dispatcher.BeginInvoke(action);
        }
    }
}
