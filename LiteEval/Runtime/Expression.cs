using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LiteEval {
    public interface IValueProvider {
        public bool TryGetValue(string name, out double value);
    }

    internal class DefaultValueProvider : IValueProvider {
        private readonly Dictionary<string, double> _values = new();

        public double this[string name] {
            get => _values[name];
            set => _values[name] = value;
        }

        public bool TryGetValue(string name, out double value) {
            if (!_values.TryGetValue(name, out value)) {
                value = 0;
                return false;
            }
            return true;
        }
    }


    public class Expression {
        internal interface IToken { }

        internal interface IValueToken : IToken {
            double value { get; }
        }

        internal class ValueToken : IValueToken {
            public double value { get; }

            public ValueToken(ReadOnlySpan<char> value) {
                this.value = double.Parse(value);
            }

            public ValueToken(double value) {
                this.value = value;
            }
        }

        internal class VariableToken : IValueToken {
            private readonly string     name;
            private readonly Expression parent;

            public double value => parent[name];

            public VariableToken(Expression parent, ReadOnlySpan<char> value) {
                this.parent = parent;
                this.name   = value.ToString();
            }
        }

        internal class OperatorToken : IToken {
            public enum OperatorType {
                Add,
                Subtract,
                Multiply,
                Divide,
                Power,
                ParenthesisStart,
                ParenthesisEnd,
                UnaryMinus,
                Comma,
            }

            public OperatorType Type { get; set; }

            public int Priority =>
                Type switch {
                    OperatorType.Add => 1,
                    OperatorType.Subtract => 1,
                    OperatorType.Multiply => 2,
                    OperatorType.Divide => 2,
                    OperatorType.Power => 3,
                    OperatorType.UnaryMinus => 4,
                    _ => 0
                };

            public OperatorToken(char value) {
                Type = value switch {
                    '+' => OperatorType.Add,
                    '-' => OperatorType.Subtract,
                    '*' => OperatorType.Multiply,
                    '/' => OperatorType.Divide,
                    '^' => OperatorType.Power,
                    '(' => OperatorType.ParenthesisStart,
                    ')' => OperatorType.ParenthesisEnd,
                    ',' => OperatorType.Comma,
                    _ => throw new Exception("unknown oper " + value)
                };
            }
        }

        internal class FunctionToken : IToken {
            private readonly string            method;
            private readonly List<IValueToken> args = new List<IValueToken>();

            public int ArgsCount =>
                method switch {
                    "abs" => 1,
                    "acos" => 1,
                    "asin" => 1,
                    "atan" => 1,
                    "atan2" => 2,
                    "ceiling" => 1,
                    "cos" => 1,
                    "cosh" => 1,
                    "exp" => 1,
                    "floor" => 1,
                    "log" => 1,
                    "log10" => 1,
                    "max" => 2,
                    "min" => 2,
                    "pow" => 2,
                    "round" => 1,
                    "sign" => 1,
                    "sin" => 1,
                    "sinh" => 1,
                    "sqrt" => 1,
                    "tan" => 1,
                    "tanh" => 1,
                    "truncate" => 1,
                    _ => throw new Exception("unknown function " + method)
                };

            public ValueToken Invoke(Stack<IToken> arg) {
                switch (method) {
                    case "abs": {
                        var val = arg.Pop() as IValueToken;
                        return new ValueToken(Math.Abs(val.value));
                    }
                    case "acos": {
                        var val = arg.Pop() as IValueToken;
                        return new ValueToken(Math.Acos(val.value));
                    }
                    case "asin": {
                        var val = arg.Pop() as IValueToken;
                        return new ValueToken(Math.Asin(val.value));
                    }
                    case "atan": {
                        var val = arg.Pop() as IValueToken;
                        return new ValueToken(Math.Atan(val.value));
                    }
                    case "atan2": {
                        var val1 = arg.Pop() as IValueToken;
                        var val2 = arg.Pop() as IValueToken;
                        return new ValueToken(Math.Atan2(val1.value, val2.value));
                    }
                    case "ceiling": {
                        var val = arg.Pop() as IValueToken;
                        return new ValueToken(Math.Ceiling(val.value));
                    }
                    case "cos": {
                        var val = arg.Pop() as IValueToken;
                        return new ValueToken(Math.Cos(val.value));
                    }
                    case "cosh": {
                        var val = arg.Pop() as IValueToken;
                        return new ValueToken(Math.Cosh(val.value));
                    }
                    case "exp": {
                        var val = arg.Pop() as IValueToken;
                        return new ValueToken(Math.Exp(val.value));
                    }
                    case "floor": {
                        var val = arg.Pop() as IValueToken;
                        return new ValueToken(Math.Floor(val.value));
                    }
                    case "log": {
                        var val = arg.Pop() as IValueToken;
                        return new ValueToken(Math.Log(val.value));
                    }
                    case "log10": {
                        var val = arg.Pop() as IValueToken;
                        return new ValueToken(Math.Log10(val.value));
                    }
                    case "max": {
                        var val1 = arg.Pop() as IValueToken;
                        var val2 = arg.Pop() as IValueToken;
                        return new ValueToken(Math.Max(val1.value, val2.value));
                    }
                    case "min": {
                        var val1 = arg.Pop() as IValueToken;
                        var val2 = arg.Pop() as IValueToken;
                        return new ValueToken(Math.Min(val1.value, val2.value));
                    }
                    case "pow": {
                        var val1 = arg.Pop() as IValueToken;
                        var val2 = arg.Pop() as IValueToken;
                        return new ValueToken(Math.Pow(val1.value, val2.value));
                    }
                    case "round": {
                        var val = arg.Pop() as IValueToken;
                        return new ValueToken(Math.Round(val.value));
                    }
                    case "sign": {
                        var val = arg.Pop() as IValueToken;
                        return new ValueToken(Math.Sign(val.value));
                    }
                    case "sin": {
                        var val = arg.Pop() as IValueToken;
                        return new ValueToken(Math.Sin(val.value));
                    }
                    case "sinh": {
                        var val = arg.Pop() as IValueToken;
                        return new ValueToken(Math.Sinh(val.value));
                    }
                    case "sqrt": {
                        var val = arg.Pop() as IValueToken;
                        return new ValueToken(Math.Sqrt(val.value));
                    }
                    case "tan": {
                        var val = arg.Pop() as IValueToken;
                        return new ValueToken(Math.Tan(val.value));
                    }
                    case "tanh": {
                        var val = arg.Pop() as IValueToken;
                        return new ValueToken(Math.Tanh(val.value));
                    }
                    case "truncate": {
                        var val = arg.Pop() as IValueToken;
                        return new ValueToken(Math.Truncate(val.value));
                    }
                    default:
                        throw new Exception("unknown function " + method);
                }
            }

            public FunctionToken(ReadOnlySpan<char> value) {
                method = value.ToString();
            }
        }

        private static readonly char   exponentChar = 'E';
        private static readonly char[] operatorChar = new char[] { '(', ')', '+', '-', '/', '^', '*' };

        private string _expression;

        public string expression {
            get => _expression.ToString();
            set {
                _expression = value.Replace(" ", "");
                GetToken(in _expression, out _tokens);
            }
        }

        private DefaultValueProvider _valueProvider = new DefaultValueProvider();
        private IValueProvider       _tempProvider;

        private IToken[]       _tokens;
        
        public double this[string name] {
            get {
                if (_tempProvider.TryGetValue(name, out var value))
                    return value;
                return _valueProvider[name];
            }
            set => _valueProvider[name] = value;
        }

        public Expression(string expr) {
            expression = expr.Replace(" ", "");
        }

        private static readonly Regex Regex = new Regex(@"(?<number>\d+(\.\d+)?)|(?<function>[a-zA-Z_][_a-zA-Z0-9]+(?=\())|(?<variable>[a-zA-Z_][_a-zA-Z0-9]*)|(?<operator>[+/*^()-])", RegexOptions.Compiled);

        private void GetToken(in string expression, out IToken[] result) {
            var    tokens        = new List<IToken>();
            var    m             = Regex.Match(expression);
            IToken previousToken = null;

            var stack = new Stack<IToken>();

            while (m.Success) {
                if (m.Groups["number"].Success) {
                    var valueToken = new ValueToken(m.Value.AsSpan());
                    tokens.Add(valueToken);
                    previousToken = valueToken;
                }
                else if (m.Groups["variable"].Success) {
                    var variableToken = new VariableToken(this, m.Value.AsSpan());
                    tokens.Add(variableToken);
                    previousToken = variableToken;
                }
                else if (m.Groups["function"].Success) {
                    var functionToken = new FunctionToken(m.Value.AsSpan());
                    stack.Push(functionToken);
                    previousToken = functionToken;
                }
                else if (m.Groups["operator"].Success) {
                    var opToken = new OperatorToken(m.Value.AsSpan()[0]);

                    if (opToken.Type == OperatorToken.OperatorType.Subtract && previousToken is null or OperatorToken) {
                        opToken.Type = OperatorToken.OperatorType.UnaryMinus;
                    }

                    if (opToken.Type == OperatorToken.OperatorType.ParenthesisStart) {
                        stack.Push(opToken);
                        previousToken = opToken;
                    }
                    else if (opToken.Type == OperatorToken.OperatorType.ParenthesisEnd || opToken.Type == OperatorToken.OperatorType.Comma) {
                        while (stack.Count > 0 && !(stack.Peek() is OperatorToken opParenthesis && opParenthesis.Type == OperatorToken.OperatorType.ParenthesisStart)) {
                            tokens.Add(stack.Pop());
                        }

                        if (opToken.Type != OperatorToken.OperatorType.Comma) {
                            if (stack.Count > 0 && stack.Peek() is OperatorToken op && op.Type == OperatorToken.OperatorType.ParenthesisStart) {
                                stack.Pop(); // Discard '('
                                previousToken = opToken;
                            }

                            if (stack.Count > 0 && stack.Peek() is FunctionToken) {
                                var functionToken = stack.Pop();
                                tokens.Add(functionToken); // Add the function token to the output
                                previousToken = functionToken;
                            }
                        }
                    }
                    else {
                        while (stack.Count > 0 && stack.Peek() is OperatorToken && ((OperatorToken)stack.Peek()).Priority >= opToken.Priority) {
                            tokens.Add(stack.Pop());
                        }

                        stack.Push(opToken);
                        previousToken = opToken;
                    }
                }

                m = m.NextMatch();
            }

            while (stack.Count > 0) {
                tokens.Add(stack.Pop());
            }

            result = tokens.ToArray();
        }

        public double GetResult(IValueProvider valueProvider = null) {
            var stack = new Stack<IToken>();

            foreach (var token in _tokens) {
                switch (token) {
                    case ValueToken:
                    case VariableToken:
                        stack.Push(token);
                        break;

                    case FunctionToken func when stack.Count < func.ArgsCount:
                        throw new InvalidOperationException("Not enough values for function.");

                    case FunctionToken func: {
                        stack.Push(func.Invoke(stack));
                        break;
                    }

                    case OperatorToken opUnary when opUnary.Type == OperatorToken.OperatorType.UnaryMinus && stack.Count < 1:
                    case OperatorToken op when op.Type != OperatorToken.OperatorType.UnaryMinus && stack.Count < 2:
                        throw new InvalidOperationException("Not enough values for operator.");

                    case OperatorToken op when op.Type == OperatorToken.OperatorType.UnaryMinus: {
                        var right = stack.Pop() as IValueToken;

                        if (right == null)
                            throw new InvalidOperationException("Not enough values for operator.");

                        stack.Push(new ValueToken(-right.value));
                        break;
                    }

                    case OperatorToken op: {
                        var right = stack.Pop() as IValueToken;
                        var left  = stack.Pop() as IValueToken;

                        if (right == null || left == null)
                            throw new InvalidOperationException("Not enough values for operator.");

                        stack.Push(new ValueToken(op.Type switch {
                            OperatorToken.OperatorType.Add => left.value + right.value,
                            OperatorToken.OperatorType.Subtract => left.value - right.value,
                            OperatorToken.OperatorType.Multiply => left.value * right.value,
                            OperatorToken.OperatorType.Divide => left.value / right.value,
                            OperatorToken.OperatorType.Power => Math.Pow(left.value, right.value),
                            _ => throw new Exception("unknown oper " + op.Type)
                        }));
                        break;
                    }
                    default:
                        throw new InvalidOperationException("Unknown token.");
                }
            }

            if (stack.Count != 1)
                throw new InvalidOperationException("Expression is not well-formed.");

            if (stack.Pop() is not IValueToken result)
                throw new InvalidOperationException("Expression is not well-formed.");

            return result.value;
        }

        public double Result => GetResult();
    }
}