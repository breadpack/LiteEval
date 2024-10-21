using System;
using System.Runtime.InteropServices;
using LiteEval.Enums;

namespace LiteEval.Tokens {
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal unsafe struct FunctionToken : IEquatable<FunctionToken> {
        public FunctionType Type;

        public int ArgCount
            => Type switch {
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

        public readonly double Invoke(double* stack, ref int top) {
            switch (Type) {
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

        public bool Equals(FunctionToken other) {
            return Type == other.Type;
        }

        public override bool Equals(object obj) {
            return obj is FunctionToken other && Equals(other);
        }

        public override int GetHashCode() {
            return (int)Type;
        }

        public static bool operator ==(FunctionToken left, FunctionToken right) {
            return left.Equals(right);
        }

        public static bool operator !=(FunctionToken left, FunctionToken right) {
            return !left.Equals(right);
        }
        
        public override string ToString() {
            return Type.ToString();
        }
    }
}