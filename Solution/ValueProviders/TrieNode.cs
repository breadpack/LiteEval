using System;
using System.Collections.Generic;

namespace LiteEval {
    internal class TrieNode {
        private double                     Value       { get; set; }
        private bool                       IsEndOfWord { get; set; }
        private Dictionary<char, TrieNode> Children    { get; } = new();

        public void Insert(ReadOnlySpan<char> word, double value) {
            var current = this;
            foreach (var ch in word) {
                if (ch == '\0') break;

                if (!current.Children.ContainsKey(ch)) {
                    current.Children[ch] = new();
                }

                current = current.Children[ch];
            }

            current.IsEndOfWord = true;
            current.Value       = value;
        }

        public bool TryGetValue(ReadOnlySpan<char> word, out double value) {
            var current = this;
            foreach (var ch in word) {
                if (ch == '\0') break;

                if (!current.Children.TryGetValue(ch, out current)) {
                    value = default;
                    return false;
                }
            }

            if (current.IsEndOfWord) {
                value = current.Value;
                return true;
            }

            value = 0;
            return false;
        }
    }
}