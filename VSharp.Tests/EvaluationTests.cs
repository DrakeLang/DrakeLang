//------------------------------------------------------------------------------
// VSharp - Viv's C#-esque sandbox.
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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using VSharp.Symbols;
using VSharp.Syntax;
using VSharp.Text;
using Xunit;
using static VSharp.Symbols.SystemSymbols;

namespace VSharp.Tests
{
    public class EvaluationTests
    {
        [Theory]
        [MemberData(nameof(GetStatementsData))]
        public void Evaluator_Computes_CorrectValues(string text, object expectedValue)
        {
            AssertValue(text, expectedValue);
        }

        #region Reports

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
        public void Evaluator_Assigned_Reports_Undefined()
        {
            string text = @"
                [x] = 10;
            ";

            string diagnostics = @"
                Symbol 'x' does not exist.
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
        public void Evaluator_Unary_Reports_UndefinedOperator()
        {
            string text = @"[+]true;";

            string diagnostics = @"
                Unary operator '+' is not defined for type 'bool'.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Evaluator_Binary_Reports_UndefinedOperator()
        {
            string text = @"10 [*] true;";

            string diagnostics = @"
                Binary operator '*' is not defined for types 'int' and 'bool'.
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
        public void Evaluator_Statement_Reports_Illegal_Statement_Placement()
        {
            var text = @"
                namespace A;

                [var a = 0;]
            ";

            string diagnostics = @"
                Unexpected statement. Namespaces and type declarations cannot directly contain statements.
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

        #endregion Reports

        public static IEnumerable<object[]> GetStatementsData()
        {
            foreach ((string statement, object result) in GetStatements())
                yield return new object[] { statement + "var resultX = result; result = resultX;", result };

            static IEnumerable<(string statement, object result)> GetStatements()
            {
                // Bool statements
                yield return ("var result = true;", true);
                yield return ("var result = false;", false);
                yield return ("var result = !true;", false);
                yield return ("var result = !false;", true);
                yield return ("var result = true || true;", true);
                yield return ("var result = true || false;", true);
                yield return ("var result = false || true;", true);
                yield return ("var result = false || false;", false);
                yield return ("var result = true && true;", true);
                yield return ("var result = true && false;", false);
                yield return ("var result = false && true;", false);
                yield return ("var result = false && false;", false);

                // Bool comparisons
                yield return ("var result = false == false;", true);
                yield return ("var result = true == false;", false);
                yield return ("var result = false != false;", false);
                yield return ("var result = true != false;", true);

                // Bool bitwise operations
                yield return ("var result = false | false;", false);
                yield return ("var result = false | true;", true);
                yield return ("var result = true | false;", true);
                yield return ("var result = true | true;", true);
                yield return ("var result = false & false;", false);
                yield return ("var result = false & true;", false);
                yield return ("var result = true & false;", false);
                yield return ("var result = true & true;", true);
                yield return ("var result = false ^ false;", false);
                yield return ("var result = false ^ true;", true);
                yield return ("var result = true ^ false;", true);
                yield return ("var result = true ^ true;", false);

                // Int statements
                yield return ("var result = 1;", 1);
                yield return ("var result = +1;", 1);
                yield return ("var result = -1;", -1);
                yield return ("var result = ~1;", ~1);
                yield return ("var result = 14 + 12;", 26);
                yield return ("var result = 12 - 3;", 9);
                yield return ("var result = 4 * 2;", 8);
                yield return ("var result = 9 / 3;", 3);
                yield return ("var result = (10);", 10);

                // Int comparisons
                yield return ("var result = 12 == 3;", false);
                yield return ("var result = 3 == 3;", true);
                yield return ("var result = 12 != 3;", true);
                yield return ("var result = 3 != 3;", false);
                yield return ("var result = 3 < 4;", true);
                yield return ("var result = 5 < 4;", false);
                yield return ("var result = 4 <= 4;", true);
                yield return ("var result = 4 <= 5;", true);
                yield return ("var result = 5 <= 4;", false);
                yield return ("var result = 4 > 3;", true);
                yield return ("var result = 4 > 5;", false);
                yield return ("var result = 4 >= 4;", true);
                yield return ("var result = 5 >= 4;", true);
                yield return ("var result = 4 >= 5;", false);

                // Int bitwise operations
                yield return ("var result = 1 | 2;", 3);
                yield return ("var result = 1 | 0;", 1);
                yield return ("var result = 1 & 2;", 0);
                yield return ("var result = 1 & 1;", 1);
                yield return ("var result = 1 & 0;", 0);
                yield return ("var result = 1 ^ 0;", 1);
                yield return ("var result = 0 ^ 1;", 1);
                yield return ("var result = 1 ^ 3;", 2);

                // Int variable & assignment
                yield return ("var a = 0; var result = (a = 10) * a;", 100);
                yield return ("var a = 11; var result = ++a;", 12);
                yield return ("var a = 11; var result = --a;", 10);
                yield return ("var a = 11; var result = a++;", 11);
                yield return ("var a = 11; var result = a--;", 11);
                yield return ("var a = 11; ++a; var result = a;", 12);
                yield return ("var a = 11; --a; var result = a;", 10);
                yield return ("var a = 11; a++; var result = a;", 12);
                yield return ("var a = 11; a--; var result = a;", 10);
                yield return ("var a = 11; var result = a += -1;", 10);
                yield return ("var a = 11; var result = a -= 1;", 10);
                yield return ("var a = 10; var result = a *= 2;", 20);
                yield return ("var a = 10; var result = a /= 2;", 5);

                // Float statements
                yield return ("var result = 1f;", 1d);
                yield return ("var result = +1f;", 1d);
                yield return ("var result = -1f;", -1d);
                yield return ("var result = 14f + 12f;", 26d);
                yield return ("var result = 12f - 3f;", 9d);
                yield return ("var result = 4f * 2f;", 8d);
                yield return ("var result = 9f / 3f;", 3d);
                yield return ("var result = (10f);", 10d);

                // Float comparisons
                yield return ("var result = 12f == 3f;", false);
                yield return ("var result = 3f == 3f;", true);
                yield return ("var result = 12f != 3f;", true);
                yield return ("var result = 3f != 3f;", false);
                yield return ("var result = 3f < 4f;", true);
                yield return ("var result = 5f < 4f;", false);
                yield return ("var result = 4f <= 4f;", true);
                yield return ("var result = 4f <= 5f;", true);
                yield return ("var result = 5f <= 4f;", false);
                yield return ("var result = 4f > 3f;", true);
                yield return ("var result = 4f > 5f;", false);
                yield return ("var result = 4f >= 4f;", true);
                yield return ("var result = 5f >= 4f;", true);
                yield return ("var result = 4f >= 5f;", false);

                // Float variable & assignment
                yield return ("var a = 0f; var result = (a = 10f) * a;", 100d);
                yield return ("var a = 11f; var result = ++a;", 12d);
                yield return ("var a = 11f; var result = --a;", 10d);
                yield return ("var a = 11f; var result = a++;", 11d);
                yield return ("var a = 11f; var result = a--;", 11d);
                yield return ("var a = 11f; ++a; var result = a;", 12d);
                yield return ("var a = 11f; --a; var result = a;", 10d);
                yield return ("var a = 11f; a++; var result = a;", 12d);
                yield return ("var a = 11f; a--; var result = a;", 10d);
                yield return ("var a = 11f; var result = a += -1f;", 10d);
                yield return ("var a = 11f; var result = a -= 1f;", 10d);
                yield return ("var a = 10f; var result = a *= 2f;", 20d);
                yield return ("var a = 10f; var result = a /= 2f;", 5d);

                // If-else-statement
                yield return ("var a = 0; if (a == 0) a = 10; var result = a;", 10);
                yield return ("var a = 4; if (a == 0) a = 10; var result = a;", 4);
                yield return ("var a = 0; if (a == 0) a = 10; else a = 34; var result = a;", 10);
                yield return ("var a = 4; if (a == 0) a = 10; else a = 32; var result = a;", 32);

                // While, for statement
                yield return ("var a = 0; while (a < 10) a = a + 1; var result = a;", 10);
                yield return ("var result = 0; for (var i = 0; i <= 10; ++i) result = result + i;", 55);

                // Typeof
                yield return ("var result = typeof(string);", Types.String.Name);
                yield return ("var result = typeof(int);", Types.Int.Name);
                yield return ("var result = typeof(bool);", Types.Boolean.Name);

                // Nameof
                yield return ("var a = 0; var result = nameof(a);", "a");

                // Line comment
                yield return ("var a = 3; var result = nameof(a); // gets the name of result\n", "a");
                yield return ("var result = 5; //nameof(a); \n", 5);

                // Method declarations
                yield return ("string Ret() => \"a\"; var result = Ret();", "a");
                yield return ("def Ret() => \"a\"; var result = Ret();", "a");
                yield return ("def Ret(bool b) { if (b) return \"a\"; else return \"b\"; } var result = Ret(true);", "a");

                // Piping
                yield return ("string Ret(string s) => s; var result = \"a\" |> Ret();", "a");
                yield return ("string Ret(string s) => s; var result = \"a\" |> Ret(_);", "a");
                yield return ("string Ret(string s, string b) => s + b; var result = \"a\" |> Ret(\"b\");", "ba");
                yield return ("string Ret(string s, string b) => s + b; var result = \"a\" |> Ret(\"b\", _);", "ba");
                yield return ("string Ret(string s, string b) => s + b; var result = \"a\" |> Ret(_, \"b\");", "ab");

                // Namespaces
                yield return (@"
                    var result = B.GetVal();

                    namespace A;

                    namespace B
                    {
                        def GetVal() => ""a"";
                    }", "a");
            }
        }

        private static void AssertValue(string text, object expectedValue)
        {
            var syntaxTree = SyntaxTree.Parse(text);
            var compilation = new Compilation(syntaxTree);
            var variables = new Dictionary<VariableSymbol, object>();
            var result = compilation.Evaluate(variables);

            Assert.Empty(result.Diagnostics);

            var resultVariable = variables.Keys.FirstOrDefault(v => v.Name == "result");
            Assert.NotNull(resultVariable);

            Assert.Equal(expectedValue, variables[resultVariable!]);
        }

        private static void AssertDiagnostics(string text, string diagnosticText)
        {
            AnnotatedText annotatedText = AnnotatedText.Parse(text);
            SyntaxTree syntaxTree = SyntaxTree.Parse(annotatedText.Text);
            Compilation compilation = new Compilation(syntaxTree);
            EvaluationResult result = compilation.Evaluate(new Dictionary<VariableSymbol, object>());

            string[] expectedDiagnostics = AnnotatedText.UnintentLines(diagnosticText);

            if (annotatedText.Spans.Length != expectedDiagnostics.Length)
                throw new Exception($"ERROR: Must mark as many spans as there are expected diagnostics.");

            var diagnostics = result.Diagnostics.OrderBy(d => d.Span).ToImmutableArray();
            Assert.Equal(expectedDiagnostics.Length, diagnostics.Length);

            for (int i = 0; i < expectedDiagnostics.Length; i++)
            {
                string expectedMessage = expectedDiagnostics[i];
                string actualMessage = diagnostics[i].Message;

                TextSpan expectedSpan = annotatedText.Spans[i];
                TextSpan actualSpan = diagnostics[i].Span;

                Assert.Equal(expectedMessage, actualMessage);
                Assert.Equal(expectedSpan, actualSpan);
            }
        }
    }
}