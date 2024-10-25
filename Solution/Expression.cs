using System;
using System.Collections.Generic;
using System.Globalization;
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
                    var operatorToken = IsUnaryOperator(slice[0], previousToken)
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
                        while (stack.Count > 0 && stack.Peek().Type == TokenType.Operator &&
                               ((operatorToken.Operator.Associativity == Associativity.Left && operatorToken.Operator.Priority <= stack.Peek().Operator.Priority) ||
                                (operatorToken.Operator.Associativity == Associativity.Right && operatorToken.Operator.Priority < stack.Peek().Operator.Priority))) {
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

        private static bool IsUnaryOperator(char currentChar, Token? previousToken) {
            if (currentChar != '-')
                return false;

            if (previousToken == null)
                return true;

            switch (previousToken.Value.Type) {
                case TokenType.Value:
                case TokenType.Variable:
                case TokenType.Function:
                case TokenType.Operator when previousToken.Value.Operator.Type == OperatorType.ParenthesisEnd:
                    // 이전 토큰이 값, 변수, 닫는 괄호인 경우 이항 연산자
                    return false;
                default:
                    // 그 외의 경우 단항 연산자
                    return true;
            }
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
                                _                     => throw new("unknown oper " + token.Operator)
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

        // ExpressionPart 구조체를 정의합니다.
        struct ExpressionPart {
            public int           StartIndex;
            public int           Length;
            public int           Precedence;
            public Associativity Associativity;

            public ExpressionPart(int startIndex, int length, int precedence, Associativity associativity) {
                StartIndex    = startIndex;
                Length        = length;
                Precedence    = precedence;
                Associativity = associativity;
            }
        }

        public override string ToString() {
            // 최대 필요 길이를 추정합니다.
            int estimatedLength = Tokens.Length * 64; // 토큰당 평균 길이를 고려하여 조정하세요.

            // 미리 할당된 버퍼를 생성합니다.
            var buffer    = new char[estimatedLength];
            int bufferPos = 0;

            // 스택으로 사용할 배열을 선언합니다.
            var stack    = new ExpressionPart[Tokens.Length];
            int stackTop = -1;

            foreach (var token in Tokens) {
                switch (token.Type) {
                    case TokenType.Value: {
                        // 값 토큰을 문자열로 변환하고 버퍼에 복사합니다.
                        var valueStr = token.Value.Value.ToString(CultureInfo.InvariantCulture);
                        int start    = bufferPos;
                        foreach (char c in valueStr) {
                            buffer[bufferPos++] = c;
                        }

                        stack[++stackTop] = new(start, bufferPos - start, int.MaxValue, Associativity.None);
                        break;
                    }

                    case TokenType.Variable: {
                        unsafe {
                            // 변수 이름을 버퍼에 복사합니다.
                            var start = bufferPos;
                            buffer[bufferPos++] = '{';
                            for (int i = 0; i < 32 && token.Variable.Name[i] != '\0'; i++) {
                                buffer[bufferPos++] = token.Variable.Name[i];
                            }

                            buffer[bufferPos++] = '}';

                            stack[++stackTop] = new(start, bufferPos - start, int.MaxValue, Associativity.None);
                            break;
                        }
                    }

                    case TokenType.Function: {
                        int argCount = token.Function.ArgCount;
                        if (stackTop + 1 < argCount)
                            throw new InvalidOperationException("함수에 필요한 인자가 부족합니다: " + token.Function);

                        // 함수 호출을 버퍼에 작성합니다.
                        int funcStart = bufferPos;
                        foreach (char c in token.Function.ToString()) {
                            buffer[bufferPos++] = c;
                        }

                        buffer[bufferPos++] = '(';

                        for (int i = argCount - 1; i >= 0; i--) {
                            if (i < argCount - 1) {
                                buffer[bufferPos++] = ',';
                                buffer[bufferPos++] = ' ';
                            }

                            var argPart = stack[stackTop - i];
                            Array.Copy(buffer, argPart.StartIndex, buffer, bufferPos, argPart.Length);
                            bufferPos += argPart.Length;
                        }

                        stackTop -= argCount;

                        buffer[bufferPos++] = ')';
                        stack[++stackTop]   = new(funcStart, bufferPos - funcStart, int.MaxValue, Associativity.None);
                        break;
                    }

                    case TokenType.Operator: {
                        if (token.Operator.Type == OperatorType.UnaryMinus) {
                            // 단항 마이너스 연산자 처리
                            if (stackTop < 0)
                                throw new InvalidOperationException("연산자에 필요한 피연산자가 부족합니다: " + token.Operator);
                            var operand = stack[stackTop--];

                            bool operandNeedsParens = operand.Precedence < token.Operator.Priority;
                            int  exprStart          = bufferPos;

                            buffer[bufferPos++] = '-';
                            if (operandNeedsParens) {
                                buffer[bufferPos++] = '(';
                            }

                            // 피연산자 복사
                            Array.Copy(buffer, operand.StartIndex, buffer, bufferPos, operand.Length);
                            bufferPos += operand.Length;

                            if (operandNeedsParens) {
                                buffer[bufferPos++] = ')';
                            }

                            stack[++stackTop] = new(exprStart, bufferPos - exprStart, token.Operator.Priority, Associativity.Right);
                        }
                        else {
                            // 이항 연산자 처리
                            if (stackTop < 1)
                                throw new InvalidOperationException("연산자에 필요한 피연산자가 부족합니다: " + token.Operator);
                            var right = stack[stackTop--];
                            var left  = stack[stackTop--];

                            int           operatorPrecedence    = token.Operator.Priority;
                            Associativity operatorAssociativity = token.Operator.Associativity;

                            bool leftNeedsParens = left.Precedence < operatorPrecedence || (left.Precedence == operatorPrecedence && operatorAssociativity == Associativity.Right);

                            bool rightNeedsParens = right.Precedence < operatorPrecedence || (right.Precedence == operatorPrecedence && operatorAssociativity == Associativity.Left);

                            int exprStart = bufferPos;

                            // 왼쪽 피연산자 처리
                            if (leftNeedsParens) buffer[bufferPos++] = '(';
                            Array.Copy(buffer, left.StartIndex, buffer, bufferPos, left.Length);
                            bufferPos += left.Length;
                            if (leftNeedsParens) buffer[bufferPos++] = ')';

                            // 연산자 추가
                            buffer[bufferPos++] = ' ';
                            buffer[bufferPos++] = token.Operator.Symbol;
                            buffer[bufferPos++] = ' ';

                            // 오른쪽 피연산자 처리
                            if (rightNeedsParens) buffer[bufferPos++] = '(';
                            Array.Copy(buffer, right.StartIndex, buffer, bufferPos, right.Length);
                            bufferPos += right.Length;
                            if (rightNeedsParens) buffer[bufferPos++] = ')';

                            stack[++stackTop] = new(exprStart, bufferPos - exprStart, operatorPrecedence, operatorAssociativity);
                        }

                        break;
                    }

                    default:
                        throw new InvalidOperationException("알 수 없는 토큰 유형입니다.");
                }
            }

            if (stackTop != 0)
                throw new InvalidOperationException("잘못된 수식입니다.");

            var finalPart = stack[stackTop];
            // 버퍼에서 문자열을 생성합니다.
            return new(buffer, finalPart.StartIndex, finalPart.Length);
        }

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
            if (obj is not Expression other)
                return false;

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