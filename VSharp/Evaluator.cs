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
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using VSharp.Binding;
using VSharp.Symbols;
using VSharp.Utils;
using static VSharp.Symbols.SystemSymbols;

namespace VSharp
{
    internal sealed class Evaluator : IEvaluator
    {
        public Evaluator()
        {
        }

        public void Evaluate(ImmutableArray<BoundMethodDeclaration> methods, Dictionary<VariableSymbol, object> variables)
        {
            var entryMethod = methods.FirstOrDefault(m => m.Method.Name == Binder.GeneratedMainMethodName)
                ?? methods.FirstOrDefault(m => m.Method.Name == Binder.MainMethodName);
            if (entryMethod is null)
                throw new Exception($"No method with the name '{Binder.MainMethodName}' was found.");

            var evaluator = new InternalEvaluator(entryMethod.Declaration, variables, methods.ToDictionary(md => md.Method));

            evaluator.Evaluate();
        }

        private sealed class InternalEvaluator
        {
            private readonly ImmutableArray<BoundStatement> _statements;
            private readonly Dictionary<VariableSymbol, object> _variables;
            private readonly Dictionary<LabelSymbol, int> _labelToIndex = new Dictionary<LabelSymbol, int>();
            private readonly Dictionary<MethodSymbol, BoundMethodDeclaration> _methods;

            public InternalEvaluator(ImmutableArray<BoundStatement> statements,
                                     Dictionary<VariableSymbol, object> variables,
                                     Dictionary<MethodSymbol, BoundMethodDeclaration> methods)
            {
                _statements = statements;
                _variables = variables;
                _methods = methods;

                // Create label-index mapping for goto statements.
                for (int i = 0; i < statements.Length; i++)
                {
                    if (statements[i] is BoundLabelStatement l)
                    {
                        _labelToIndex.Add(l.Label, i + 1);
                    }
                }
            }

            public object Evaluate()
            {
                // Evaluate program.
                int index = 0;
                while (index < _statements.Length)
                {
                    var s = _statements[index];
                    switch (s.Kind)
                    {
                        case BoundNodeKind.VariableDeclarationStatement:
                            EvaluateVariableDeclarationStatement((BoundVariableDeclarationStatement)s);
                            index++;
                            break;

                        case BoundNodeKind.ExpressionStatement:
                            EvaluateExpressionStatement((BoundExpressionStatement)s);
                            index++;
                            break;

                        case BoundNodeKind.GotoStatement:
                            var gotoStatement = (BoundGotoStatement)s;
                            index = _labelToIndex[gotoStatement.Label];
                            break;

                        case BoundNodeKind.ConditionalGotoStatement:
                            var conGotoStatement = (BoundConditionalGotoStatement)s;
                            if (EvaluateConditionalGotoStatement(conGotoStatement))
                                index = _labelToIndex[conGotoStatement.Label];
                            else
                                index++;
                            break;

                        case BoundNodeKind.ReturnStatement:
                            var returnStatement = (BoundReturnStatement)s;
                            if (returnStatement.Expression is null)
                                return 0;
                            else
                                return EvaluateExpression(returnStatement.Expression);

                        case BoundNodeKind.LabelStatement:
                        case BoundNodeKind.NoOpStatement:
                            index++;
                            break;

                        default:
                            throw new Exception($"Unexpected node '{s.Kind}'.");
                    }
                }

                return 0;
            }

            #region EvaluateStatement

            public void EvaluateVariableDeclarationStatement(BoundVariableDeclarationStatement node)
            {
                object value = EvaluateExpression(node.Initializer);
                _variables[node.Variable] = value;
            }

            public void EvaluateExpressionStatement(BoundExpressionStatement node)
            {
                EvaluateExpression(node.Expression);
            }

            public bool EvaluateConditionalGotoStatement(BoundConditionalGotoStatement s)
            {
                bool condition = (bool)EvaluateExpression(s.Condition);

                return condition ^ s.JumpIfFalse;
            }

            #endregion EvaluateStatement

            #region EvaluateExpression

            public object EvaluateExpression(BoundExpression node) => node.Kind switch
            {
                BoundNodeKind.LiteralExpression => EvaluateLiteralExpression((BoundLiteralExpression)node),
                BoundNodeKind.VariableExpression => EvaluateVariableExpression((BoundVariableExpression)node),
                BoundNodeKind.AssignmentExpression => EvaluateAssignmentExpression((BoundAssignmentExpression)node),
                BoundNodeKind.UnaryExpression => EvaluateUnaryExpression((BoundUnaryExpression)node),
                BoundNodeKind.BinaryExpression => EvaluateBinaryExpression((BoundBinaryExpression)node),
                BoundNodeKind.CallExpression => EvaluateCallExpression((BoundCallExpression)node),
                BoundNodeKind.ExplicitCastExpression => EvaluateExplicitCastExpression((BoundExplicitCastExpression)node),
                BoundNodeKind.ArrayInitializationExpression => EvaluateArrayInitializationExpression((BoundArrayInitializationExpression)node),

                _ => throw new Exception($"Unexpected node '{node.Kind}'."),
            };

            public static object EvaluateLiteralExpression(BoundLiteralExpression node)
            {
                return node.Value;
            }

            public object EvaluateVariableExpression(BoundVariableExpression node)
            {
                return _variables[node.Variable];
            }

            public object EvaluateAssignmentExpression(BoundAssignmentExpression node)
            {
                object value = EvaluateExpression(node.Expression);
                _variables[node.Variable] = value;

                return value;
            }

            public object EvaluateUnaryExpression(BoundUnaryExpression node)
            {
                // Pre- and post increment/decrement.
                if (node.Op.Kind == BoundUnaryOperatorKind.PreDecrement || node.Op.Kind == BoundUnaryOperatorKind.PreIncrement)
                {
                    var variableExpression = (BoundVariableExpression)node.Operand;

                    if (node.Type == Types.Int)
                        _variables[variableExpression.Variable] = (int)_variables[variableExpression.Variable] + (node.Op.Kind == BoundUnaryOperatorKind.PreIncrement ? 1 : -1);
                    else
                        _variables[variableExpression.Variable] = (double)_variables[variableExpression.Variable] + (node.Op.Kind == BoundUnaryOperatorKind.PreIncrement ? 1 : -1);

                    return _variables[variableExpression.Variable];
                }
                else if (node.Op.Kind == BoundUnaryOperatorKind.PostDecrement || node.Op.Kind == BoundUnaryOperatorKind.PostIncrement)
                {
                    var variableExpression = (BoundVariableExpression)node.Operand;

                    object value = _variables[variableExpression.Variable];
                    if (node.Type == Types.Int)
                    {
                        _variables[variableExpression.Variable] = (int)_variables[variableExpression.Variable] + (node.Op.Kind == BoundUnaryOperatorKind.PostIncrement ? 1 : -1);
                    }
                    else
                    {
                        _variables[variableExpression.Variable] = (double)_variables[variableExpression.Variable] + (node.Op.Kind == BoundUnaryOperatorKind.PostIncrement ? 1 : -1);
                    }

                    return value;
                }

                // Other unary operations.
                object operand = EvaluateExpression(node.Operand);
                return LiteralEvaluator.EvaluateUnaryExpression(node.Op, operand);
            }

            public object EvaluateBinaryExpression(BoundBinaryExpression node)
            {
                object left = EvaluateExpression(node.Left);
                object right = EvaluateExpression(node.Right);

                return LiteralEvaluator.EvaluateBinaryExpression(node.Op, left, right);
            }

            public object EvaluateCallExpression(BoundCallExpression node)
            {
                if (node.Operand is not null)
                    return EvaluateInstanceCallExpression(node);
                else
                    return EvaluateStaticCallExpression(node);
            }

            private object EvaluateInstanceCallExpression(BoundCallExpression node)
            {
                Debug.Assert(node.Operand is not null);

                var operand = EvaluateExpression(node.Operand);
                var args = node.Arguments.Select(arg => EvaluateExpression(arg)).ToArray();

                if (LiteralEvaluator.TryEvaluateCallExpression(operand, args, node.Method, out var result))
                    return result;

                if (node.Operand.Type.IsGenericType)
                {
                    if (node.Method == Types.Array.MakeConcreteType(node.Operand.Type.GenericTypeArguments[0]).FindGetIndexers().Single())
                    {
                        var array = (object[])operand;
                        var index = (int)args[0];
                        return array[index];
                    }
                }

                throw new Exception($"Indexer for type '{node.Operand.Type}' not handled.");
            }

            private object EvaluateStaticCallExpression(BoundCallExpression node)
            {
                var args = node.Arguments.Select(arg => EvaluateExpression(arg)).ToArray();

                if (node.Method == Methods.Sys_Console_ReadLine)
                {
                    return Console.ReadLine() ?? string.Empty;
                }
                else if (node.Method == Methods.Sys_Console_Write)
                {
                    var message = toInvariantString(args[0]);
                    Console.Write(message);
                }
                else if (node.Method == Methods.Sys_Console_WriteLine)
                {
                    var message = toInvariantString(args[0]);
                    Console.WriteLine(message);
                }
                else if (node.Method == Methods.Sys_IO_File_ReadAllText)
                {
                    var path = (string)args[0];
                    if (!System.IO.File.Exists(path))
                        return "";

                    return System.IO.File.ReadAllText(path);
                }
                else if (node.Method == Methods.Sys_String_Length)
                {
                    var str = (string)args[0];
                    return str.Length;
                }
                else if (node.Method == Methods.Sys_String_CharAt)
                {
                    var pos = (int)args[0];
                    var str = (string)args[1];

                    if (pos < 0 || pos >= str.Length)
                        return default(char);

                    return str[pos];
                }
                else if (_methods.TryGetValue(node.Method, out var method))
                {
                    var stackFrame = new Dictionary<VariableSymbol, object>();
                    for (int i = 0; i < node.Method.Parameters.Length; i++)
                    {
                        stackFrame[node.Method.Parameters[i]] = args[i];
                    }

                    return new InternalEvaluator(method.Declaration, stackFrame, _methods).Evaluate();
                }

                return 0; // cannot return null due to nullable reference types being enabled.

                static string? toInvariantString(object o)
                {
                    return o is IConvertible convertible ?
                        convertible.ToString(CultureInfo.InvariantCulture) :
                        o.ToString();
                }
            }

            public object EvaluateExplicitCastExpression(BoundExplicitCastExpression node)
            {
                var value = EvaluateExpression(node.Expression);
                return LiteralEvaluator.EvaluateExplicitCastExpression(node.Type, value);
            }

            public object EvaluateArrayInitializationExpression(BoundArrayInitializationExpression node)
            {
                var size = (int)EvaluateExpression(node.SizeExpression);
                var value = new object[size];

                for (int i = 0; i < value.Length; i++)
                {
                    value[i] = node.Initializer.Length == 1 ? EvaluateExpression(node.Initializer[0]) : EvaluateExpression(node.Initializer[i]);
                }

                return value;
            }

            #endregion EvaluateExpression
        }
    }
}