using System;
using System.Collections.Generic;
using System.Threading;

namespace LiteEval {
    internal class FunctionToken : IToken {
        private readonly ReadOnlyMemory<char> method;
        private readonly List<IValueToken>    args = new List<IValueToken>();

        private static readonly ThreadLocal<Random> _threadLocalRandom = new ThreadLocal<Random>(
            () => new Random(Interlocked.Increment(ref seed)));

        private static int seed = Environment.TickCount;

        private static double Rand() {
            return _threadLocalRandom.Value.NextDouble();
        }

        private static int Rand(int min, int max) {
            return _threadLocalRandom.Value.Next(min, max);
        }

        public int ArgsCount {
            get {
                if (method.Span.SequenceEqual("abs".AsSpan()))
                    return 1;
                if (method.Span.SequenceEqual("acos".AsSpan()))
                    return 1;
                if (method.Span.SequenceEqual("asin".AsSpan()))
                    return 1;
                if (method.Span.SequenceEqual("atan".AsSpan()))
                    return 1;
                if (method.Span.SequenceEqual("ceiling".AsSpan()))
                    return 1;
                if (method.Span.SequenceEqual("cos".AsSpan()))
                    return 1;
                if (method.Span.SequenceEqual("cosh".AsSpan()))
                    return 1;
                if (method.Span.SequenceEqual("exp".AsSpan()))
                    return 1;
                if (method.Span.SequenceEqual("floor".AsSpan()))
                    return 1;
                if (method.Span.SequenceEqual("log".AsSpan()))
                    return 1;
                if (method.Span.SequenceEqual("log10".AsSpan()))
                    return 1;
                if (method.Span.SequenceEqual("round".AsSpan()))
                    return 1;
                if (method.Span.SequenceEqual("sign".AsSpan()))
                    return 1;
                if (method.Span.SequenceEqual("sin".AsSpan()))
                    return 1;
                if (method.Span.SequenceEqual("sinh".AsSpan()))
                    return 1;
                if (method.Span.SequenceEqual("sqrt".AsSpan()))
                    return 1;
                if (method.Span.SequenceEqual("tan".AsSpan()))
                    return 1;
                if (method.Span.SequenceEqual("tanh".AsSpan()))
                    return 1;
                if (method.Span.SequenceEqual("truncate".AsSpan()))
                    return 1;
                if (method.Span.SequenceEqual("atan2".AsSpan()))
                    return 2;
                if (method.Span.SequenceEqual("max".AsSpan()))
                    return 2;
                if (method.Span.SequenceEqual("min".AsSpan()))
                    return 2;
                if (method.Span.SequenceEqual("pow".AsSpan()))
                    return 2;
                if (method.Span.SequenceEqual("random".AsSpan()))
                    return 0;
                throw new Exception("unknown function " + method);
            }
        }

        public unsafe double Invoke(double* stack, ref int top) {
            if (method.Span.SequenceEqual("abs".AsSpan())) {
                var val = stack[top--];
                return Math.Abs(val);
            }

            if (method.Span.SequenceEqual("acos".AsSpan())) {
                var val = stack[top--];
                return Math.Acos(val);
            }

            if (method.Span.SequenceEqual("asin".AsSpan())) {
                var val = stack[top--];
                return Math.Asin(val);
            }

            if (method.Span.SequenceEqual("atan".AsSpan())) {
                var val = stack[top--];
                return Math.Atan(val);
            }

            if (method.Span.SequenceEqual("atan2".AsSpan())) {
                var val1 = stack[top--];
                var val2 = stack[top--];
                return (Math.Atan2(val1, val2));
            }

            if (method.Span.SequenceEqual("ceiling".AsSpan())) {
                var val = stack[top--];
                return (Math.Ceiling(val));
            }

            if (method.Span.SequenceEqual("cos".AsSpan())) {
                var val = stack[top--];
                return (Math.Cos(val));
            }

            if (method.Span.SequenceEqual("cosh".AsSpan())) {
                var val = stack[top--];
                return (Math.Cosh(val));
            }

            if (method.Span.SequenceEqual("exp".AsSpan())) {
                var val = stack[top--];
                return (Math.Exp(val));
            }

            if (method.Span.SequenceEqual("floor".AsSpan())) {
                var val = stack[top--];
                return (Math.Floor(val));
            }

            if (method.Span.SequenceEqual("log".AsSpan())) {
                var val = stack[top--];
                return (Math.Log(val));
            }

            if (method.Span.SequenceEqual("log10".AsSpan())) {
                var val = stack[top--];
                return (Math.Log10(val));
            }

            if (method.Span.SequenceEqual("max".AsSpan())) {
                var val1 = stack[top--];
                var val2 = stack[top--];
                return (Math.Max(val1, val2));
            }

            if (method.Span.SequenceEqual("min".AsSpan())) {
                var val1 = stack[top--];
                var val2 = stack[top--];
                return (Math.Min(val1, val2));
            }

            if (method.Span.SequenceEqual("pow".AsSpan())) {
                var val1 = stack[top--];
                var val2 = stack[top--];
                return (Math.Pow(val1, val2));
            }

            if (method.Span.SequenceEqual("round".AsSpan())) {
                var val = stack[top--];
                return (Math.Round(val));
            }

            if (method.Span.SequenceEqual("sign".AsSpan())) {
                var val = stack[top--];
                return (Math.Sign(val));
            }

            if (method.Span.SequenceEqual("sin".AsSpan())) {
                var val = stack[top--];
                return (Math.Sin(val));
            }

            if (method.Span.SequenceEqual("sinh".AsSpan())) {
                var val = stack[top--];
                return (Math.Sinh(val));
            }

            if (method.Span.SequenceEqual("sqrt".AsSpan())) {
                var val = stack[top--];
                return (Math.Sqrt(val));
            }

            if (method.Span.SequenceEqual("tan".AsSpan())) {
                var val = stack[top--];
                return (Math.Tan(val));
            }

            if (method.Span.SequenceEqual("tanh".AsSpan())) {
                var val = stack[top--];
                return (Math.Tanh(val));
            }

            if (method.Span.SequenceEqual("truncate".AsSpan())) {
                var val = stack[top--];
                return (Math.Truncate(val));
            }

            if (method.Span.SequenceEqual("random".AsSpan()))
                return (Rand());

            if (method.Span.SequenceEqual("irandomrange".AsSpan())) {
                var val1 = stack[top--];
                var val2 = stack[top--];
                return (Rand((int)val1, (int)val2));
            }

            throw new Exception("unknown function " + method);
        }

        public FunctionToken(ReadOnlyMemory<char> value) {
            method = value;
        }
    }
}