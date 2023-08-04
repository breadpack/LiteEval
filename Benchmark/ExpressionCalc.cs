// Old code
public class ExpressionCalc {
    private string                     expression       = string.Empty;
    private Dictionary<string, double> epxxressionValue = new();
    private char[]                     operatorChar     = new char[] { '(', ')', '+', '-', '/', '^', '*' };
    Stack<string>                      stackOper        = new();
    Stack<double>                      stackValue       = new();


    //
    private static readonly char exponentChar = 'E';

    public ExpressionCalc() {
    }

    public ExpressionCalc(string expr) {
        expression = expr.Replace(" ", "");
    }

    public void SetExpression(string expr) {
        expression = expr.Replace(" ", "");
    }

    public List<string> getVariables() {
        return epxxressionValue.Select(r => r.Key).ToList();
    }

    public void Bind(string key, double value) {
        var lowKey = key.ToLowerInvariant();
        epxxressionValue[lowKey] = value;
    }

    public double Eval() {
        var lowerExpression = expression.ToLowerInvariant();

        //set bind value
        foreach (var expreValue in epxxressionValue) {
            lowerExpression = lowerExpression.Replace(expreValue.Key, $"{expreValue.Value}");
        }

        lowerExpression = CalcAlpha(lowerExpression);
        return Calc(lowerExpression);
    }

    private string CalcAlpha(string lowerExpression) {
        bool isEnd = false;
        while (!isEnd) {
            var startIndex = GetNexAlphaIndex(in lowerExpression, 0);
            if (startIndex == -1) {
                break;
            }

            var endIndex               = GetFindCharIndex(in lowerExpression, startIndex, '(');
            var methodName             = lowerExpression.Substring(startIndex, endIndex - startIndex);
            var parenthesis_StartIndex = endIndex + 1;
            var parenthesis_EndIndex   = GetParenthesisEndIndex(in lowerExpression, parenthesis_StartIndex);
            var methodExpression =
                lowerExpression.Substring(parenthesis_StartIndex, parenthesis_EndIndex - parenthesis_StartIndex);


            var replaceStr  = lowerExpression.Substring(startIndex, parenthesis_EndIndex + 1 - startIndex);
            var resultValue = GetMethodValue(methodName, methodExpression);
            lowerExpression = lowerExpression.Replace(replaceStr, resultValue);
        }

        return lowerExpression;
    }

    private string GetMethodValue(string methodName, string methodExpression) {
        while (methodExpression.Any(a => Char.IsLetter(a) && a != exponentChar)) {
            methodExpression = CalcAlpha(methodExpression);
        }

        switch (methodName) {
            case "power": {
                if (methodExpression.Contains(',')) {
                    var currentIndex  = methodExpression.IndexOf(',');
                    var firExpression = methodExpression.Substring(0, currentIndex);
                    currentIndex += 1;
                    var secondExpr = methodExpression.Substring(currentIndex, methodExpression.Length - currentIndex);
                    return $"{Math.Pow(Calc(firExpression), Calc(secondExpr))}";
                }
                else {
                    return $"{Math.Pow(Calc(methodExpression), 1)}";
                }
            }
            case "sqrt": {
                return $"{Math.Sqrt(Calc(methodExpression))}";
            }
            case "floor": {
                return $"{Math.Floor(Calc(methodExpression))}";
            }
            case "ceiling": {
                return $"{Math.Ceiling(Calc(methodExpression))}";
            }
            case "round": {
                if (methodExpression.Contains(',')) {
                    var currentIndex  = methodExpression.IndexOf(',');
                    var firExpression = methodExpression.Substring(0, currentIndex);
                    currentIndex += 1;
                    var secondExpr = methodExpression.Substring(currentIndex, methodExpression.Length - currentIndex);
                    return $"{Math.Round(Calc(firExpression), (int)Calc(secondExpr))}";
                }
                else {
                    return $"{Math.Round(Calc(methodExpression))}";
                }
                
            }
        }

        return string.Empty;
    }

    private int GetParenthesisEndIndex(in string str, int startIndex) {
        int count = 1;
        for (int i = startIndex; i < str.Length; ++i) {
            var currentChar = str[i];
            if (currentChar == ')') --count;
            else if (currentChar == '(') ++count;

            if (count <= 0) {
                return i;
            }
        }

        return -1;
    }

    private int GetNexAlphaIndex(in string str, int startIndex) {
        for (int i = startIndex; i < str.Length; ++i) {
            var currentChar = str[i];
            if (Char.IsNumber(currentChar) || currentChar == exponentChar || currentChar == '.' ||
                operatorChar.Any(r => r == currentChar)) continue;
            return i;
        }

        return -1;
    }

    private int GetFindCharIndex(in string str, int startIndex, char find) {
        for (int i = startIndex; i < str.Length; ++i) {
            var currentChar = str[i];
            if (currentChar == find)
                return i;
        }

        return -1;
    }

    private void GetToken(in string expression, out List<string> tokens) {
        tokens = new List<string>();
        if (expression.Any(a => operatorChar.Any(r => r == a)) == false) {
            tokens.Add(expression);
        }

        int startIndex = 0;
        var subString  = string.Empty;
        var endLenght  = 0;
        for (int i = 0; i < expression.Length; ++i) {
            var currentChar = expression[i];

            if (operatorChar.Any(r => r == currentChar) && expression[i - 1 <= 0 ? i : i - 1] != exponentChar) {
                endLenght = i - startIndex;
                subString = expression.Substring(startIndex, endLenght == 0 ? 1 : endLenght);
                tokens.Add(subString);
                if (endLenght != 0) {
                    subString = expression.Substring(i, 1);
                    tokens.Add(subString);
                }
                startIndex = i + 1;
            }
            else if (expression.Length == i + 1 && expression.Length - startIndex > 0) {
                endLenght = i - startIndex;
                subString = expression.Substring(startIndex, endLenght + 1);
                tokens.Add(subString);
                startIndex = i + 1;
            }
        }
    }

    private double Calc(string formula) {
        stackOper.Clear();
        stackValue.Clear();
        GetToken(formula, out var toks);

        foreach (var s in toks) {
            if (isOper(s)) {
                if (stackOper.Count == 0) {
                    stackOper.Push(s);
                }
                else {
                    if (s == "(") stackOper.Push(s);
                    else if (s == ")") {
                        // ( 올때까지 계산처리함. )
                        while (stackOper.Count > 0) {
                            var oper = stackOper.Pop();
                            if ("(".Equals(oper)) break;
                            var val = calc(stackValue.Pop(), stackValue.Pop(), oper);
                            stackValue.Push(val);
                        }
                    }
                    else {
                        // 비교해서, 기존의 operator가 우선순위가 높은 경우, 계산함. 
                        var old_priority  = get_oper_priority(stackOper.Peek());
                        var curr_priority = get_oper_priority(s);
                        if (old_priority >= curr_priority) {
                            // 계산
                            var val1 = stackValue.Pop();
                            var val2 = stackValue.Pop();
                            var val  = calc(val1, val2, stackOper.Pop());
                            stackValue.Push(val);
                            stackOper.Push(s);
                        }
                        else {
                            stackOper.Push(s);
                        }
                    }
                }
            }
            else {
                stackValue.Push(Convert.ToDouble(s));
            }
        }

        while (stackOper.Count > 0) {
            var val = calc(stackValue.Pop(), stackValue.Pop(), stackOper.Pop());
            stackValue.Push(val);
        }

        return stackValue.Pop();
    }

    private double calc(double val1, double val2, string oper) {
        if (oper == "+") return val1 + val2;
        if (oper == "-") return val2 - val1;
        if (oper == "*") return val1 * val2;
        if (oper == "*") return val1 * val2;
        if (oper == "/") return val2 / val1;
        if (oper == "^") return Math.Pow(val2, val1);
        throw new Exception("unknown oper : " + oper);
    }

    private int get_oper_priority(string oper) {
        if (oper == "(" || oper == ")") return 0;
        if (oper == "-" || oper == "+") return 1;
        if (oper == "*" || oper == "/" || oper == "^") return 2;
        throw new Exception("unknown oper " + oper);
    }

    private bool isOper(string s) {
        if (s == "(" ||
            s == ")" ||
            s == "+" ||
            s == "-" ||
            s == "*" ||
            s == "^" ||
            s == "/") return true;
        return false;
    }
}