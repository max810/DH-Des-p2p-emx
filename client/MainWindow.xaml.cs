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
        private WebSocket socket;
        private JavaScriptSerializer json = new JavaScriptSerializer();
        private RSACryptoServiceProvider rsa;
        private string serverUrl = "ws://localhost:5000";
        private RSAParameters serverPbkParameters;
        private RSAParameters clientPrkParameters;
        private string Username;
        private Dictionary<string, Action<Message>> MessageHandlers;

        public MainWindow()
        {
            InitializeComponent();
            MessageHandlers = new Dictionary<string, Action<Message>>()
            {
                { "username_available", OnUsernameAvailable },
                { "registration_success", OnRegistrationSuccess },
                { "client_joined_chat", OnClientJoinedChat },
                { "client_left_chat", OnClientLeftChat },
                { "chat_message_received", OnChatMessageReceived },
                { "my_chat_message_received", OnMyChatMessageReceived },
                { "username_taken", OnUsernameTaken }
            };

            clientPrkParameters = GenerateNewPrk();

            socket = new WebSocket(serverUrl);
            socket.OnMessage += (sender, e) =>
            {
                Message msg = json.Deserialize<Message>(e.Data);
                var handler = MessageHandlers[msg.event_type];
                handler(msg);
            };

            socket.Connect();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            socket.Close(CloseStatusCode.Normal);
        }

        private void CheckUsernameButtonClick(object sender, RoutedEventArgs e)
        {
            string username = UsernameInput.Text;
            UsernameInput.IsReadOnly = true;
            CheckUsernameButton.IsEnabled = false;
            Username = UsernameInput.Text;

            UsernameLabel.Content = "ПРОВЕРЯЕМ НИК...";

            SendCheckUsername(username);
        }

        private void PrintMessage(string username, string message, bool useSystemColor = false)
        {
            Paragraph p = new Paragraph();
            Run user = new Run($"@{username}: ")
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
            string username = Username;
            string message = Input.Text;

            SendChatMessage(username, message);
            ChangeUIThreadSafe(() => Input.Text = "");
        }

        private void SendChatMessage(string username, string chatTextMessage)
        {
            List<string> messagesb64 = new List<string>();
            byte[] textDecoded = Encoding.UTF8.GetBytes(chatTextMessage);
            while (textDecoded.Any())
            {
                byte[] block = textDecoded.Take(245).ToArray();
                byte[] blockEncrypted = Encrypt(block, serverPbkParameters);
                messagesb64.Add(Convert.ToBase64String(blockEncrypted));
                textDecoded = textDecoded.Skip(245).ToArray();
            }

            string totalTextb64 = string.Join(Environment.NewLine, messagesb64);
            ChangeUIThreadSafe(() => ClientEncryptedOutput.Text = totalTextb64);

            clientPrkParameters = GenerateNewPrk();
            string[] clientPbkB64 = PbkToBase64(clientPrkParameters);
            var message = new Message("chat_message")
            {
                message_blocks = messagesb64.ToArray(),
                data = new Dictionary<string, string>
                {
                    { "e", clientPbkB64[0] },
                    { "n", clientPbkB64[1] },
                }
            };

            SendMessage(message);
        }

        private void SendCheckUsername(string username)
        {
            var message = new Message("check_username")
            {
                data = new Dictionary<string, string>
                {
                    { "username", username }
                }
            };

            SendMessage(message);
        }

        private void SendMessage(Message message)
        {
            string data = json.Serialize(message);
            socket.Send(data);

            //socket.Send(new string('c', 100_000));
        }

        private void OnUsernameAvailable(Message msg)
        {
            string[] pbk64 = PbkToBase64(clientPrkParameters);

            Message message = new Message("register")
            {
                data = new Dictionary<string, string>
                {
                    { "username", Username },
                    { "e", pbk64[0] },
                    { "n", pbk64[1] }
                }
            };

            SendMessage(message);

            ChangeUIThreadSafe(ShowMainUI);
        }

        private void OnUsernameTaken(Message msg)
        {
            ChangeUIThreadSafe(() =>
            {
                UsernameInput.IsReadOnly = false;
                UsernameLabel.Content = "НИК ЗАНЯТ. Другой избери";
                UsernameInput.SelectAll();
                CheckUsernameButton.IsEnabled = true;
            });
        }

        private void ShowMainUI()
        {
            UsernameUI.Visibility = Visibility.Hidden;
            MainUI.Visibility = Visibility.Visible;
            UsernameInput.Visibility = Visibility.Visible;
            Username = UsernameInput.Text;
            UsernameBoxReadonly.Text = Username;
        }

        private void OnRegistrationSuccess(Message msg)
        {
            SetNewServerPbk(msg.data);
        }


        private void OnClientJoinedChat(Message msg)
        {
            string username = msg.data["username"];
            ChangeUIThreadSafe(() => PrintMessage(username, " has joined chat.", useSystemColor: true));
        }


        private void OnClientLeftChat(Message msg)
        {
            string username = msg.data["username"];
            ChangeUIThreadSafe(() => PrintMessage(username, " has left chat.", useSystemColor: true));
        }


        private void OnChatMessageReceived(Message msg)
        {
            string username = msg.data["username"];
            string[] messageBlocksEncryptedB64 = msg.message_blocks;
            string allTextEncrypted = string.Join(Environment.NewLine, messageBlocksEncryptedB64);
            ChangeUIThreadSafe(() => ServerEncryptedOutput.Text = allTextEncrypted);

            List<byte[]> decryptedBlocks = new List<byte[]>();
            foreach (var block in messageBlocksEncryptedB64)
            {
                var blockBytes = Convert.FromBase64String(block);
                var bytesDecrypted = Decrypt(blockBytes, clientPrkParameters);
                decryptedBlocks.Add(bytesDecrypted);
            }

            // SelectMany == Flatten
            byte[] allBytesDecrypted = decryptedBlocks.SelectMany(x => x).ToArray();
            var msgDecrypted = Encoding.UTF8.GetString(allBytesDecrypted);
            ChangeUIThreadSafe(() => PrintMessage(username, msgDecrypted));
        }

        private void OnMyChatMessageReceived(Message msg)
        {
            SetNewServerPbk(msg.data);
            OnChatMessageReceived(msg);
        }

        private void SetNewServerPbk(Dictionary<string, string> data)
        {
            string e = data["e"];
            string n = data["n"];
            var serverpbk = PbkFromBase64(new[] { e, n });

            serverPbkParameters = serverpbk;
        }

        private string[] PbkToBase64(RSAParameters pbk)
        {
            string E = Convert.ToBase64String(pbk.Exponent);
            string N = Convert.ToBase64String(pbk.Modulus);

            return new[] { E, N };
        }

        private RSAParameters PbkFromBase64(string[] pbkb64)
        {
            string E = pbkb64[0];
            string N = pbkb64[1];

            return new RSAParameters()
            {
                Exponent = Convert.FromBase64String(E),
                Modulus = Convert.FromBase64String(N),
            };
        }

        private byte[] Encrypt(byte[] text, RSAParameters pbk)
        {
            rsa.ImportParameters(pbk);
            //byte[] textEncoded = Encoding.UTF8.GetBytes(text);
            byte[] textEncrypted = rsa.Encrypt(text, false);

            return textEncrypted;
        }

        private byte[] Decrypt(byte[] textEncrypted, RSAParameters prk)
        {
            rsa.ImportParameters(prk);
            byte[] textDecrypted = rsa.Decrypt(textEncrypted, false);

            return textDecrypted;
            //string textDecoded = Encoding.UTF8.GetString(textDecrypted);

            //return textDecoded;
        }

        private RSAParameters GenerateNewPrk()
        {
            rsa = new RSACryptoServiceProvider(KeyLength);
            var clientPrk = rsa.ExportParameters(true);

            return clientPrk;
        }

        private void ChangeUIThreadSafe(Action action)
        {
            this.Dispatcher.BeginInvoke(action);
        }
    }
}
