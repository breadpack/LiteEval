using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LiteEval {
    public class Expression {
        private static readonly char exponentChar = 'E';

        private ReadOnlyMemory<char> _expression;

        public string expression {
            get => _expression.ToString();
            set {
                _expression = value.AsMemory();
                GetToken(in value, out _tokens);
            }
        }

        private readonly DefaultValueProvider _valueProvider = new();
        private static   IValueProvider       _globalProvider;
        private          IValueProvider       _customProvider;
        private          IToken[]             _tokens;
        
        public static void SetGlobalValueProvider(IValueProvider provider) {
            _globalProvider = provider;
        }
        
        public void SetCustomValueProvider(IValueProvider provider) {
            _customProvider = provider;
        }

        public double this[string name] {
            get => this[name.AsMemory()];
            set => this[name.AsMemory()] = value;
        }

        public double this[ReadOnlyMemory<char> name] {
            get {
                if (_globalProvider?.TryGetValue(name, out var value) ?? false)
                    return value;
                if (_customProvider?.TryGetValue(name, out value) ?? false)
                    return value;
                return _valueProvider.TryGetValue(name, out value) ? value : 0;
            }
            set => _valueProvider[name] = value;
        }

        public Expression() { }

        public Expression(string expr) {
            expression = expr;
        }

        private static readonly Regex Regex = new Regex(@"(?<number>\d+(\.\d+)?)|(?<function>[a-zA-Z_][_a-zA-Z0-9]+(?=\s*\())|{\s*(?<variable>[a-zA-Z_][\._a-zA-Z0-9]*)\s*}|(?<operator>[+/*^()-])", RegexOptions.Compiled);

        private void GetToken(in string expression, out IToken[] result) {
            var    tokens        = new List<IToken>();
            var    m             = Regex.Match(expression);
            IToken previousToken = null;
            var    stack         = new Stack<IToken>();

            while (m.Success) {
                if (m.Groups["number"].Success) {
                    var valueToken = new ValueToken(_expression.Slice(m.Index, m.Length));
                    tokens.Add(valueToken);
                    previousToken = valueToken;
                }
                else if (m.Groups["variable"].Success) {
                    var g             = m.Groups["variable"];
                    var variableToken = new VariableToken(this, _expression.Slice(g.Index, g.Length));
                    tokens.Add(variableToken);
                    previousToken = variableToken;
                }
                else if (m.Groups["function"].Success) {
                    var functionToken = new FunctionToken(_expression.Slice(m.Index, m.Length));
                    stack.Push(functionToken);
                    previousToken = functionToken;
                }
                else if (m.Groups["operator"].Success) {
                    var opToken = new OperatorToken(_expression.Slice(m.Index, m.Length).Span[0]);

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
            unsafe {
                if (_expression.Length == 0 || _tokens == null || _tokens.Length == 0) {
                    throw new InvalidOperationException($"Expression is not well-formed. (expression:{_expression})");
                }

                var stack = stackalloc double[_tokens.Length];
                var top   = -1;

                foreach (var token in _tokens) {
                    switch (token) {
                        case ValueToken valueToken:
                            stack[++top] = valueToken.value;
                            break;
                        case VariableToken variableToken:
                            stack[++top] = variableToken.value;
                            break;

                        case FunctionToken func when top + 1 < func.ArgsCount:
                            throw new InvalidOperationException("Not enough values for function.");

                        case FunctionToken func: {
                            var result = func.Invoke(stack, ref top);
                            stack[++top] = result;
                            break;
                        }

                        case OperatorToken opUnary when opUnary.Type == OperatorToken.OperatorType.UnaryMinus && top + 1 < 1:
                        case OperatorToken op when op.Type != OperatorToken.OperatorType.UnaryMinus && top + 1 < 2:
                            throw new InvalidOperationException("Not enough values for operator.");

                        case OperatorToken op when op.Type == OperatorToken.OperatorType.UnaryMinus: {
                            stack[top] = -stack[top];
                            break;
                        }

                        case OperatorToken op: {
                            var right = stack[top--];
                            var left  = stack[top--];

                            if (right == null || left == null)
                                throw new InvalidOperationException("Not enough values for operator.");

                            stack[++top] = op.Type switch {
                                OperatorToken.OperatorType.Add   => left + right, OperatorToken.OperatorType.Subtract => left - right, OperatorToken.OperatorType.Multiply => left * right, OperatorToken.OperatorType.Divide => left / right
                              , OperatorToken.OperatorType.Power => Math.Pow(left, right), _                          => throw new Exception("unknown oper " + op.Type)
                            };
                            break;
                        }
                        default:
                            throw new InvalidOperationException("Unknown token.");
                    }
                }

                if (top != 0)
                    throw new InvalidOperationException("Expression is not well-formed.");

                return stack[0];
            }
        }

        public double Result => GetResult();
    }
}