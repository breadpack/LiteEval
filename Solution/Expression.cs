using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using LiteEval.Enums;
using LiteEval.Tokens;
using LiteEval.Utility;

namespace LiteEval {
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Expression {
        internal Token[]  Tokens;
        internal string[] VariableNames;

        public int    VariableCount                  => VariableNames?.Length ?? 0;
        public string GetVariableName(int index)     => VariableNames[index];

        public Expression(ReadOnlySpan<char> expr) {
            (Tokens, VariableNames) = Tokenize(expr);
        }

        public Expression(double value) {
            Tokens        = new[] { Token.CreateValueToken(value) };
            VariableNames = null;
        }

        private static (Token[], string[]) Tokenize(ReadOnlySpan<char> expression) {
            var tokens = ExpressionUtility<Token>.Rent();
            var stack  = ExpressionUtility<Token>.RentStack();

            var variableDict = new Dictionary<string, int>();
            var variableList = new List<string>();

            Token? previousToken = null;
            int    pos           = 0;

            while (pos < expression.Length) {
                var c = expression[pos];

                // Skip whitespace
                if (c == ' ' || c == '\t' || c == '\r' || c == '\n') {
                    pos++;
                    continue;
                }

                // Number: starts with digit
                if (c >= '0' && c <= '9') {
                    int start = pos;
                    while (pos < expression.Length && expression[pos] >= '0' && expression[pos] <= '9') pos++;
                    if (pos < expression.Length && expression[pos] == '.') {
                        pos++;
                        while (pos < expression.Length && expression[pos] >= '0' && expression[pos] <= '9') pos++;
                    }
                    // Scientific notation
                    if (pos < expression.Length && (expression[pos] == 'e' || expression[pos] == 'E')) {
                        int expStart = pos;
                        pos++;
                        if (pos < expression.Length && (expression[pos] == '+' || expression[pos] == '-')) pos++;
                        if (pos < expression.Length && expression[pos] >= '0' && expression[pos] <= '9') {
                            while (pos < expression.Length && expression[pos] >= '0' && expression[pos] <= '9') pos++;
                        }
                        else {
                            pos = expStart; // backtrack - not a valid exponent
                        }
                    }

                    var token = Token.CreateValueToken(expression.Slice(start, pos - start));
                    tokens.Add(token);
                    previousToken = token;
                }
                // Variable: { name }
                else if (c == '{') {
                    pos++;
                    while (pos < expression.Length && expression[pos] == ' ') pos++;
                    int nameStart = pos;
                    int nameEnd = nameStart;
                    while (pos < expression.Length && expression[pos] != '}') {
                        if (expression[pos] != ' ') nameEnd = pos + 1;
                        pos++;
                    }
                    if (pos < expression.Length && expression[pos] == '}') pos++;

                    var name = expression.Slice(nameStart, nameEnd - nameStart).ToString();
                    if (!variableDict.TryGetValue(name, out var nameIndex)) {
                        nameIndex = variableList.Count;
                        variableDict[name] = nameIndex;
                        variableList.Add(name);
                    }

                    var token = Token.CreateVariableToken(nameIndex);
                    tokens.Add(token);
                    previousToken = token;
                }
                // Identifier: function name
                else if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_') {
                    int start = pos;
                    pos++;
                    while (pos < expression.Length &&
                           ((expression[pos] >= 'a' && expression[pos] <= 'z') ||
                            (expression[pos] >= 'A' && expression[pos] <= 'Z') ||
                            (expression[pos] >= '0' && expression[pos] <= '9') ||
                            expression[pos] == '_')) {
                        pos++;
                    }

                    // Check if followed by '(' (with optional whitespace)
                    int lookahead = pos;
                    while (lookahead < expression.Length && expression[lookahead] == ' ') lookahead++;

                    if (lookahead < expression.Length && expression[lookahead] == '(') {
                        var token = Token.CreateFunctionToken(expression.Slice(start, pos - start));
                        stack.Push(token);
                        previousToken = token;
                    }
                    // else: unrecognized identifier, skip
                }
                // Operators
                else if (c == '+' || c == '-' || c == '*' || c == '/' || c == '^' ||
                         c == '(' || c == ')' || c == ',') {
                    var operatorToken = IsUnaryOperator(c, previousToken)
                        ? Token.CreateUnaryMinusToken()
                        : Token.CreateOperatorToken(c);

                    if (operatorToken.Operator.Type == OperatorType.ParenthesisStart) {
                        stack.Push(operatorToken);
                        previousToken = operatorToken;
                    }
                    else if (operatorToken.Operator.Type is OperatorType.ParenthesisEnd
                                                         or OperatorType.Comma) {
                        while (stack.Count > 0 && stack.Peek() is not { Operator: { Type: OperatorType.ParenthesisStart } }) {
                            tokens.Add(stack.Pop());
                        }

                        if (operatorToken.Operator.Type == OperatorType.Comma) {
                            previousToken = operatorToken;
                        }
                        else {
                            if (stack.Count > 0 && stack.Peek() is { Operator: { Type: OperatorType.ParenthesisStart } }) {
                                stack.Pop(); // Discard '('
                                previousToken = operatorToken;
                            }

                            if (stack.Count > 0 && stack.Peek() is { Type: TokenType.Function }) {
                                var functionToken = stack.Pop();
                                tokens.Add(functionToken);
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

                    pos++;
                }
                else {
                    pos++; // skip unknown character
                }
            }

            while (stack.Count > 0) {
                tokens.Add(stack.Pop());
            }

            var resultArray = tokens.ToArray();
            ExpressionUtility<Token>.Return(tokens);
            ExpressionUtility<Token>.ReturnStack(stack);
            return (resultArray, variableList.Count > 0 ? variableList.ToArray() : null);
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

        public int GetByteCount() {
            if (Tokens == null || Tokens.Length == 0)
                return 3;

            if (Tokens.Length > ushort.MaxValue)
                throw new InvalidOperationException("Token count exceeds maximum (65535).");

            // version(1) + tokenCount(2) + variableNameCount(2)
            int size = 5;

            // Variable name table
            if (VariableNames != null) {
                foreach (var name in VariableNames) {
                    size += 2 + name.Length * 2; // charCount(2) + chars
                }
            }

            // Tokens
            foreach (var token in Tokens) {
                switch (token.Type) {
                    case TokenType.Value:
                        size += 9; // type(1) + double(8)
                        break;
                    case TokenType.Operator:
                    case TokenType.Function:
                        size += 2; // type(1) + enumByte(1)
                        break;
                    case TokenType.Variable:
                        size += 3; // type(1) + nameIndex(2)
                        break;
                }
            }

            return size;
        }

        public bool TryWriteBytes(Span<byte> destination, out int bytesWritten) {
            int size = GetByteCount();
            if (destination.Length < size) {
                bytesWritten = 0;
                return false;
            }

            if (Tokens == null || Tokens.Length == 0) {
                destination[0] = 2; // version
                destination[1] = 0;
                destination[2] = 0;
                bytesWritten = 3;
                return true;
            }

            int pos = 0;

            // Header
            destination[pos++] = 2; // version
            BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(pos), (ushort)Tokens.Length);
            pos += 2;
            BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(pos), (ushort)(VariableNames?.Length ?? 0));
            pos += 2;

            // Variable name table
            if (VariableNames != null) {
                foreach (var name in VariableNames) {
                    BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(pos), (ushort)name.Length);
                    pos += 2;
                    for (int j = 0; j < name.Length; j++) {
                        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(pos), name[j]);
                        pos += 2;
                    }
                }
            }

            // Tokens
            foreach (var token in Tokens) {
                destination[pos++] = (byte)token.Type;
                switch (token.Type) {
                    case TokenType.Value:
                        BinaryPrimitives.WriteInt64LittleEndian(destination.Slice(pos),
                            BitConverter.DoubleToInt64Bits(token.Value.Value));
                        pos += 8;
                        break;
                    case TokenType.Operator:
                        destination[pos++] = (byte)token.Operator.Type;
                        break;
                    case TokenType.Function:
                        destination[pos++] = (byte)token.Function.Type;
                        break;
                    case TokenType.Variable:
                        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(pos), (ushort)token.Variable.NameIndex);
                        pos += 2;
                        break;
                }
            }

            bytesWritten = pos;
            return true;
        }

        public byte[] ToBytes() {
            int size = GetByteCount();
            var bytes = new byte[size];
            TryWriteBytes(bytes, out _);
            return bytes;
        }

        public static Expression FromBytes(ReadOnlySpan<byte> bytes) {
            if (bytes.Length < 3)
                throw new ArgumentException("Invalid binary data: too short.");

            byte version = bytes[0];
            return version switch {
                1 => FromBytesV1(bytes),
                2 => FromBytesV2(bytes),
                _ => throw new NotSupportedException($"Unsupported binary format version: {version}")
            };
        }

        private static Expression FromBytesV1(ReadOnlySpan<byte> bytes) {
            ushort tokenCount = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(1));
            if (tokenCount == 0)
                return default;

            var tokens       = new Token[tokenCount];
            var nameDict     = new Dictionary<string, int>();
            var nameList     = new List<string>();
            int pos          = 3;

            for (int i = 0; i < tokenCount; i++) {
                var type = (TokenType)bytes[pos++];
                switch (type) {
                    case TokenType.Value:
                        tokens[i] = Token.CreateValueToken(
                            BitConverter.Int64BitsToDouble(
                                BinaryPrimitives.ReadInt64LittleEndian(bytes.Slice(pos))));
                        pos += 8;
                        break;
                    case TokenType.Operator:
                        tokens[i] = Token.CreateOperatorToken((OperatorType)bytes[pos++]);
                        break;
                    case TokenType.Function:
                        tokens[i] = Token.CreateFunctionToken((FunctionType)bytes[pos++]);
                        break;
                    case TokenType.Variable:
                        int nameLen = bytes[pos++];
                        Span<char> name = stackalloc char[nameLen];
                        for (int j = 0; j < nameLen; j++) {
                            name[j] = (char)BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(pos));
                            pos += 2;
                        }
                        var nameStr = new string(name);
                        if (!nameDict.TryGetValue(nameStr, out var nameIndex)) {
                            nameIndex = nameList.Count;
                            nameDict[nameStr] = nameIndex;
                            nameList.Add(nameStr);
                        }
                        tokens[i] = Token.CreateVariableToken(nameIndex);
                        break;
                    default:
                        throw new ArgumentException($"Unknown token type: {type}");
                }
            }

            return new Expression {
                Tokens        = tokens,
                VariableNames = nameList.Count > 0 ? nameList.ToArray() : null
            };
        }

        private static Expression FromBytesV2(ReadOnlySpan<byte> bytes) {
            ushort tokenCount = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(1));
            if (tokenCount == 0)
                return default;

            ushort varNameCount = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(3));
            int pos = 5;

            // Read variable name table
            string[] variableNames = null;
            if (varNameCount > 0) {
                variableNames = new string[varNameCount];
                for (int i = 0; i < varNameCount; i++) {
                    ushort charCount = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(pos));
                    pos += 2;
                    Span<char> chars = charCount <= 256
                        ? stackalloc char[charCount]
                        : new char[charCount];
                    for (int j = 0; j < charCount; j++) {
                        chars[j] = (char)BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(pos));
                        pos += 2;
                    }
                    variableNames[i] = new string(chars);
                }
            }

            // Read tokens
            var tokens = new Token[tokenCount];
            for (int i = 0; i < tokenCount; i++) {
                var type = (TokenType)bytes[pos++];
                switch (type) {
                    case TokenType.Value:
                        tokens[i] = Token.CreateValueToken(
                            BitConverter.Int64BitsToDouble(
                                BinaryPrimitives.ReadInt64LittleEndian(bytes.Slice(pos))));
                        pos += 8;
                        break;
                    case TokenType.Operator:
                        tokens[i] = Token.CreateOperatorToken((OperatorType)bytes[pos++]);
                        break;
                    case TokenType.Function:
                        tokens[i] = Token.CreateFunctionToken((FunctionType)bytes[pos++]);
                        break;
                    case TokenType.Variable:
                        ushort nameIndex = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(pos));
                        pos += 2;
                        tokens[i] = Token.CreateVariableToken(nameIndex);
                        break;
                    default:
                        throw new ArgumentException($"Unknown token type: {type}");
                }
            }

            return new Expression {
                Tokens        = tokens,
                VariableNames = variableNames
            };
        }

        public double GetResult() {
            unsafe {
                if (Tokens == null || Tokens.Length == 0) {
                    return 0;
                }

                // Resolve unique variable values once
                Span<double> resolved = default;
                if (VariableNames != null && VariableNames.Length > 0) {
                    if (VariableNames.Length <= 64)
                        resolved = stackalloc double[VariableNames.Length];
                    else
                        resolved = new double[VariableNames.Length];
                    for (int i = 0; i < VariableNames.Length; i++) {
                        if (!ValueProviderContext.TryGetValue(VariableNames[i], out resolved[i])) {
                            throw new InvalidOperationException("Variable not found. variableName:" + VariableNames[i]);
                        }
                    }
                }

                var stack = stackalloc double[Tokens.Length];
                var top   = -1;

                foreach (var token in Tokens) {
                    switch (token.Type) {
                        case TokenType.Value:
                            stack[++top] = token.Value;
                            break;
                        case TokenType.Variable:
                            stack[++top] = resolved[token.Variable.NameIndex];
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

        public double GetResult(ReadOnlySpan<double> variableValues) {
            unsafe {
                if (Tokens == null || Tokens.Length == 0) {
                    return 0;
                }

                int varCount = VariableNames?.Length ?? 0;
                if (variableValues.Length < varCount) {
                    throw new ArgumentException(
                        $"Expected at least {varCount} variable values, but got {variableValues.Length}.");
                }

                var stack = stackalloc double[Tokens.Length];
                var top   = -1;

                foreach (var token in Tokens) {
                    switch (token.Type) {
                        case TokenType.Value:
                            stack[++top] = token.Value;
                            break;
                        case TokenType.Variable:
                            stack[++top] = variableValues[token.Variable.NameIndex];
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
            if (Tokens == null || Tokens.Length == 0)
                return "0";

            // 최대 필요 길이를 추정합니다.
            // Postfix-to-infix conversion copies sub-expressions at each operator,
            // so total buffer can grow quadratically for long variable names.
            int maxPerToken = 32;
            if (VariableNames != null) {
                foreach (var name in VariableNames)
                    maxPerToken = Math.Max(maxPerToken, name.Length + 4);
            }
            int estimatedLength = Tokens.Length * maxPerToken;
            if (maxPerToken > 32) {
                estimatedLength = Math.Max(estimatedLength,
                    Tokens.Length * Tokens.Length * maxPerToken / 2);
            }
            estimatedLength = Math.Max(estimatedLength, 16);

            char[] pooledBuffer = null;
            Span<char> buffer = estimatedLength <= 512
                ? stackalloc char[estimatedLength]
                : (pooledBuffer = ArrayPool<char>.Shared.Rent(estimatedLength));

            ExpressionPart[] pooledStack = null;
            Span<ExpressionPart> stack = Tokens.Length <= 32
                ? stackalloc ExpressionPart[Tokens.Length]
                : (pooledStack = ArrayPool<ExpressionPart>.Shared.Rent(Tokens.Length));

            try {
                int bufferPos = 0;
                int stackTop  = -1;

                foreach (var token in Tokens) {
                    switch (token.Type) {
                        case TokenType.Value: {
                            var valueStr = token.Value.Value.ToString(CultureInfo.InvariantCulture);
                            int start    = bufferPos;
                            foreach (char c in valueStr) {
                                buffer[bufferPos++] = c;
                            }

                            stack[++stackTop] = new(start, bufferPos - start, int.MaxValue, Associativity.None);
                            break;
                        }

                        case TokenType.Variable: {
                            var start   = bufferPos;
                            var varName = VariableNames[token.Variable.NameIndex];
                            buffer[bufferPos++] = '{';
                            foreach (char c in varName) {
                                buffer[bufferPos++] = c;
                            }
                            buffer[bufferPos++] = '}';

                            stack[++stackTop] = new(start, bufferPos - start, int.MaxValue, Associativity.None);
                            break;
                        }

                        case TokenType.Function: {
                            int argCount = token.Function.ArgCount;
                            if (stackTop + 1 < argCount)
                                throw new InvalidOperationException("함수에 필요한 인자가 부족합니다: " + token.Function);

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
                                buffer.Slice(argPart.StartIndex, argPart.Length).CopyTo(buffer.Slice(bufferPos));
                                bufferPos += argPart.Length;
                            }

                            stackTop -= argCount;

                            buffer[bufferPos++] = ')';
                            stack[++stackTop]   = new(funcStart, bufferPos - funcStart, int.MaxValue, Associativity.None);
                            break;
                        }

                        case TokenType.Operator: {
                            if (token.Operator.Type == OperatorType.UnaryMinus) {
                                if (stackTop < 0)
                                    throw new InvalidOperationException("연산자에 필요한 피연산자가 부족합니다: " + token.Operator);
                                var operand = stack[stackTop--];

                                bool operandNeedsParens = operand.Precedence < token.Operator.Priority;
                                int  exprStart          = bufferPos;

                                buffer[bufferPos++] = '-';
                                if (operandNeedsParens) {
                                    buffer[bufferPos++] = '(';
                                }

                                buffer.Slice(operand.StartIndex, operand.Length).CopyTo(buffer.Slice(bufferPos));
                                bufferPos += operand.Length;

                                if (operandNeedsParens) {
                                    buffer[bufferPos++] = ')';
                                }

                                stack[++stackTop] = new(exprStart, bufferPos - exprStart, token.Operator.Priority, Associativity.Right);
                            }
                            else {
                                if (stackTop < 1)
                                    throw new InvalidOperationException("연산자에 필요한 피연산자가 부족합니다: " + token.Operator);
                                var right = stack[stackTop--];
                                var left  = stack[stackTop--];

                                int           operatorPrecedence    = token.Operator.Priority;
                                Associativity operatorAssociativity = token.Operator.Associativity;

                                bool leftNeedsParens = left.Precedence < operatorPrecedence || (left.Precedence == operatorPrecedence && operatorAssociativity == Associativity.Right);

                                bool rightNeedsParens = right.Precedence < operatorPrecedence || (right.Precedence == operatorPrecedence && operatorAssociativity == Associativity.Left);

                                int exprStart = bufferPos;

                                if (leftNeedsParens) buffer[bufferPos++] = '(';
                                buffer.Slice(left.StartIndex, left.Length).CopyTo(buffer.Slice(bufferPos));
                                bufferPos += left.Length;
                                if (leftNeedsParens) buffer[bufferPos++] = ')';

                                buffer[bufferPos++] = ' ';
                                buffer[bufferPos++] = token.Operator.Symbol;
                                buffer[bufferPos++] = ' ';

                                if (rightNeedsParens) buffer[bufferPos++] = '(';
                                buffer.Slice(right.StartIndex, right.Length).CopyTo(buffer.Slice(bufferPos));
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
                return new string(buffer.Slice(finalPart.StartIndex, finalPart.Length));
            }
            finally {
                if (pooledBuffer != null) ArrayPool<char>.Shared.Return(pooledBuffer);
                if (pooledStack != null) ArrayPool<ExpressionPart>.Shared.Return(pooledStack);
            }
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

            // Merge variable name tables
            string[] mergedNames   = null;
            int[]    rightIndexMap = null;
            var      leftNames    = left.VariableNames;
            var      rightNames   = right.VariableNames;

            if (leftNames != null || rightNames != null) {
                var nameDict = new Dictionary<string, int>();
                var nameList = new List<string>();

                if (leftNames != null) {
                    for (int i = 0; i < leftNames.Length; i++) {
                        nameDict[leftNames[i]] = i;
                        nameList.Add(leftNames[i]);
                    }
                }

                if (rightNames != null) {
                    rightIndexMap = new int[rightNames.Length];
                    for (int i = 0; i < rightNames.Length; i++) {
                        if (!nameDict.TryGetValue(rightNames[i], out var idx)) {
                            idx = nameList.Count;
                            nameDict[rightNames[i]] = idx;
                            nameList.Add(rightNames[i]);
                        }
                        rightIndexMap[i] = idx;
                    }
                }

                mergedNames = nameList.ToArray();
            }

            var combinedTokens = new Token[left.Tokens.Length + right.Tokens.Length + 1];
            left.Tokens.CopyTo(combinedTokens, 0);

            for (int i = 0; i < right.Tokens.Length; i++) {
                var token = right.Tokens[i];
                if (token.Type == TokenType.Variable && rightIndexMap != null) {
                    token = Token.CreateVariableToken(rightIndexMap[token.Variable.NameIndex]);
                }
                combinedTokens[left.Tokens.Length + i] = token;
            }

            combinedTokens[left.Tokens.Length + right.Tokens.Length] = Token.CreateOperatorToken(operatorType);

            return new Expression {
                Tokens        = combinedTokens,
                VariableNames = mergedNames
            };
        }

        public override bool Equals(object obj) {
            if (obj is not Expression other)
                return false;

            if (Tokens == null && other.Tokens == null)
                return true;

            if (Tokens == null || other.Tokens == null)
                return false;

            if (Tokens.Length != other.Tokens.Length)
                return false;

            int varCount      = VariableNames?.Length ?? 0;
            int otherVarCount = other.VariableNames?.Length ?? 0;
            if (varCount != otherVarCount)
                return false;

            for (int i = 0; i < varCount; i++) {
                if (VariableNames[i] != other.VariableNames[i])
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
            if (VariableNames != null) {
                foreach (var name in VariableNames) {
                    hash = hash * 31 + name.GetHashCode();
                }
            }
            if (Tokens != null) {
                foreach (var token in Tokens) {
                    hash = hash * 31 + token.GetHashCode();
                }
            }
            return hash;
        }

        public static bool operator ==(Expression left, Expression right) => Math.Abs(left.GetResult() - right.GetResult()) < double.Epsilon;
        public static bool operator !=(Expression left, Expression right) => !(left == right);
    }
}
