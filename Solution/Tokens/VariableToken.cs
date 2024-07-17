using System;
using System.Runtime.InteropServices;

namespace LiteEval.Tokens {
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct VariableToken {
        public fixed char Name[32];
        
        public static implicit operator ReadOnlySpan<char>(VariableToken token) {
            var length = 0;
            while (length < 32 && token.Name[length] != '\0') {
                length++;
            }

            return new Span<char>(token.Name, length);
        }

        public override string ToString() {
            return ((ReadOnlySpan<char>)this).ToString();
        }
    }
}