using System;
using System.Runtime.InteropServices;
using LiteEval.Enums;

namespace LiteEval.Tokens {
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    internal struct Token : IEquatable<Token> {
        [FieldOffset(0)] public TokenType     Type;
        [FieldOffset(1)] public ValueToken    Value;
        [FieldOffset(1)] public VariableToken Variable;
        [FieldOffset(1)] public FunctionToken Function;
        [FieldOffset(1)] public OperatorToken Operator;

        internal static Token CreateValueToken(ReadOnlySpan<char> str) {
            var value = str.IsEmpty ? 0 : double.Parse(str);
            var token = new Token {
                Type  = TokenType.Value,
                Value = new() { Value = value },
            };
            return token;
        }

        internal static Token CreateValueToken(double d) {
            return new Token {
                Type  = TokenType.Value,
                Value = new() { Value = d }
            };
        }

        internal static Token CreateVariableToken(int nameIndex) {
            return new Token {
                Type     = TokenType.Variable,
                Variable = new() { NameIndex = nameIndex }
            };
        }

        internal static Token CreateFunctionToken(ReadOnlySpan<char> str) {
            var token = new Token {
                Type     = TokenType.Function,
                Function = new() { Type = Enum.Parse<FunctionType>(str.ToString(), true) }
            };
            return token;
        }

        internal static Token CreateFunctionToken(FunctionType type) {
            return new Token {
                Type     = TokenType.Function,
                Function = new() { Type = type }
            };
        }

        internal static Token CreateOperatorToken(char c) {
            var operatorType = c switch {
                '+' => OperatorType.Add,
                '-' => OperatorType.Subtract,
                '*' => OperatorType.Multiply,
                '/' => OperatorType.Divide,
                '^' => OperatorType.Power,
                '(' => OperatorType.ParenthesisStart,
                ')' => OperatorType.ParenthesisEnd,
                ',' => OperatorType.Comma,
                _   => throw new Exception("unknown oper " + c)
            };
            return CreateOperatorToken(operatorType);
        }

        internal static Token CreateOperatorToken(OperatorType operatorType) {
            return new Token {
                Type     = TokenType.Operator,
                Operator = new() { Type = operatorType }
            };
        }

        internal static Token CreateUnaryMinusToken() {
            return new Token {
                Type     = TokenType.Operator,
                Operator = new() { Type = OperatorType.UnaryMinus },
            };
        }

        public bool Equals(Token other) {
            return Type == other.Type
                && Type switch {
                       TokenType.Value            => Value.Equals(other.Value),
                       TokenType.Variable         => Variable.Equals(other.Variable),
                       TokenType.Function         => Function.Equals(other.Function),
                       TokenType.Operator         => Operator.Equals(other.Operator),
                       _                          => false
                   };
        }

        public override bool Equals(object obj) {
            return obj is Token other && Equals(other);
        }

        public override int GetHashCode() {
            return HashCode.Combine(
                (int)Type,
                Type switch {
                    TokenType.Value            => Value.GetHashCode(),
                    TokenType.Variable         => Variable.GetHashCode(),
                    TokenType.Function         => Function.GetHashCode(),
                    TokenType.Operator         => Operator.GetHashCode(),
                    _                          => 0
                });
        }

        public static bool operator ==(Token left, Token right) {
            return left.Equals(right);
        }

        public static bool operator !=(Token left, Token right) {
            return !left.Equals(right);
        }
    }
}
