using System.Text;
using LiteEval;
using Newtonsoft.Json;

namespace LiteEval.Serialization.Newtonsoft {
    public class NewtonsoftExpressionSerializer : IExpressionSerializer {
        private readonly JsonSerializerSettings _settings;

        public NewtonsoftExpressionSerializer() {
            _settings = new JsonSerializerSettings();
            _settings.Converters.Add(new ExpressionJsonConverter());
        }

        public byte[] Serialize(Expression expression) {
            var json = JsonConvert.SerializeObject(expression, _settings);
            return Encoding.UTF8.GetBytes(json);
        }

        public Expression Deserialize(byte[] data) {
            var json = Encoding.UTF8.GetString(data);
            return JsonConvert.DeserializeObject<Expression>(json, _settings);
        }
    }
}
