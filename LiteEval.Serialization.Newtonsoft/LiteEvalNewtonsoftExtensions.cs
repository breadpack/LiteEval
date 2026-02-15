using Newtonsoft.Json;

namespace LiteEval.Serialization.Newtonsoft {
    public static class LiteEvalNewtonsoftExtensions {
        public static JsonSerializerSettings UseLiteEval(this JsonSerializerSettings settings) {
            settings.Converters.Add(new ExpressionJsonConverter());
            return settings;
        }

        public static JsonSerializer UseLiteEval(this JsonSerializer serializer) {
            serializer.Converters.Add(new ExpressionJsonConverter());
            return serializer;
        }
    }
}
