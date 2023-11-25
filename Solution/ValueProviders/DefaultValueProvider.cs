﻿using System;
using System.Collections.Generic;

namespace LiteEval {
    internal class TrieNode {
        public double Value       { get; set; }
        public bool   IsEndOfWord { get; set; }
        public Dictionary<char, TrieNode> Children { get; } = new();

        public void Insert(ReadOnlySpan<char> word, double value) {
            var current = this;
            foreach (var ch in word) {
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
    internal class DefaultValueProvider : IValueProvider {
        private       TrieNode trie = new();

        public double this[ReadOnlyMemory<char> name] {
            get => TryGetValue(name, out var value) ? value : 0;
            set => trie.Insert(name.Span, value);
        }

        public bool TryGetValue(ReadOnlyMemory<char> name, out double value) {
            return trie.TryGetValue(name.Span, out value);
        }
    }
}