using System;

namespace LiteEval {
    public interface IValueProvider {
        public bool TryGetValue(ReadOnlySpan<char> name, out double value);
    }
}