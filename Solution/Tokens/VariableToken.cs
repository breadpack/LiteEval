using System;
using System.Runtime.InteropServices;

namespace LiteEval.Tokens {
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct VariableToken : IEquatable<VariableToken> {
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

        public bool Equals(VariableToken other) {
            return ((ReadOnlySpan<char>)this).SequenceEqual((ReadOnlySpan<char>)other);
        }

        public override bool Equals(object obj) {
            return obj is VariableToken other && Equals(other);
        }

        public override int GetHashCode() {
            var hash = new HashCode();
            for (var i = 0; i < 32 && Name[i] != '\0'; i++) {
                hash.Add(Name[i]);
            }
            return hash.ToHashCode();
        }

        public static bool operator ==(VariableToken left, VariableToken right) {
            return left.Equals(right);
        }

        public static bool operator !=(VariableToken left, VariableToken right) {
            return !left.Equals(right);
        }
    }
}