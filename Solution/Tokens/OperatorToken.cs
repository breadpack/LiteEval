using System.Runtime.InteropServices;
using LiteEval.Enums;

namespace LiteEval.Tokens {
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct OperatorToken {
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
    }
}