using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Security.Cryptography;
using System.Net.Http;
using System.Numerics;

namespace BPiDLab2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private string serverUrl = "http://localhost:5000";
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            string plainText = Input.Text;

            var rsa = new RSACryptoServiceProvider(2048);
            var originalParameters = rsa.ExportParameters(true);
            var clientE = originalParameters.Exponent;
            var clientN = originalParameters.Modulus;

            HttpClient http = new HttpClient();
            string serverPbk = "";
            using (var response = await http.GetAsync(serverUrl + "/key"))
            {
                response.EnsureSuccessStatusCode();
                serverPbk = await response.Content.ReadAsStringAsync();
            }

            string[] serverPublicKey = serverPbk.Split(',');
            byte[] serverE = Convert.FromBase64String(serverPublicKey[0]);
            byte[] serverN = Convert.FromBase64String(serverPublicKey[1]);
            //BigInteger serverPrivateN = BigInteger.Parse(serverPublicKey[2]);
            //BigInteger serverPrivateE = BigInteger.Parse(serverPublicKey[3]);
            //BigInteger serverPrivateD = BigInteger.Parse(serverPublicKey[4]);
            //BigInteger serverPrivateP = BigInteger.Parse(serverPublicKey[5]);
            //BigInteger serverPrivateQ = BigInteger.Parse(serverPublicKey[6]);


            // copy
            var parametersWithServerPbk = new RSAParameters()
            {
                Modulus = serverN,
                Exponent = serverE,
                //
                //D = serverPrivateD.ToByteArray(),
                //P = serverPrivateP.ToByteArray(),
                //Q = serverPrivateQ.ToByteArray(),
                
            };
            rsa.ImportParameters(parametersWithServerPbk);

            // CAREFUL WITH EDNIANNES

            byte[] encryptedClientBytes = rsa.Encrypt(Encoding.UTF8.GetBytes(plainText), false);
            string encryptedClientString = Convert.ToBase64String(encryptedClientBytes);
            string clientEString = Convert.ToBase64String(clientE);
            string clientNString = Convert.ToBase64String(clientN);

            var postContent = new MultipartFormDataContent
            {
                // OR CONVERT BIGINTEGER TO STRING AND USE STRINGCONTENT
                { new StringContent(encryptedClientString), "b64text" },
                { new StringContent(clientEString), "e" },
                { new StringContent(clientNString), "n" },
                // new BigInteger(clientN.Concat(new byte[]{00}).ToArray()).ToString())
                //{ new ByteArrayContent(pbkN), "n" },
                //{ new ByteArrayContent(pbkE), "e" },
            };

            string responseText = "SOMETHING FAILED - DECODED STRING IS EMPTY, EPTA";

            rsa.ImportParameters(originalParameters);

            using (var response = await http.PostAsync(serverUrl + "/text", postContent))
            {
                string encryptedServer64 = await response.Content.ReadAsStringAsync();
                byte[] encryptedServerBytes = Convert.FromBase64String(encryptedServer64);
                // ебнулось тут
                byte[] decryptedResponseBytes = rsa.Decrypt(encryptedServerBytes, false);
                responseText = Encoding.UTF8.GetString(decryptedResponseBytes);
            }

            Output.Text += $"{DateTime.Now.TimeOfDay}: {responseText}{Environment.NewLine}";

            rsa.PersistKeyInCsp = false;
            rsa.Dispose();
        }
    }
}
