using System;
using System.Runtime.InteropServices;
using LiteEval.Enums;

namespace LiteEval.Tokens {
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct OperatorToken : IEquatable<OperatorToken> {
        public OperatorType Type;
        
        public static implicit operator OperatorType(OperatorToken token) {
            return token.Type;
        }

        public int Priority
            => Type switch {
                OperatorType.Add        => 1,
                OperatorType.Subtract   => 1,
                OperatorType.Multiply   => 2,
                OperatorType.Divide     => 2,
                OperatorType.Power      => 3,
                OperatorType.UnaryMinus => 4,
                _                       => 0
            };

        public bool Equals(OperatorToken other) {
            return Type == other.Type;
        }

        public override bool Equals(object obj) {
            return obj is OperatorToken other && Equals(other);
        }

        public override int GetHashCode() {
            return (int)Type;
        }

        public static bool operator ==(OperatorToken left, OperatorToken right) {
            return left.Equals(right);
        }

        public static bool operator !=(OperatorToken left, OperatorToken right) {
            return !left.Equals(right);
        }
    }
}