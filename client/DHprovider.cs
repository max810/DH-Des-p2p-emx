using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BPiDLab2
{
    public class DHprovider
    {
        public BigInteger P = 23;
        public BigInteger G = 5;
        private BigInteger E;

        private Random rnd = new Random();
        public DHprovider()
        {
            E = rnd.Next();
        }

        public BigInteger Compute(BigInteger keyPart)
        {
            return BigInteger.ModPow(keyPart, E, P);
        }

        public BigInteger ComputeInitial()
        {
            return Compute(G);
        }
    }
}
