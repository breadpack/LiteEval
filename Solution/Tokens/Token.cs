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
                Function = new() { Type = ParseFunctionType(str) }
            };
            return token;
        }

        private static FunctionType ParseFunctionType(ReadOnlySpan<char> name) {
            switch (name.Length) {
                case 3:
                    if (name.Equals("abs", StringComparison.OrdinalIgnoreCase)) return FunctionType.Abs;
                    if (name.Equals("cos", StringComparison.OrdinalIgnoreCase)) return FunctionType.Cos;
                    if (name.Equals("exp", StringComparison.OrdinalIgnoreCase)) return FunctionType.Exp;
                    if (name.Equals("log", StringComparison.OrdinalIgnoreCase)) return FunctionType.Log;
                    if (name.Equals("max", StringComparison.OrdinalIgnoreCase)) return FunctionType.Max;
                    if (name.Equals("min", StringComparison.OrdinalIgnoreCase)) return FunctionType.Min;
                    if (name.Equals("pow", StringComparison.OrdinalIgnoreCase)) return FunctionType.Pow;
                    if (name.Equals("sin", StringComparison.OrdinalIgnoreCase)) return FunctionType.Sin;
                    if (name.Equals("tan", StringComparison.OrdinalIgnoreCase)) return FunctionType.Tan;
                    break;
                case 4:
                    if (name.Equals("acos", StringComparison.OrdinalIgnoreCase)) return FunctionType.Acos;
                    if (name.Equals("asin", StringComparison.OrdinalIgnoreCase)) return FunctionType.Asin;
                    if (name.Equals("atan", StringComparison.OrdinalIgnoreCase)) return FunctionType.Atan;
                    if (name.Equals("cosh", StringComparison.OrdinalIgnoreCase)) return FunctionType.Cosh;
                    if (name.Equals("sign", StringComparison.OrdinalIgnoreCase)) return FunctionType.Sign;
                    if (name.Equals("sinh", StringComparison.OrdinalIgnoreCase)) return FunctionType.Sinh;
                    if (name.Equals("sqrt", StringComparison.OrdinalIgnoreCase)) return FunctionType.Sqrt;
                    if (name.Equals("tanh", StringComparison.OrdinalIgnoreCase)) return FunctionType.Tanh;
                    break;
                case 5:
                    if (name.Equals("atan2", StringComparison.OrdinalIgnoreCase)) return FunctionType.Atan2;
                    if (name.Equals("floor", StringComparison.OrdinalIgnoreCase)) return FunctionType.Floor;
                    if (name.Equals("log10", StringComparison.OrdinalIgnoreCase)) return FunctionType.Log10;
                    if (name.Equals("round", StringComparison.OrdinalIgnoreCase)) return FunctionType.Round;
                    break;
                case 7:
                    if (name.Equals("ceiling", StringComparison.OrdinalIgnoreCase)) return FunctionType.Ceiling;
                    break;
                case 8:
                    if (name.Equals("truncate", StringComparison.OrdinalIgnoreCase)) return FunctionType.Truncate;
                    break;
            }

            throw new ArgumentException("Unknown function: " + name.ToString());
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
