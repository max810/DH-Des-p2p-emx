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
        private Dictionary<string, Action<Dictionary<string, string>>> MessageHandlers;

        public MainWindow()
        {
            // MAXIMUM 245 bytes
            InitializeComponent();
            MessageHandlers = new Dictionary<string, Action<Dictionary<string, string>>>()
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
                Message msgData = json.Deserialize<Message>(e.Data);
                var handler = GetHandler(msgData);
                handler(msgData.data);
            };

            socket.Connect();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            socket.Close(CloseStatusCode.Normal);
        }

        private Action<Dictionary<string, string>> GetHandler(Message msgData)
        {
            return MessageHandlers[msgData.event_type];
        }

        //private async void Button_Click(object sender, RoutedEventArgs e)
        //{

        //    string plainText = Input.Text;

        //    var rsa = new RSACryptoServiceProvider(KeyLength);
        //    var originalParameters = rsa.ExportParameters(true);
        //    var clientE = originalParameters.Exponent;
        //    var clientN = originalParameters.Modulus;

        //    HttpClient http = new HttpClient();
        //    string serverPbk = "";
        //    using (var response = await http.GetAsync(serverUrl + "/key"))
        //    {
        //        response.EnsureSuccessStatusCode();
        //        serverPbk = await response.Content.ReadAsStringAsync();
        //    }

        //    string[] serverPublicKey = serverPbk.Split(',');
        //    byte[] serverE = Convert.FromBase64String(serverPublicKey[0]);
        //    byte[] serverN = Convert.FromBase64String(serverPublicKey[1]);
        //    //BigInteger serverPrivateN = BigInteger.Parse(serverPublicKey[2]);
        //    //BigInteger serverPrivateE = BigInteger.Parse(serverPublicKey[3]);
        //    //BigInteger serverPrivateD = BigInteger.Parse(serverPublicKey[4]);
        //    //BigInteger serverPrivateP = BigInteger.Parse(serverPublicKey[5]);
        //    //BigInteger serverPrivateQ = BigInteger.Parse(serverPublicKey[6]);


        //    // copy
        //    var parametersWithServerPbk = new RSAParameters()
        //    {
        //        Modulus = serverN,
        //        Exponent = serverE,
        //        //
        //        //D = serverPrivateD.ToByteArray(),
        //        //P = serverPrivateP.ToByteArray(),
        //        //Q = serverPrivateQ.ToByteArray(),

        //    };
        //    rsa.ImportParameters(parametersWithServerPbk);

        //    // CAREFUL WITH EDNIANNES

        //    byte[] encryptedClientBytes = rsa.Encrypt(Encoding.UTF8.GetBytes(plainText), false);
        //    string encryptedClientString = Convert.ToBase64String(encryptedClientBytes);
        //    string clientEString = Convert.ToBase64String(clientE);
        //    string clientNString = Convert.ToBase64String(clientN);

        //    var postContent = new MultipartFormDataContent
        //    {
        //        // OR CONVERT BIGINTEGER TO STRING AND USE STRINGCONTENT
        //        { new StringContent(encryptedClientString), "b64text" },
        //        { new StringContent(clientEString), "e" },
        //        { new StringContent(clientNString), "n" },
        //        // new BigInteger(clientN.Concat(new byte[]{00}).ToArray()).ToString())
        //        //{ new ByteArrayContent(pbkN), "n" },
        //        //{ new ByteArrayContent(pbkE), "e" },
        //    };

        //    string responseText = "SOMETHING FAILED - DECODED STRING IS EMPTY, EPTA";

        //    rsa.ImportParameters(originalParameters);

        //    using (var response = await http.PostAsync(serverUrl + "/text", postContent))
        //    {
        //        string encryptedServer64 = await response.Content.ReadAsStringAsync();
        //        byte[] encryptedServerBytes = Convert.FromBase64String(encryptedServer64);
        //        byte[] decryptedResponseBytes = rsa.Decrypt(encryptedServerBytes, false);
        //        responseText = Encoding.UTF8.GetString(decryptedResponseBytes);
        //    }
        //    // TODO
        //    //Output.Text += $"{DateTime.Now.TimeOfDay}: {responseText}{Environment.NewLine}";
        //    Input.Text = "";

        //    rsa.PersistKeyInCsp = false;
        //    rsa.Dispose();
        //}


        private void CheckUsernameButtonClick(object sender, RoutedEventArgs e)
        {
            string username = UsernameInput.Text;
            UsernameInput.IsReadOnly = true;
            CheckUsernameButton.IsEnabled = false;
            Username = UsernameInput.Text;

            UsernameLabel.Content = "ПРОВЕРЯЕМ НИК...";

            SendCheckUsername(username);
            //socket.Connect();
            //socket.Emit("check_username", username);
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
        }

        private void SendMessageButtonClick(object sender, EventArgs e)
        {
            string username = Username;
            string message = Input.Text;

            SendChatMessage(username, message);
            this.Dispatcher.BeginInvoke((Action)(() => Input.Text = ""));
        }

        private void SendChatMessage(string username, string chatTextMessage)
        {
            string textMessageb64 = Convert.ToBase64String(Encrypt(chatTextMessage, serverPbkParameters));
            //TODO - tweak gui (username, system color)
            // commit
            // add username_taken
            clientPrkParameters = GenerateNewPrk();
            string[] clientPbkB64 = PbkToBase64(clientPrkParameters);
            var message = new Message("chat_message", new Dictionary<string, string>
            {
                { "message",  textMessageb64},
                { "e", clientPbkB64[0] },
                { "n", clientPbkB64[1] },
            });

            SendMessage(message);
        }

        private void SendCheckUsername(string username)
        {
            var message = new Message("check_username", new Dictionary<string, string>
            {
                { "username", username }
            });

            SendMessage(message);
        }

        private void SendMessage(Message message)
        {
            string data = json.Serialize(message);
            socket.Send(data);
        }

        private void OnUsernameAvailable(Dictionary<string, string> data)
        {
            string[] pbk64 = PbkToBase64(clientPrkParameters);

            // "please, wait"
            // send button disabled

            Message message = new Message("register", new Dictionary<string, string>
            {
                { "username", Username },
                { "e", pbk64[0] },
                { "n", pbk64[1] }
            });
            string data1 = json.Serialize(message);
            //socket.SendAsync(data1, ShowMainUI);
            SendMessage(message);

            this.Dispatcher.BeginInvoke((Action)ShowMainUI);

        }


        private void OnUsernameTaken(Dictionary<string, string> obj)
        {
            this.Dispatcher.BeginInvoke((Action)(() =>
            {
                UsernameInput.IsReadOnly = false;
                UsernameLabel.Content = "НИК ЗАНЯТ. Другой избери";
                UsernameInput.SelectAll();
                CheckUsernameButton.IsEnabled = true;
            }));
        }

        private void ShowMainUI()
        {
            UsernameUI.Visibility = Visibility.Hidden;
            MainUI.Visibility = Visibility.Visible;
            UsernameInput.Visibility = Visibility.Visible;
            UsernameInput.IsReadOnly = true;
            UsernameInput.Margin = new Thickness(20, 20, 0, 0);
            Username = UsernameInput.Text;
        }

        private void OnRegistrationSuccess(Dictionary<string, string> data)
        {
            // "welcome"
            // send button enabled
            SetNewServerPbk(data);
        }


        private void OnClientJoinedChat(Dictionary<string, string> data)
        {
            string username = data["username"];
            this.Dispatcher.BeginInvoke((Action)(() => PrintMessage(username, " has joined chat.", true)));
        }


        private void OnClientLeftChat(Dictionary<string, string> data)
        {
            string username = data["username"];
            this.Dispatcher.BeginInvoke((Action)(() => PrintMessage(username, " has left chat.", true)));
        }


        private void OnChatMessageReceived(Dictionary<string, string> data)
        {
            string username = data["username"];
            string msgEncrypted = data["message"];
            string msgDecrypted = Decrypt(Convert.FromBase64String(msgEncrypted), clientPrkParameters);

            this.Dispatcher.BeginInvoke((Action)(() => PrintMessage(username, msgDecrypted)));
        }

        private void OnMyChatMessageReceived(Dictionary<string, string> data)
        {
            SetNewServerPbk(data);
            OnChatMessageReceived(data);
        }

        private void SetNewServerPbk(Dictionary<string, string> data)
        {
            string e = data["e"];
            string n = data["n"];
            var serverpbk = PbkFromBase64(new[] { e, n });

            serverPbkParameters = serverpbk;
        }

        //private RSAParameters ExtractPublicKey(dynamic data)
        //{
        //    string E = data.e;
        //    string N = data.n;
        //    var pbk = new RSAParameters()
        //    {
        //        Exponent = Convert.FromBase64String(E),
        //        Modulus = Convert.FromBase64String(N),
        //    };

        //    return pbk;
        //}

        //private string[] ExtractMessageInfo(dynamic data)
        //{
        //    string usernameEncrypted = data.username;
        //    string messageEncrypted = data.message;
        //    string username = Decrypt(Convert.FromBase64String(usernameEncrypted), clientPrkParameters);
        //    string message = Decrypt(Convert.FromBase64String(messageEncrypted), clientPrkParameters);

        //    return new[] { username, message };
        //}

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

        private byte[] Encrypt(string text, RSAParameters pbk)
        {
            rsa.ImportParameters(pbk);
            byte[] textEncoded = Encoding.UTF8.GetBytes(text);
            byte[] textEncrypted = rsa.Encrypt(textEncoded, false);

            return textEncrypted;
        }

        private string Decrypt(byte[] textEncrypted, RSAParameters prk)
        {
            rsa.ImportParameters(prk);
            byte[] textDecrypted = rsa.Decrypt(textEncrypted, false);
            string textDecoded = Encoding.UTF8.GetString(textDecrypted);

            return textDecoded;
        }

        private RSAParameters GenerateNewPrk()
        {
            rsa = new RSACryptoServiceProvider(KeyLength);
            var clientPrk = rsa.ExportParameters(true);

            return clientPrk;
        }
    }
}
