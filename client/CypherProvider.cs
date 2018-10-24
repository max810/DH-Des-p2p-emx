using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPiDLab2
{
    public class CypherProvider
    {
        private static readonly Dictionary<char, string> CharNumbers =
            new Dictionary<char, string>
        {
            {'а', "1"},
            {'и', "2"},
            {'т', "3"},
            {'е', "4"},
            {'с', "5"},
            {'н', "6"},
            {'о', "7"},

            {'б', "81"},
            {'в', "82"},
            {'г', "83"},
            {'д', "84"},
            {'ж', "85"},
            {'к', "86"},
            {'л', "87"},
            {'м', "88"},
            {'п', "80"},

            {'р', "91"},
            {'у', "92"},
            {'ф', "93"},
            {'х', "94"},
            {'ц', "95"},
            {'ч', "96"},
            {'щ', "97"},
            {'ъ', "98"},
            {'ы', "90"},

            {'ь', "01"},
            {'э', "02"},
            {'ю', "03"},
            {'я', "04"},
            {' ', "00"},
        };

        private static readonly Dictionary<string, char> NumbersChar =
            CharNumbers.ToDictionary(k => k.Value, k => k.Key);

        private readonly byte[] key;

        public CypherProvider(string key)
        {
            this.key = Encode(key);
        }

        public string Encrypt(string text)
        {
            byte[] textNumbers = Encode(text);
            byte[] encryptedNumbers = textNumbers.Zip(key, (x, y) => (byte)((x + y) % 10)).ToArray();
            string encryptedChars = Decode(encryptedNumbers);

            return encryptedChars;
        }

        public string Decrypt(string textEncrypted)
        {
            byte[] textNumbers = Encode(textEncrypted);
            byte[] decryptedNumbers = textNumbers.Zip(key, (x, y) => (byte)((x - y) % 10)).ToArray();
            string decryptedChars = Decode(decryptedNumbers);

            return decryptedChars;
        }

        private byte[] Encode(string text)
        {
            string textEncoded = string.Join("", text.Select(x => CharNumbers[x]));
            List<byte> bytes = new List<byte>();
            foreach (var digit in textEncoded)
            {
                byte c = (byte)(digit - '0');
                bytes.Add(c);
            }

            return bytes.ToArray();
        }

        private string Decode(byte[] numbers)
        {
            StringBuilder textBuilder = new StringBuilder();
            for (int i = 0; i < numbers.Length; i++)
            {
                string symbol = numbers[i].ToString();
                if (numbers[i] == 0 || numbers[i] > 7)
                {
                    symbol += numbers[++i];
                }

                textBuilder.Append(NumbersChar[symbol]);
            }

            return textBuilder.ToString();
        }
    }
}
