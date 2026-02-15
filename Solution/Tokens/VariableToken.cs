using System;
using System.Runtime.InteropServices;

namespace LiteEval.Tokens {
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct VariableToken : IEquatable<VariableToken> {
        public int NameIndex;

        public bool Equals(VariableToken other) {
            return NameIndex == other.NameIndex;
        }

        public override bool Equals(object obj) {
            return obj is VariableToken other && Equals(other);
        }

        public override int GetHashCode() {
            return NameIndex;
        }

        public static bool operator ==(VariableToken left, VariableToken right) {
            return left.Equals(right);
        }

        public static bool operator !=(VariableToken left, VariableToken right) {
            return !left.Equals(right);
        }
    }
}
