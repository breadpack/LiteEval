using System;
using System.Runtime.InteropServices;

namespace LiteEval {
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct Token {
        public enum TokenType : byte {
            Value,
            Operator,
            Variable,
            Function,
            OpenParenthesis,
            CloseParenthesis,
        }

        public enum FunctionType : byte {
            Abs,
            Acos,
            Asin,
            Atan,
            Ceiling,
            Cos,
            Cosh,
            Exp,
            Floor,
            Log,
            Log10,
            Round,
            Sign,
            Sin,
            Sinh,
            Sqrt,
            Tan,
            Tanh,
            Truncate,
            Atan2,
            Max,
            Min,
            Pow,
        }

        public enum OperatorType : byte {
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

        public       TokenType    Type;
        public       FunctionType Function;
        public       OperatorType Operator;
        public       double       Value;
        public fixed char         VariableName[32];

        public int Priority
            => Operator switch {
                OperatorType.Add        => 1,
                OperatorType.Subtract   => 1,
                OperatorType.Multiply   => 2,
                OperatorType.Divide     => 2,
                OperatorType.Power      => 3,
                OperatorType.UnaryMinus => 4,
                _                       => 0
            };

        public int ArgCount
            => Function switch {
                FunctionType.Abs      => 1,
                FunctionType.Acos     => 1,
                FunctionType.Asin     => 1,
                FunctionType.Atan     => 1,
                FunctionType.Ceiling  => 1,
                FunctionType.Cos      => 1,
                FunctionType.Cosh     => 1,
                FunctionType.Exp      => 1,
                FunctionType.Floor    => 1,
                FunctionType.Log      => 1,
                FunctionType.Log10    => 1,
                FunctionType.Round    => 1,
                FunctionType.Sign     => 1,
                FunctionType.Sin      => 1,
                FunctionType.Sinh     => 1,
                FunctionType.Sqrt     => 1,
                FunctionType.Tan      => 1,
                FunctionType.Tanh     => 1,
                FunctionType.Truncate => 1,
                FunctionType.Atan2    => 2,
                FunctionType.Max      => 2,
                FunctionType.Min      => 2,
                FunctionType.Pow      => 2,
                _                     => throw new("unknown function")
            };
        

        public double Invoke(double* stack, ref int top) {
            switch (Function) {
                case FunctionType.Abs:
                    return Math.Abs(stack[top--]);
                case FunctionType.Acos:
                    return Math.Acos(stack[top--]);
                case FunctionType.Asin:
                    return Math.Asin(stack[top--]);
                case FunctionType.Atan:
                    return Math.Atan(stack[top--]);
                case FunctionType.Ceiling:
                    return Math.Ceiling(stack[top--]);
                case FunctionType.Cos:
                    return Math.Cos(stack[top--]);
                case FunctionType.Cosh:
                    return Math.Cosh(stack[top--]);
                case FunctionType.Exp:
                    return Math.Exp(stack[top--]);
                case FunctionType.Floor:
                    return Math.Floor(stack[top--]);
                case FunctionType.Log:
                    return Math.Log(stack[top--]);
                case FunctionType.Log10:
                    return Math.Log10(stack[top--]);
                case FunctionType.Round:
                    return Math.Round(stack[top--]);
                case FunctionType.Sign:
                    return Math.Sign(stack[top--]);
                case FunctionType.Sin:
                    return Math.Sin(stack[top--]);
                case FunctionType.Sinh:
                    return Math.Sinh(stack[top--]);
                case FunctionType.Sqrt:
                    return Math.Sqrt(stack[top--]);
                case FunctionType.Tan:
                    return Math.Tan(stack[top--]);
                case FunctionType.Tanh:
                    return Math.Tanh(stack[top--]);
                case FunctionType.Truncate:
                    return Math.Truncate(stack[top--]);
                case FunctionType.Atan2:
                    return Math.Atan2(stack[top--], stack[top--]);
                case FunctionType.Max:
                    return Math.Max(stack[top--], stack[top--]);
                case FunctionType.Min:
                    return Math.Min(stack[top--], stack[top--]);
                case FunctionType.Pow:
                    return Math.Pow(stack[top--], stack[top--]);
                default:
                    throw new("unknown function");
            }
        }

        public static Token CreateValueToken(double value) {
            var token = new Token {
                Type  = TokenType.Value,
                Value = value,
            };
            return token;
        }

        public static Token CreateValueToken(ReadOnlySpan<char> value) {
            return CreateValueToken(double.Parse(value));
        }

        public static Token CreateVariableToken(ReadOnlySpan<char> value) {
            var token = new Token {
                Type = TokenType.Variable,
            };
            value.CopyTo(new(token.VariableName, 32));
            return token;
        }

        public static Token CreateFunctionToken(ReadOnlySpan<char> slice) {
            var token = new Token {
                Type     = TokenType.Function,
                Function = Enum.Parse<FunctionType>(slice.ToString(), true)
            };
            return token;
        }

        public static Token CreateOperatorToken(char c) {
            var token = new Token() {
                Type = TokenType.Operator,
            };
            switch (c) {
                case '+':
                    token.Operator = OperatorType.Add;
                    break;
                case '-':
                    token.Operator = OperatorType.Subtract;
                    break;
                case '*':
                    token.Operator = OperatorType.Multiply;
                    break;
                case '/':
                    token.Operator = OperatorType.Divide;
                    break;
                case '^':
                    token.Operator = OperatorType.Power;
                    break;
                case '(':
                    token.Operator = OperatorType.ParenthesisStart;
                    break;
                case ')':
                    token.Operator = OperatorType.ParenthesisEnd;
                    break;
                case ',':
                    token.Operator = OperatorType.Comma;
                    break;
                default:
                    throw new Exception("unknown oper " + c);
            }

            return token;
        }
        
        public static Token CreateUnaryMinusToken() {
            return new Token {
                Type     = TokenType.Operator,
                Operator = OperatorType.UnaryMinus
            };
        }
    }
}