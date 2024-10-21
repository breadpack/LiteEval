using System;
using System.Runtime.InteropServices;
using LiteEval.Enums;

namespace LiteEval.Tokens {
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct OperatorToken : IEquatable<OperatorToken> {
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
        
        public char Symbol
            => Type switch {
                OperatorType.Add        => '+',
                OperatorType.Subtract   => '-',
                OperatorType.Multiply   => '*',
                OperatorType.Divide     => '/',
                OperatorType.Power      => '^',
                OperatorType.UnaryMinus => '-',
                _                       => throw new ArgumentOutOfRangeException()
            };
        
        public Associativity Associativity
         => Type switch {
             OperatorType.Add        => Associativity.Left,
             OperatorType.Subtract   => Associativity.Left,
             OperatorType.Multiply   => Associativity.Left,
             OperatorType.Divide     => Associativity.Left,
             OperatorType.Power      => Associativity.Right,
             OperatorType.UnaryMinus => Associativity.Right,
             _                       => throw new ArgumentOutOfRangeException()
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
        
        public override string ToString() {
            return Symbol.ToString();
        }
    }
}