using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LiteEval {
    public class ValueProviderContext : IDisposable {
        private static readonly Stack<ValueProvider> _pool = new();
        
        private static object _lock = new();
        private static IValueProvider _globalProvider = null;

        private static readonly AsyncLocal<Stack<IValueProvider>> _providers = new();

        public ValueProviderContext() {
        }
        
        public ValueProviderContext(IValueProvider provider) {
            _providers.Value.Push(provider);
        }
        
        public ValueProvider SetValueProvider() {
            var valueProvider = Pop();
            _providers.Value.Push(valueProvider);
            return valueProvider;
        }

        public void Dispose() {
            var providers = _providers.Value.Pop();
            if (providers is ValueProvider valueProvider) {
                Push(valueProvider);
            }
        }

        public static void SetGlobalValueProvider(IValueProvider provider) {
            lock (_lock) {
                _globalProvider = provider;
            }
        }

        public static bool TryGetValue(ReadOnlySpan<char> name, out double value) {
            if (_providers.Value != null) {
                foreach (var provider in _providers.Value) {
                    if (provider.TryGetValue(name, out value)) {
                        return true;
                    }
                }
            }
            
            lock (_lock) {
                if (_globalProvider != null && _globalProvider.TryGetValue(name, out value)) {
                    return true;
                }
            }

            value = 0;
            return false;
        }
        
        
        #region Pool

        public static ValueProvider Pop() {
            if (_pool.Count <= 0) return new();
            return _pool.Pop();
        }

        public static void Push(ValueProvider element) {
            element.Clear();
            _pool.Push(element);
        }
        #endregion
    }
}