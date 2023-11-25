using System;

namespace LiteEval {
    public interface IValueProvider {
        public bool TryGetValue(ReadOnlyMemory<char> name, out double value);
    }
}