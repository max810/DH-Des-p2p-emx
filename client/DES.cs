using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;

namespace BPiDLab2
{
    public class DES
    {
        private Random keyRandom;
        private Random IVrandom;
        private DESCryptoServiceProvider des;
        private ICryptoTransform encryptor;
        private ICryptoTransform decryptor;
        public byte[] Key { get; }
        public DES(int seed)
        {
            keyRandom = new Random(seed);
            IVrandom = new Random(seed * 2 - 1);

            des = new DESCryptoServiceProvider();
            byte[] key = new byte[8];
            keyRandom.NextBytes(key);
            byte[] IV = new byte[8];
            keyRandom.NextBytes(IV);
            Key = key;
            des.Key = key;
            des.IV = IV;
            encryptor = des.CreateEncryptor();
            decryptor = des.CreateDecryptor();
        }

        //public byte[] Encrypt(string text)
        //{
        //    return Encrypt(Encoding.UTF8.GetBytes(text));
        //}

        public byte[] Encrypt(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using(CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    cs.Write(bytes, 0, bytes.Length);
                    cs.FlushFinalBlock();
                    return ms.ToArray();
                }
            }
        }

        //public byte[] Decrypt(string encText)
        //{
        //    return Decrypt(Encoding.UTF8.GetBytes(encText));
        //}

        public byte[] Decrypt(byte[] encBytes)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
                {
                    cs.Write(encBytes, 0, encBytes.Length);
                    cs.FlushFinalBlock();
                    return ms.ToArray();
                }
            }
        }
    }
}
