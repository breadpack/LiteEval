using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using LiteEval.Enums;
using LiteEval.Tokens;

namespace LiteEval {
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Expression {
        private static readonly Regex Regex = new(
            @"(?<number>\d+(\.\d+)?)|(?<function>[a-zA-Z_][_a-zA-Z0-9]+(?=\s*\())|{\s*(?<variable>[a-zA-Z_][\._a-zA-Z0-9]*)\s*}|(?<operator>[+/*^()-])",
            RegexOptions.Compiled);

        private static readonly char exponentChar = 'E';

        internal Token[] Tokens;

        public Expression(ReadOnlySpan<char> expr) {
            Tokens = Tokenize(expr);
        }

        public Expression(double value) {
            Tokens = new[] { Token.CreateValueToken(value) };
        }

        private static Token[] Tokenize(ReadOnlySpan<char> expression) {
            var    tokens        = new List<Token>();
            var    stack         = new Stack<Token>();
            Token? previousToken = null;

            var m = Regex.Match(expression.ToString());
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
                    var operatorToken = slice[0] == '-' && (previousToken is null || previousToken.Value.Type == TokenType.Operator)
                                            ? Token.CreateUnaryMinusToken()
                                            : Token.CreateOperatorToken(slice[0]);

                    if (operatorToken.Operator.Type == OperatorType.ParenthesisStart) {
                        stack.Push(operatorToken);
                        previousToken = operatorToken;
                    }
                    else if (operatorToken.Operator.Type is OperatorType.ParenthesisEnd
                                                         or OperatorType.Comma) {
                        while (stack.Count > 0 && stack.Peek() is not { Operator : { Type: OperatorType.ParenthesisStart } }) {
                            tokens.Add(stack.Pop());
                        }

                        if (operatorToken.Operator.Type != OperatorType.Comma) {
                            if (stack.Count > 0 && stack.Peek() is { Operator : { Type : OperatorType.ParenthesisStart } }) {
                                stack.Pop(); // Discard '('
                                previousToken = operatorToken;
                            }

                            if (stack.Count > 0 && stack.Peek() is { Type : TokenType.Function }) {
                                var functionToken = stack.Pop();
                                tokens.Add(functionToken); // Add the function token to the output
                                previousToken = functionToken;
                            }
                        }
                    }
                    else {
                        while (stack.Count > 0 && stack.Peek() is { Type : TokenType.Operator } && stack.Peek().Operator.Priority >= operatorToken.Operator.Priority) {
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
                if (Tokens == null || Tokens.Length == 0) {
                    return 0;
                }
                
                var stack = stackalloc double[Tokens.Length];
                var top   = -1;

                foreach (var token in Tokens) {
                    switch (token.Type) {
                        case TokenType.Value:
                            stack[++top] = token.Value;
                            break;
                        case TokenType.Variable:
                            if (!ValueProviderContext.TryGetValue(token.Variable, out var variableValue)) {
                                throw new InvalidOperationException("Variable not found. variableName:" + token.Variable);
                            }

                            stack[++top] = variableValue;
                            break;

                        case TokenType.Function when top + 1 < token.Function.ArgCount:
                            throw new InvalidOperationException("Not enough values for function.");

                        case TokenType.Function: {
                            var result = token.Function.Invoke(stack, ref top);
                            stack[++top] = result;
                            break;
                        }

                        case TokenType.Operator when token.Operator == OperatorType.UnaryMinus && top + 1 < 1:
                            throw new InvalidOperationException("Not enough values for operator unary minus.");
                        case TokenType.Operator when token.Operator != OperatorType.UnaryMinus && top + 1 < 2:
                            throw new InvalidOperationException("Not enough values for operator.");

                        case TokenType.Operator when token.Operator == OperatorType.UnaryMinus: {
                            stack[top] = -stack[top];
                            break;
                        }

                        case TokenType.Operator: {
                            var right = stack[top--];
                            var left  = stack[top--];

                            stack[++top] = token.Operator.Type switch {
                                OperatorType.Add      => left + right,
                                OperatorType.Subtract => left - right,
                                OperatorType.Multiply => left * right,
                                OperatorType.Divide   => left / right,
                                OperatorType.Power    => Math.Pow(left, right),
                                _                     => throw new Exception("unknown oper " + token.Operator)
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
        
        public static Expression operator +(Expression left, Expression right) => CombineTokens(left, right, OperatorType.Add);
        public static Expression operator -(Expression left, Expression right) => CombineTokens(left, right, OperatorType.Subtract);
        public static Expression operator *(Expression left, Expression right) => CombineTokens(left, right, OperatorType.Multiply);
        public static Expression operator /(Expression left, Expression right) => CombineTokens(left, right, OperatorType.Divide);
        public static Expression operator ^(Expression left, Expression right) => CombineTokens(left, right, OperatorType.Power);
        
        public static Expression operator +(Expression left, double right) => left + new Expression(right);
        public static Expression operator -(Expression left, double right) => left - new Expression(right);
        public static Expression operator *(Expression left, double right) => left * new Expression(right);
        public static Expression operator /(Expression left, double right) => left / new Expression(right);
        public static Expression operator ^(Expression left, double right) => left ^ new Expression(right);
        
        private static Expression CombineTokens(Expression left, Expression right, OperatorType operatorType) {
            if (left.Tokens == null || left.Tokens.Length == 0) {
                return right;
            }
            if (right.Tokens == null || right.Tokens.Length == 0) {
                return left;
            }
            
            var combinedTokens = new Token[left.Tokens.Length + right.Tokens.Length + 1];
            left.Tokens.CopyTo(combinedTokens, 0);
            right.Tokens.CopyTo(combinedTokens, left.Tokens.Length);
            combinedTokens[left.Tokens.Length + right.Tokens.Length] = Token.CreateOperatorToken(operatorType);
            return new() { Tokens = combinedTokens };
        }

        public override bool Equals(object obj) {
            if (obj is Expression other) {
                if (Tokens.Length != other.Tokens.Length) {
                    return false;
                }
                for (int i = 0; i < Tokens.Length; i++) {
                    if (!Tokens[i].Equals(other.Tokens[i])) {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        public override int GetHashCode() {
            var hash = 17;
            foreach (var token in Tokens) {
                hash = hash * 31 + token.GetHashCode();
            }
            return hash;
        }

        public static bool operator ==(Expression left, Expression right) => Math.Abs(left.GetResult() - right.GetResult()) < double.Epsilon;
        public static bool operator !=(Expression left, Expression right) => !(left == right);
    }
}