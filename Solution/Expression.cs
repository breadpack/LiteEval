using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace LiteEval {
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Expression {
        private static readonly Regex Regex = new(
            @"(?<number>\d+(\.\d+)?)|(?<function>[a-zA-Z_][_a-zA-Z0-9]+(?=\s*\())|{\s*(?<variable>[a-zA-Z_][\._a-zA-Z0-9]*)\s*}|(?<operator>[+/*^()-])",
            RegexOptions.Compiled);

        private static readonly char exponentChar = 'E';

        public Token[] Tokens;

        public Expression(ReadOnlySpan<char> expr) {
            Tokens = Tokenize(expr);
        }

        private static Token[] Tokenize(ReadOnlySpan<char> expression) {
            var    tokens        = new List<Token>();
            var    m             = Regex.Match(expression.ToString());
            Token? previousToken = null;
            var    stack         = new Stack<Token>();

            while (m.Success) {
                var slice = expression.Slice(m.Index, m.Length);

                if (m.Groups["number"].Success) {
                    var token = Token.CreateValueToken(slice);

                    tokens.Add(token);
                    previousToken = token;
                }
                else if (m.Groups["variable"].Success) {
                    var g     = m.Groups["variable"];
                    var token = Token.CreateVariableToken(expression.Slice(g.Index, g.Length));
                    tokens.Add(token);
                    previousToken = token;
                }
                else if (m.Groups["function"].Success) {
                    var token = Token.CreateFunctionToken(slice);
                    stack.Push(token);
                    previousToken = token;
                }
                else if (m.Groups["operator"].Success) {
                    var operatorToken = slice[0] == '-' && (previousToken is null || previousToken.Value.Type == Token.TokenType.Operator)
                                            ? Token.CreateUnaryMinusToken()
                                            : Token.CreateOperatorToken(slice[0]);

                    if (operatorToken.Operator == Token.OperatorType.ParenthesisStart) {
                        stack.Push(operatorToken);
                        previousToken = operatorToken;
                    }
                    else if (operatorToken.Operator is Token.OperatorType.ParenthesisEnd
                                                    or Token.OperatorType.Comma) {
                        while (stack.Count > 0 && stack.Peek() is not { Operator : Token.OperatorType.ParenthesisStart }) {
                            tokens.Add(stack.Pop());
                        }

                        if (operatorToken.Operator != Token.OperatorType.Comma) {
                            if (stack.Count > 0 && stack.Peek() is { Operator : Token.OperatorType.ParenthesisStart }) {
                                stack.Pop(); // Discard '('
                                previousToken = operatorToken;
                            }

                            if (stack.Count > 0 && stack.Peek() is { Type : Token.TokenType.Function }) {
                                var functionToken = stack.Pop();
                                tokens.Add(functionToken); // Add the function token to the output
                                previousToken = functionToken;
                            }
                        }
                    }
                    else {
                        while (stack.Count > 0 && stack.Peek() is { Type : Token.TokenType.Operator } && stack.Peek().Priority >= operatorToken.Priority) {
                            tokens.Add(stack.Pop());
                        }

                        stack.Push(operatorToken);
                        previousToken = operatorToken;
                    }
                }

                m = m.NextMatch();
            }

            while (stack.Count > 0) {
                tokens.Add(stack.Pop());
            }

            return tokens.ToArray();
        }

        public double GetResult() {
            unsafe {
                var stack = stackalloc double[Tokens.Length];
                var top   = -1;

                foreach (var token in Tokens) {
                    switch (token.Type) {
                        case Token.TokenType.Value:
                            stack[++top] = token.Value;
                            break;
                        case Token.TokenType.Variable:
                            
                            var length = 0;
                            while (length < 32 && token.VariableName[length] != '\0')
                            {
                                length++;
                            }
                            
                            var name = new Span<char>(token.VariableName, length);
                            if (!ValueProviderContext.TryGetValue(name, out var variableValue)) {
                                throw new InvalidOperationException("Variable not found. variableName:" + name.ToString());
                            }
                            stack[++top] = variableValue;
                            break;
                        
                        case Token.TokenType.Function when top + 1 < token.ArgCount:
                            throw new InvalidOperationException("Not enough values for function.");

                        case Token.TokenType.Function: {
                            var result = token.Invoke(stack, ref top);
                            stack[++top] = result;
                            break;
                        }

                        case Token.TokenType.Operator when token.Operator == Token.OperatorType.UnaryMinus && top + 1 < 1:
                            throw new InvalidOperationException("Not enough values for operator unary minus.");
                        case Token.TokenType.Operator when token.Operator != Token.OperatorType.UnaryMinus && top + 1 < 2:
                            throw new InvalidOperationException("Not enough values for operator.");

                        case Token.TokenType.Operator when token.Operator == Token.OperatorType.UnaryMinus: {
                            stack[top] = -stack[top];
                            break;
                        }

                        case Token.TokenType.Operator: {
                            var right = stack[top--];
                            var left  = stack[top--];

                            stack[++top] = token.Operator switch {
                                Token.OperatorType.Add      => left + right,
                                Token.OperatorType.Subtract => left - right,
                                Token.OperatorType.Multiply => left * right,
                                Token.OperatorType.Divide   => left / right,
                                Token.OperatorType.Power    => Math.Pow(left, right),
                                _                           => throw new Exception("unknown oper " + token.Operator)
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