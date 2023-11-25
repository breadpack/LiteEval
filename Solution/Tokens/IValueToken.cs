namespace LiteEval {
    internal interface IValueToken : Expression.IToken {
        double value { get; }
    }
}