namespace LiteEval.Serialization {
    public interface IExpressionSerializer {
        byte[] Serialize(Expression expression);
        Expression Deserialize(byte[] data);
    }
}
