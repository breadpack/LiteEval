using System;
using System.Collections;

namespace LiteEval {
    public class ValueProvider : IValueProvider {
        private readonly TrieNode trie = new();
        
        public ValueProvider Add(string name, double value) {
            trie.Insert(name.AsSpan(), value);
            return this;
        }
        
        public ValueProvider Add(ReadOnlySpan<char> name, double value) {
            trie.Insert(name, value);
            return this;
        }
        
        public bool TryGetValue(ReadOnlySpan<char> name, out double value) {
            return trie.TryGetValue(name, out value);
        }

        public void Clear() {
            trie.Clear();
        }
    }
}