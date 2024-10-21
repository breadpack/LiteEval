using System;
using System.Runtime.InteropServices;
using LiteEval.Enums;

namespace LiteEval.Tokens {
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    internal unsafe struct Token {
        [FieldOffset(0)] public TokenType     Type;
        [FieldOffset(1)] public ValueToken    Value;
        [FieldOffset(1)] public VariableToken Variable;
        [FieldOffset(1)] public FunctionToken Function;
        [FieldOffset(1)] public OperatorToken Operator;

        internal static Token CreateValueToken(ReadOnlySpan<char> str) {
            var value = double.Parse(str);
            var token = new Token {
                Type  = TokenType.Value,
                Value = new() { Value = value },
            };
            return token;
        }

        internal static Token CreateVariableToken(ReadOnlySpan<char> str) {
            var token = new Token {
                Type = TokenType.Variable,
            };
            str.CopyTo(new(token.Variable.Name, 32));
            return token;
        }

        internal static Token CreateFunctionToken(ReadOnlySpan<char> str) {
            var token = new Token {
                Type     = TokenType.Function,
                Function = new() { Type = Enum.Parse<FunctionType>(str.ToString(), true) }
            };
            return token;
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
                Type = TokenType.Operator,
                Operator = new() {
                    Type = operatorType
                }
            };
        }

        internal static Token CreateUnaryMinusToken() {
            return new Token {
                Type     = TokenType.Operator,
                Operator = new() { Type = OperatorType.UnaryMinus },
            };
        }
    }
}