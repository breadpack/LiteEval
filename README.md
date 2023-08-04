# LiteEval
LiteEval is a lightweight, easy-to-use, and fast expression evaluator for C#.

Expression 객체를 생성할 때 Tokenize를 수행하고 실행 시에는 최소한의 메모리 할당과 연산만 일어납니다. 따라서 한번 생성한 Expression을 많이 재활용하는 것이 좋습니다.

단일 C# 파일로 구성되어 있어 Unity 등에서도 사용할 수 있습니다.

## Features
- 기본 사칙연산을 지원합니다.
- 변수를 사용할 수 있습니다.
- System.Math에서 제공하는 함수를 사용할 수 있습니다.

## Usage
```csharp
// 연산자 우선순위에 따른 사칙연산
Console.WriteLine(new Expression("2*5-20/4").Result); // 5

// Unary minus 연산자
Console.WriteLine(new Expression("-5-5--5").Result); // -5

// 변수 사용
Expression exp = new Expression("2*x+3");
exp._expressionValue["x"] = 5;
Assert.AreEqual(13, exp.Result);

// System.Math에서 제공하는 함수 사용
Expression exp = new Expression("-sin(0+0)");
Assert.AreEqual(0, exp.Result);

// 변수와 System.Math에서 제공하는 함수를 함께 사용
Expression exp = new Expression("2*x + sin(y)");
exp._expressionValue["x"] = 5;
exp._expressionValue["y"] = Math.PI / 2;
Assert.AreEqual(11, exp.Result);
```

