using System;

namespace LiteEval {
    internal class VariableToken : IValueToken {
        private readonly ReadOnlyMemory<char> name;
        private readonly Expression           parent;

        public double value => parent[name];

        public VariableToken(Expression parent, ReadOnlyMemory<char> value) {
            this.parent = parent;
            this.name   = value;
        }
    }
}