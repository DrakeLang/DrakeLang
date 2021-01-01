//------------------------------------------------------------------------------
// DrakeLang - Viv's C#-esque sandbox.
// Copyright (C) 2019  Vivian Vea
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.
//------------------------------------------------------------------------------

using DrakeLang.Symbols;
using DrakeLang.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using static DrakeLang.Symbols.SystemSymbols;

namespace DrakeLang.Tests
{
    public class EvaluationTests
    {
        #region Evaluates

        #region Bool

        [Theory]
        [InlineData("var result = true;", true)]
        [InlineData("var result = false;", false)]
        [InlineData("var result = !true;", false)]
        [InlineData("var result = !false;", true)]
        [InlineData("var result = true || true;", true)]
        [InlineData("var result = true || false;", true)]
        [InlineData("var result = false || true;", true)]
        [InlineData("var result = false || false;", false)]
        [InlineData("var result = true && true;", true)]
        [InlineData("var result = true && false;", false)]
        [InlineData("var result = false && true;", false)]
        [InlineData("var result = false && false;", false)]
        [InlineData("var result = false == false;", true)]
        [InlineData("var result = true == false;", false)]
        [InlineData("var result = false != false;", false)]
        [InlineData("var result = true != false;", true)]
        [InlineData("var result = false | false;", false)]
        [InlineData("var result = false | true;", true)]
        [InlineData("var result = true | false;", true)]
        [InlineData("var result = true | true;", true)]
        [InlineData("var result = false & false;", false)]
        [InlineData("var result = false & true;", false)]
        [InlineData("var result = true & false;", false)]
        [InlineData("var result = true & true;", true)]
        [InlineData("var result = false ^ false;", false)]
        [InlineData("var result = false ^ true;", true)]
        [InlineData("var result = true ^ false;", true)]
        [InlineData("var result = true ^ true;", false)]
        [InlineData("var result = true; result &= false;", false)]
        [InlineData("var result = true; result &= true;", true)]
        [InlineData("var result = false; result &= false;", false)]
        [InlineData("var result = false; result &= true;", false)]
        [InlineData("var result = true; result |= false;", true)]
        [InlineData("var result = true; result |= true;", true)]
        [InlineData("var result = false; result |= false;", false)]
        [InlineData("var result = false; result |= true;", true)]
        public void Evaluator_Computes_BooleanStatements(string text, bool expectedValue) => AssertValue(text, expectedValue);

        #endregion Bool

        #region Int

        [Theory]
        [InlineData("var result = 1;", 1)]
        [InlineData("var result = +1;", 1)]
        [InlineData("var result = -1;", -1)]
        [InlineData("var result = ~1;", ~1)]
        [InlineData("var result = 14 + 12;", 26)]
        [InlineData("var result = 12 - 3;", 9)]
        [InlineData("var result = 4 * 2;", 8)]
        [InlineData("var result = 9 / 3;", 3)]
        [InlineData("var result = (10);", 10)]
        [InlineData("var result = 12 == 3;", false)]
        [InlineData("var result = 3 == 3;", true)]
        [InlineData("var result = 12 != 3;", true)]
        [InlineData("var result = 3 != 3;", false)]
        [InlineData("var result = 3 < 4;", true)]
        [InlineData("var result = 5 < 4;", false)]
        [InlineData("var result = 4 <= 4;", true)]
        [InlineData("var result = 4 <= 5;", true)]
        [InlineData("var result = 5 <= 4;", false)]
        [InlineData("var result = 4 > 3;", true)]
        [InlineData("var result = 4 > 5;", false)]
        [InlineData("var result = 4 >= 4;", true)]
        [InlineData("var result = 5 >= 4;", true)]
        [InlineData("var result = 4 >= 5;", false)]
        [InlineData("var result = 1 | 2;", 3)]
        [InlineData("var result = 1 | 0;", 1)]
        [InlineData("var result = 1 & 2;", 0)]
        [InlineData("var result = 1 & 1;", 1)]
        [InlineData("var result = 1 & 0;", 0)]
        [InlineData("var result = 1 ^ 0;", 1)]
        [InlineData("var result = 0 ^ 1;", 1)]
        [InlineData("var result = 1 ^ 3;", 2)]
        [InlineData("var a = 0; var result = (a = 10) * a;", 100)]
        [InlineData("var a = 11; var result = ++a;", 12)]
        [InlineData("var a = 11; var result = --a;", 10)]
        [InlineData("var a = 11; var result = a++;", 11)]
        [InlineData("var a = 11; var result = a--;", 11)]
        [InlineData("var a = 11; ++a; var result = a;", 12)]
        [InlineData("var a = 11; --a; var result = a;", 10)]
        [InlineData("var a = 11; a++; var result = a;", 12)]
        [InlineData("var a = 11; a--; var result = a;", 10)]
        [InlineData("var a = 11; var result = a += -1;", 10)]
        [InlineData("var a = 11; var result = a -= 1;", 10)]
        [InlineData("var a = 10; var result = a *= 2;", 20)]
        [InlineData("var a = 10; var result = a /= 2;", 5)]
        public void Evaluator_Computes_IntStatements(string text, object expectedValue) => AssertValue(text, expectedValue);

        #endregion Int

        #region Float

        [Theory]
        [InlineData("var result = 1f;", 1d)]
        [InlineData("var result = +1f;", 1d)]
        [InlineData("var result = -1f;", -1d)]
        [InlineData("var result = 14f + 12f;", 26d)]
        [InlineData("var result = 12f - 3f;", 9d)]
        [InlineData("var result = 4f * 2f;", 8d)]
        [InlineData("var result = 9f / 3f;", 3d)]
        [InlineData("var result = (10f);", 10d)]
        [InlineData("var result = 12f == 3f;", false)]
        [InlineData("var result = 3f == 3f;", true)]
        [InlineData("var result = 12f != 3f;", true)]
        [InlineData("var result = 3f != 3f;", false)]
        [InlineData("var result = 3f < 4f;", true)]
        [InlineData("var result = 5f < 4f;", false)]
        [InlineData("var result = 4f <= 4f;", true)]
        [InlineData("var result = 4f <= 5f;", true)]
        [InlineData("var result = 5f <= 4f;", false)]
        [InlineData("var result = 4f > 3f;", true)]
        [InlineData("var result = 4f > 5f;", false)]
        [InlineData("var result = 4f >= 4f;", true)]
        [InlineData("var result = 5f >= 4f;", true)]
        [InlineData("var result = 4f >= 5f;", false)]
        [InlineData("var a = 0f; var result = (a = 10f) * a;", 100d)]
        [InlineData("var a = 11f; var result = ++a;", 12d)]
        [InlineData("var a = 11f; var result = --a;", 10d)]
        [InlineData("var a = 11f; var result = a++;", 11d)]
        [InlineData("var a = 11f; var result = a--;", 11d)]
        [InlineData("var a = 11f; ++a; var result = a;", 12d)]
        [InlineData("var a = 11f; --a; var result = a;", 10d)]
        [InlineData("var a = 11f; a++; var result = a;", 12d)]
        [InlineData("var a = 11f; a--; var result = a;", 10d)]
        [InlineData("var a = 11f; var result = a += -1f;", 10d)]
        [InlineData("var a = 11f; var result = a -= 1f;", 10d)]
        [InlineData("var a = 10f; var result = a *= 2f;", 20d)]
        [InlineData("var a = 10f; var result = a /= 2f;", 5d)]
        public void Evaluator_Computes_FloatStatements(string text, object expectedValue) => AssertValue(text, expectedValue);

        #endregion Float

        #region String

        [Theory]
        [InlineData("var result = \"abc\";", "abc")]
        [InlineData("var result = \"abc\"; result += \"def\";", "abcdef")]
        [InlineData("var result = \"abc\"; result += 'd';", "abcd")]
        [InlineData("var a = \"abc\"; var result = a[1];", 'b')]
        [InlineData("var result = \"abc\"[2];", 'c')]
        public void Evaluator_Computes_StringStatements(string text, object expectedValue) => AssertValue(text, expectedValue);

        #endregion String

        #region Arrays

        [Theory]
        [MemberData(nameof(GetArrayStatementsData))]
        public void Evaluator_Computes_ArrayStatements(string text, object expectedValue) => AssertValue(text, expectedValue);

        public static IEnumerable<object[]> GetArrayStatementsData()
        {
            foreach (var (statement, result) in GetStatements())
            {
                yield return new object[] { statement, result };
            }

            static IEnumerable<(string statement, object result)> GetStatements()
            {
                yield return ("var result = int[2] => 1;", new[] { 1, 1 });
                yield return ("var a = 2; var result = int[a] => 1;", new[] { 1, 1 });
                yield return ("var result = int[1];", new[] { 1 });
                yield return ("var result = int[1, 2];", new[] { 1, 2 });
                yield return ("var result = string[] { \"1\", };", new[] { "1" });
                yield return ("var result = object[] { 1, \"2\" };", new object[] { 1, "2" });

                yield return ("var result = [2] => 1;", new[] { 1, 1 });
                yield return ("var a = 2; var result = [a] => 1;", new[] { 1, 1 });
                yield return ("var result = [1];", new[] { 1 });
                yield return ("var result = [1, 2];", new[] { 1, 2 });
                yield return ("var result = [] { \"1\", };", new[] { "1" });
                yield return ("var result = [] { 1, \"2\" };", new object[] { 1, "2" });

                yield return ("int[] result = [2] => 1;", new[] { 1, 1 });
                yield return ("var a = 2; int[] result = [a] => 1;", new[] { 1, 1 });
                yield return ("int[] result = [1];", new[] { 1 });
                yield return ("int[] result = [1, 2];", new[] { 1, 2 });
                yield return ("string[] result = [] { \"1\", };", new[] { "1" });
                yield return ("object[] result = [] { 1, \"2\" };", new object[] { 1, "2" });

                yield return ("var result = [0] => 1;", Array.Empty<object>());
                yield return ("var a = 0; var result = [a] => 1;", Array.Empty<object>());
                yield return ("var result = [] {};", Array.Empty<object>());

                yield return ("var result = int[] { 1, 2 + 10 };", new[] { 1, 12 });
                yield return ("var a = 5; int[] result = int[] { 1, a };", new[] { 1, 5 });
                yield return ("var a = 5; int[] result = int[a] => a;", new[] { 5, 5, 5, 5, 5 });
                yield return ("var a = 5; int[] result = int[a++] => ++a;", new[] { 7, 8, 9, 10, 11 });

                yield return ("var result = [2] => 3;", new[] { 3, 3 });
                yield return ("var result = [] { [] { 1 }, [] { 2 } };", new[] { new[] { 1 }, new[] { 2 } });
                yield return ("var result = [2] => [3];", new[] { new[] { 3 }, new[] { 3 } });
                yield return ("var result = [1, \"a\"];", new object[] { 1, "a" });

                yield return ("var a = [1, 2, 3]; var result = a[1];", 2);
                yield return ("var a = [[1, 2], [3, 4], [5, 6]]; var result = a[1][0];", 3);
            }
        }

        #endregion Arrays

        #region If

        [Theory]
        [InlineData("var a = 0; if (a == 0) a = 10; var result = a;", 10)]
        [InlineData("var a = 4; if (a == 0) a = 10; var result = a;", 4)]
        [InlineData("var a = 0; if (a == 0) a = 10; else a = 34; var result = a;", 10)]
        [InlineData("var a = 4; if (a == 0) a = 10; else a = 32; var result = a;", 32)]
        public void Evaluator_Computes_IfStatements(string text, int expectedValue) => AssertValue(text, expectedValue);

        #endregion If

        #region Loop

        [Theory]
        [InlineData("var a = 0; while (a < 10) a = a + 1; var result = a;", 10)]
        [InlineData("var result = 0; for (var i = 0; i <= 10; ++i) result = result + i;", 55)]
        public void Evaluator_Computes_LoopStatements(string text, int expectedValue) => AssertValue(text, expectedValue);

        #endregion Loop

        #region typeof

        [Theory]
        [MemberData(nameof(GettypeofStatementsData))]
        public void Evaluator_Computes_typeofStatements(string text, string expectedValue) => AssertValue(text, expectedValue);

        public static IEnumerable<object[]> GettypeofStatementsData()
        {
            foreach (var (statement, result) in GetStatements())
            {
                yield return new object[] { statement, result };
            }

            static IEnumerable<(string statement, object result)> GetStatements()
            {
                yield return ("var result = typeof(string);", Types.String.Name);
                yield return ("var result = typeof(int);", Types.Int.Name);
                yield return ("var result = typeof(bool);", Types.Boolean.Name);
                yield return ("var result = typeof(string[]);", Types.Array.MakeConcreteType(Types.String).Name);
                yield return ("var result = typeof(string[][][]);", Types.Array.MakeConcreteType(Types.Array.MakeConcreteType(Types.Array.MakeConcreteType(Types.String))).Name);
            }
        }

        #endregion typeof

        #region nameof

        [Theory]
        [InlineData("var a = 0; var result = nameof(a);", "a")]
        public void Evaluator_Computes_nameofStatements(string text, string expectedValue) => AssertValue(text, expectedValue);

        #endregion nameof

        #region Comment

        [Theory]
        [InlineData("var a = 3; var result = nameof(a); // gets the name of result\n", "a")]
        [InlineData("var result = 5; //nameof(a); \n", 5)]
        [InlineData("var result = //comment; \n 5;", 5)]
        [InlineData("var result = /* inline comment */ 5;", 5)]
        [InlineData("var result = /*\n inline comment\n */ 5;", 5)]
        [InlineData("var result = /*\n inline comment\n */\n\n\n 5;", 5)]
        public void Evaluator_Computes_StatementsWithComments(string text, object expectedValue) => AssertValue(text, expectedValue);

        #endregion Comment

        #region Method declaration

        [Theory]
        [InlineData("string Ret() => \"a\"; var result = Ret();", "a")]
        [InlineData("def Ret() => \"a\"; var result = Ret();", "a")]
        [InlineData("def Ret(bool b) { if (b) return \"a\"; else return \"b\"; } var result = Ret(true);", "a")]
        public void Evaluator_Computes_MethodDeclarations(string text, string expectedValue) => AssertValue(text, expectedValue);

        #endregion Method declaration

        #region Piped calls

        [Theory]
        [InlineData("string Ret(string s) => s; var result = \"a\" |> Ret();", "a")]
        [InlineData("string Ret(string s) => s; var result = \"a\" |> Ret(_);", "a")]
        [InlineData("string Ret(string s, string b) => s + b; var result = \"a\" |> Ret(\"b\");", "ba")]
        [InlineData("string Ret(string s, string b) => s + b; var result = \"a\" |> Ret(\"b\", _);", "ba")]
        [InlineData("string Ret(string s, string b) => s + b; var result = \"a\" |> Ret(_, \"b\");", "ab")]
        public void Evaluator_Computes_PipeCallStatements(string text, string expectedValue) => AssertValue(text, expectedValue);

        #endregion Piped calls

        #region Namespace

        [Theory]
        [MemberData(nameof(GetNamespaceStatementsData))]
        public void Evaluator_Computes_NamespaceStatements(string text, object expectedValue) => AssertValue(text, expectedValue);

        public static IEnumerable<object[]> GetNamespaceStatementsData()
        {
            foreach (var (statement, result) in GetStatements())
            {
                yield return new object[] { statement, result };
            }

            static IEnumerable<(string statement, object result)> GetStatements()
            {
                yield return (@"
                    var result = A.GetVal();

                    namespace A;
                    def GetVal() => ""a"";",
                   "a");
                yield return (@"
                    // Combination of simple and bodied statement
                    var result = B.GetVal();

                    namespace A;

                    namespace B
                    {
                        def GetVal() => ""a"";
                    }",
                    "a");
                yield return (@"
                    namespace A;

                    def GetVal()
                    {
                        with Sys;

                        Console.Write(""test"");

                        return ""a"";
                    }

                    namespace B {}

                    with A;
                    var result = GetVal();",
                    "a");
                yield return (@"
                    namespace A;

                    def GetVal()
                    {
                        with Sys;

                        Console.Write(""test"");

                        return ""a"";
                    }

                    namespace A.B;

                    def GetValB() => GetVal();

                    namespace B {}
                    var result = A.B.GetValB();",
                    "a");
            }
        }

        #endregion Namespace

        #region WithNamespace

        [Theory]
        [MemberData(nameof(GetWithNamespaceStatementsData))]
        public void Evaluator_Computes_WithNamespaceStatements(string text, object expectedValue) => AssertValue(text, expectedValue);

        public static IEnumerable<object[]> GetWithNamespaceStatementsData()
        {
            foreach (var (statement, result) in GetStatements())
            {
                yield return new object[] { statement, result };
            }

            static IEnumerable<(string statement, object result)> GetStatements()
            {
                yield return (@"
                    namespace A;

                    def GetVal() => ""a"";

                    namespace B {}

                    with A;
                    var result = GetVal();",
                    "a");

                yield return (@"
                    with A
                    {
                        def GetValB() => GetVal();

                        namespace A;

                        def GetVal()
                        {
                            with Sys;

                            Console.Write(""test"");

                            return ""a"";
                        }
                    }

                    namespace B {}
                    var result = GetValB();",
                    "a");

                yield return (@"
                    namespace A
                    {
                        def GetValue()
                        {
                            return GetValueB();

                            def GetValueB() => ""a"";
                        }
                    }

                    var result = A.GetValue();",
                    "a");
            }
        }

        #endregion WithNamespace

        #region WithNamespace

        [Theory]
        [MemberData(nameof(GetWithMethodAliasStatementsData))]
        public void Evaluator_Computes_WithMethodAliasStatements(string text, object expectedValue) => AssertValue(text, expectedValue);

        public static IEnumerable<object[]> GetWithMethodAliasStatementsData()
        {
            foreach (var (statement, result) in GetStatements())
            {
                yield return new object[] { statement, result };
            }

            static IEnumerable<(string statement, object result)> GetStatements()
            {
                yield return ("def getValue() => \"a\"; with call = getValue; var result = call();", "a");
                yield return (@"
                    namespace A;

                    def getValue() => ""a"";

                    namespace B { }

                    with call = A.getValue;
                    var result = call();",
                    "a");

                yield return (@"
                    namespace A;

                    def getValue() => ""a"";

                    namespace B { }

                    with A;
                    with call = getValue;
                    var result = call();",
                    "a");
            }
        }

        #endregion WithNamespace

        #region Implicit upcast

        [Theory]
        [InlineData("object result = false;", false)]
        public void Evaluator_Computes_ImplicitUpcastStatements(string text, bool expectedValue) => AssertValue(text, expectedValue);

        #endregion Implicit upcast

        #endregion Evaluates

        #region Reports

        [Fact]
        public void Evaluator_NoMainMethod_Reports_NoEntryPoint()
        {
            string text = @"
                []def A() { var x = 10; }
            ";

            string diagnostics = @"
                No entry point method (a method with the identifier 'Main') exists in the project.
            ";

            AssertDiagnostics(text, diagnostics, ignoreNoMain: false);
        }

        [Fact]
        public void Evaluator_Call_Reports_Ambiguous()
        {
            var text = @"
                with A;
                with B;

                [GetVal]();

                namespace A;
                def GetVal() => 0;

                namespace B;
                def GetVal() => 0;
            ";

            string diagnostics = @"
                Reference is ambiguous between the following symbols: 'A.GetVal', 'B.GetVal'.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Reassign_Readonly_Reports_Illegal()
        {
            var text = "set a = 0; a [=] 1;";
            string diagnostics = @"
                Variable 'a' is read-only and cannot be modified.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Unary_Mutate_Readonly_Reports_Illegal()
        {
            var text = "set a = 0; [a++];";
            string diagnostics = @"
                Variable 'a' is read-only and cannot be modified.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_AssignVoid_Reports()
        {
            var text = "var [a = Sys.Console.WriteLine(0)];";
            string diagnostics = @"
                Cannot assign void to an implicitly-typed variable.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Assigned_Reports_CannotConvert()
        {
            string text = @"
                {
                    var x = 10;
                    x = [true];
                }
            ";

            string diagnostics = @"
                Cannot convert type 'bool' to 'int'.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Declare_Reports_CannotFollowCondition()
        {
            string text = @"
                {
                    if (true)
                        [var a = 0;]
                    if (true)
                    { }
                    else [var b = 0;]

                    while (false)
                        [var c = 0;]
                    for ( ; ; )
                        [var d = 0;]
                }
            ";

            string diagnostics = @"
                Variable declarations cannot be placed right after a condition.
                Variable declarations cannot be placed right after a condition.
                Variable declarations cannot be placed right after a condition.
                Variable declarations cannot be placed right after a condition.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Assigned_Reports_CannotImplicitlyConvert()
        {
            string text = @"
                {
                    var x = 10f;
                    x = [5];
                }
            ";

            string diagnostics = @"
                Cannot implicitly convert type 'int' to 'float'. An explicit convertion exists (are you missing a cast?).
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Piped_Reports_OnlySupportedForMethods()
        {
            string text = @"
                {
                    var a = \[1, 2, 3\];
                    4 [|>] a\[\];
                }
            ";

            string diagnostics = @"
                Expressions can only be piped into methods.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_MethodDeclared_Reports_DuplicateParameterName()
        {
            string text = @"
                def MyMethod(int a, int [a]) { }
            ";

            string diagnostics = @"
                Duplicate parameter name 'a'.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_CharLiteral_Reports_Empty()
        {
            string text = @"
                char x = [''];
            ";

            string diagnostics = @"
                Empty character literal.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Namespace_Declaration_Reports_Illegal_In_Method()
        {
            var text = @"
                def B()
                {
                    [namespace] Y;
                }
            ";

            string diagnostics = @"
                Namespaces may not be declared inside of methods or types.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Simple_Namespace_Declaration_Reports_Illegal_Nesting()
        {
            var text = @"
                namespace A
                {
                    [namespace] X;
                }
            ";

            string diagnostics = @"
                Simple namespace declarations may only exist as top-level statement (not nested in other namespaces).
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Statement_Reports_Illegal_Statement_Placement()
        {
            var text = @"
                namespace A;

                [var a = 0;]
                [var b = a;]
                [a = b;]
            ";

            string diagnostics = @"
                Unexpected statement. Namespaces and type declarations cannot directly contain statements.
                Unexpected statement. Namespaces and type declarations cannot directly contain statements.
                Unexpected statement. Namespaces and type declarations cannot directly contain statements.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Primitive_Reports_InvalidNumberValue()
        {
            string text = @"
                int x = [1000000000000000000];
            ";

            string diagnostics = @"
                The number '1000000000000000000' is not a valid value for type 'int'.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_LableDeclared_Reports_Duplicate()
        {
            string text = @"
                label:
                [label]:
            ";

            string diagnostics = @"
                A label with the name 'label' is already declared.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_MethodDeclared_Reports_Duplicate()
        {
            string text = @"
                def MyMethod() { }
                def [MyMethod]() { }
            ";

            string diagnostics = @"
                A method with the name 'MyMethod' is already declared in this scope.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_MethodDeclaration_Reports_NotAllPathsReturn()
        {
            string text = @"
                def [MyMethodA](bool a) { if (a) return 10; }
                def [MyMethodB]() { if (true) 2; else return 10; }
                def [MyMethodC]() { while(false) { return 44; } }
                def MyMethodD() { while(true) { } return 44; }
            ";

            string diagnostics = @"
                Not all paths return a value.
                Not all paths return a value.
                Not all paths return a value.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_MethodDeclaration_Reports_IllegalEmptyReturn()
        {
            string text = @"
                int MyMethod() { [return;] }
            ";

            string diagnostics = @"
                Expected to return expression in non-void returning method.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Project_Reports_MultipleMains()
        {
            string text = @"
                def [Main]() { }

                namespace Namespace;

                def [Main]() { }
            ";

            string diagnostics = @"
                A project may only contain a single main method.
                A project may only contain a single main method.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Cast_Reports_NoExplicitConversion()
        {
            string text = @"
                var a = [(int)""myVal""];
            ";

            string diagnostics = @"
                No explicit convertion exists for type 'string' to 'int'.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_TopLevelStatement_Reports_IncompatibleWithExplicitMain()
        {
            string text = @"
                [Sys].Console.WriteLine(0);

                def Main() { }
            ";

            string diagnostics = @"
                Top-level statements may not be defined in a project that already contain an explicit main method declaration.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Expression_Reports_NoIndexer()
        {
            string text = @"
                var a = [4\[4\]];
            ";

            string diagnostics = @"
                Type 'int' does not expose an indexer.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_BinaryOperator_Reports_Undefined()
        {
            string text = @"
                var a = ""a"" [*] 3;
                var b = 4;
                b [+=] 4f;
            ";

            string diagnostics = @"
                Binary operator '*' is not defined for types 'string' and 'int'.
                Binary operator '+' is not defined for types 'int' and 'float'.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Assigned_Reports_Undefined()
        {
            string text = @"
                [x] = 10;
            ";

            string diagnostics = @"
                Symbol 'x' does not exist in this context.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Call_Reports_Undefined()
        {
            string text = @"
                [method]();
            ";

            string diagnostics = @"
                Symbol 'method' does not exist in this context.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Goto_Reports_Undefined()
        {
            string text = @"
                goto [lbl];
            ";

            string diagnostics = @"
                Symbol 'lbl' does not exist in this context.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_UnaryOperator_Reports_Undefined()
        {
            string text = @"
                [+]true;
                string a = [-]""a"";
            ";

            string diagnostics = @"
                Unary operator '+' is not defined for type 'bool'.
                Unary operator '-' is not defined for type 'string'.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_BreakAndContinue_Reports_IllegalPlacement()
        {
            string text = @"
                def Method()
                {
                    [continue;]
                    [break;]
                }
            ";

            string diagnostics = @"
                No enclosing loop out of which to break or continue.
                No enclosing loop out of which to break or continue.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Call_Reports_UnexpectedPipedArgument()
        {
            string text = @"
                def MyMethod(int a, int b) { }

                MyMethod([_], 1);
                10 |> MyMethod(_, [_]);
                MyMethod([_], [_]);
            ";

            string diagnostics = @"
                Unexpected piped argument. Method call was either not provided a piped argument, or multiple piped arguments were attempted used.
                Unexpected piped argument. Method call was either not provided a piped argument, or multiple piped arguments were attempted used.
                Unexpected piped argument. Method call was either not provided a piped argument, or multiple piped arguments were attempted used.
                Unexpected piped argument. Method call was either not provided a piped argument, or multiple piped arguments were attempted used.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_CharLiteral_Reports_Unterminated()
        {
            string text = @"
                char x = ['];
                char y = ['] ;
                char z = [']
;
            ";

            string diagnostics = @"
                Unterminated character literal.
                Unterminated character literal.
                Unterminated character literal.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_StringLiteral_Reports_Unterminated()
        {
            string text = @"
                string x = [""]a string
                ;
            ";

            string diagnostics = @"
                Unterminated string literal.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_VariableDeclaration_Reports_Redeclaration()
        {
            string text = @"
                {
                    var x = 10;
                    var y = 100;
                    {
                        var x = 10;
                    }
                    var [x] = 5;
                }
            ";

            string diagnostics = @"
                A variable with the name 'x' is already declared.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Call_Reports_WrongArgumentCount()
        {
            string text = @"
                [Sys.Console.WriteLine(0, 1, 2)];
            ";

            string diagnostics = @"
                Method or indexer 'WriteLine' requires 1 arguments, but recieved 3.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Indexer_Reports_WrongArgumentCount()
        {
            string text = @"
                var a = \[1, 2, 3\];
                [a\[1, 2, 3\]];
            ";

            string diagnostics = @"
                Method or indexer '[]' requires 1 arguments, but recieved 3.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Implicit_Recursive_Method_Return_Type_Reports_Unable_To_Infer()
        {
            var text = @"
                def Main() { }
                def [A](bool b) => B(b);
                def [B](bool b)
                {
                    if (b)
                        return A(b);
                    else
                        return A(!b);
                }
            ";

            string diagnostics = @"
                Implicit return type of method 'A' cannot be infered.
                Implicit return type of method 'B' cannot be infered.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Call_Reports_CannotConvertParameter()
        {
            string text = @"
                def MyMethod(int a) { }

                [MyMethod(2f)];
            ";

            string diagnostics = @"
                Parameter 'a' in method 'MyMethod' requires value of type 'int', but recieved value of type 'float'.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        #endregion Reports

        private static void AssertValue(string text, object expectedValue)
        {
            var syntaxTree = SyntaxTree.FromString(text);
            var compilation = new Compilation(syntaxTree, new() { Optimize = false });
            var variables = new Dictionary<VariableSymbol, object>();
            var result = compilation.Evaluate(variables);

            Assert.Empty(result.Diagnostics);

            var resultVariable = variables.Keys.FirstOrDefault(v => v.Name == "result");
            Assert.NotNull(resultVariable);

            Assert.Equal(expectedValue, variables[resultVariable!]);
        }

        private static void AssertDiagnostics(string text, string diagnosticText, bool ignoreNoMain = true)
        {
            var annotatedText = AnnotatedText.Parse(text);
            var syntaxTree = SyntaxTree.FromString(annotatedText.Text);
            var compilation = new Compilation(syntaxTree);
            var result = compilation.Evaluate(new Dictionary<VariableSymbol, object>());

            string[] expectedDiagnostics = AnnotatedText.UnintentLines(diagnosticText);

            if (annotatedText.Spans.Length != expectedDiagnostics.Length)
                throw new Exception($"ERROR: Must mark as many spans as there are expected diagnostics.");

            var diagnostics = result.Diagnostics
                .Where(d => !ignoreNoMain || d.Message != $"No entry point method (a method with the identifier 'Main') exists in the project.")
                .OrderBy(d => d.Span)
                .ToArray();

            Assert.Equal(expectedDiagnostics.Length, diagnostics.Length);

            for (int i = 0; i < expectedDiagnostics.Length; i++)
            {
                string expectedMessage = expectedDiagnostics[i];
                string actualMessage = diagnostics[i].Message;

                var expectedSpan = annotatedText.Spans[i];
                var actualSpan = diagnostics[i].Span;

                Assert.Equal(expectedMessage, actualMessage);
                Assert.Equal(expectedSpan, actualSpan);
            }
        }
    }
}