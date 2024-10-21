using System;
using System.Runtime.InteropServices;

namespace LiteEval.Tokens {
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ValueToken : IEquatable<ValueToken> {
        public double Value;
        
        public static implicit operator double(ValueToken token) {
            return token.Value;
        }

        public bool Equals(ValueToken other) {
            return Value.Equals(other.Value);
        }

        public override bool Equals(object obj) {
            return obj is ValueToken other && Equals(other);
        }

        public override int GetHashCode() {
            return Value.GetHashCode();
        }

        public static bool operator ==(ValueToken left, ValueToken right) {
            return left.Equals(right);
        }

        public static bool operator !=(ValueToken left, ValueToken right) {
            return !left.Equals(right);
        }
    }
}