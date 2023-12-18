using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LiteEval {
    public class ValueProviderContext : IDisposable {
        private static readonly AsyncLocal<Stack<IValueProvider>> _providers = new();

        public ValueProviderContext(IValueProvider provider) {
            _providers.Value ??= new();
            _providers.Value.Push(provider);
        }

        public void Dispose() {
            _providers.Value.Pop();
        }

        public static bool TryGetValue(ReadOnlyMemory<char> name, out double value) {
            if (_providers.Value == null) {
                value = 0;
                return false;
            }

            foreach (var provider in _providers.Value) {
                if (provider.TryGetValue(name, out value)) {
                    return true;
                }
            }

            value = 0;
            return false;
        }
    }
}