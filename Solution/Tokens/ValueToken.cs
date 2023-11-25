using System;

namespace LiteEval {
    internal class ValueToken : IValueToken {
        public double value { get; }

        public ValueToken(ReadOnlyMemory<char> str) {
            double.TryParse(str.Span, out var val);
            value = val;
        }

        public ValueToken(double value) {
            this.value = value;
        }
    }
}