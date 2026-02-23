using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LiteEval {
    public class ValueProviderContext : IDisposable {
        private static volatile IValueProvider _globalProvider = null;

        private static readonly AsyncLocal<Stack<IValueProvider>> _providers = new();
        
        public ValueProviderContext(IValueProvider provider) {
            _providers.Value ??= new();
            _providers.Value.Push(provider);
        }

        public void Dispose() {
            _providers.Value.Pop();
            if (_providers.Value.Count == 0) {
                _providers.Value = null;
            }
        }

        public static void SetGlobalValueProvider(IValueProvider provider) {
            _globalProvider = provider;
        }

        public static bool TryGetValue(ReadOnlySpan<char> name, out double value) {
            if (_providers.Value != null) {
                foreach (var provider in _providers.Value) {
                    if (provider.TryGetValue(name, out value)) {
                        return true;
                    }
                }
            }

            var global = _globalProvider;
            if (global != null && global.TryGetValue(name, out value))
                return true;

            value = 0;
            return false;
        }
    }
}