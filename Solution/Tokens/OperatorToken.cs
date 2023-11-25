using System;

namespace LiteEval {
    internal class OperatorToken : IToken {
        public enum OperatorType {
            Add,
            Subtract,
            Multiply,
            Divide,
            Power,
            ParenthesisStart,
            ParenthesisEnd,
            UnaryMinus,
            Comma,
        }

        public OperatorType Type { get; set; }

        public int Priority =>
            Type switch {
                OperatorType.Add        => 1,
                OperatorType.Subtract   => 1,
                OperatorType.Multiply   => 2,
                OperatorType.Divide     => 2,
                OperatorType.Power      => 3,
                OperatorType.UnaryMinus => 4,
                _                       => 0
            };

        public OperatorToken(char value) {
            Type = value switch {
                '+' => OperatorType.Add,
                '-' => OperatorType.Subtract,
                '*' => OperatorType.Multiply,
                '/' => OperatorType.Divide,
                '^' => OperatorType.Power,
                '(' => OperatorType.ParenthesisStart,
                ')' => OperatorType.ParenthesisEnd,
                ',' => OperatorType.Comma,
                _   => throw new Exception("unknown oper " + value)
            };
        }
    }
}