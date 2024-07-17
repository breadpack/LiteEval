using System.Runtime.InteropServices;

namespace LiteEval.Tokens {
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ValueToken {
        public double Value;
        
        public static implicit operator double(ValueToken token) {
            return token.Value;
        }
    }
}