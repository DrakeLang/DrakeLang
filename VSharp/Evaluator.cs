//------------------------------------------------------------------------------
// VSharp - Viv's C#-esque sandbox.
// Copyright (C) 2019  Niklas Gransjøen
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
using System.Linq;
using VSharp.Binding;
using VSharp.Symbols;

namespace VSharp
{
    internal sealed class Evaluator : IEvaluator
    {
        private readonly Dictionary<MethodSymbol, BoundMethodDeclarationStatement>? _methods;

        public Evaluator()
        {
        }

        private Evaluator(Dictionary<MethodSymbol, BoundMethodDeclarationStatement> methods)
        {
            _methods = methods;
        }

        public void Evaluate(BoundBlockStatement root, Dictionary<VariableSymbol, object> variables)
        {
            // Create label-index mapping for goto statements.
            var labelToIndex = new Dictionary<LabelSymbol, int>();
            for (int i = 0; i < root.Statements.Length; i++)
            {
                if (root.Statements[i] is BoundLabelStatement l)
                {
                    labelToIndex.Add(l.Label, i + 1);
                }
            }

            var evaluator = new InternalEvaluator(variables, _methods ?? root.MethodDeclarations.ToDictionary(md => md.Method));

            // Evaluate program.
            int index = 0;
            while (index < root.Statements.Length)
            {
                var s = root.Statements[index];
                switch (s.Kind)
                {
                    case BoundNodeKind.VariableDeclarationStatement:
                        evaluator.EvaluateVariableDeclarationStatement((BoundVariableDeclarationStatement)s);
                        index++;
                        break;

                    case BoundNodeKind.ExpressionStatement:
                        evaluator.EvaluateExpressionStatement((BoundExpressionStatement)s);
                        index++;
                        break;

                    case BoundNodeKind.GotoStatement:
                        var gotoStatement = (BoundGotoStatement)s;
                        index = labelToIndex[gotoStatement.Label];
                        break;

                    case BoundNodeKind.ConditionalGotoStatement:
                        var conGotoStatement = (BoundConditionalGotoStatement)s;
                        if (evaluator.EvaluateConditionalGotoStatement(conGotoStatement))
                            index = labelToIndex[conGotoStatement.Label];
                        else
                            index++;
                        break;

                    case BoundNodeKind.LabelStatement:
                    case BoundNodeKind.NoOpStatement:
                        index++;
                        break;

                    default:
                        throw new Exception($"Unexpected node '{s.Kind}'.");
                }
            }
        }

        private sealed class InternalEvaluator
        {
            private readonly Dictionary<VariableSymbol, object> _variables;
            private readonly Dictionary<MethodSymbol, BoundMethodDeclarationStatement> _methods;

            public InternalEvaluator(Dictionary<VariableSymbol, object> variables, Dictionary<MethodSymbol, BoundMethodDeclarationStatement> methods)
            {
                _variables = variables;
                _methods = methods;
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

                    if (node.Type == TypeSymbol.Int)
                        _variables[variableExpression.Variable] = (int)_variables[variableExpression.Variable] + (node.Op.Kind == BoundUnaryOperatorKind.PreIncrement ? 1 : -1);
                    else
                        _variables[variableExpression.Variable] = (double)_variables[variableExpression.Variable] + (node.Op.Kind == BoundUnaryOperatorKind.PreIncrement ? 1 : -1);

                    return _variables[variableExpression.Variable];
                }
                else if (node.Op.Kind == BoundUnaryOperatorKind.PostDecrement || node.Op.Kind == BoundUnaryOperatorKind.PostIncrement)
                {
                    var variableExpression = (BoundVariableExpression)node.Operand;

                    object value = _variables[variableExpression.Variable];
                    if (node.Type == TypeSymbol.Int)
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
                if (node.Method == BuiltinMethods.Input)
                {
                    return Console.ReadLine();
                }
                else if (node.Method == BuiltinMethods.Print)
                {
                    string message = (string)EvaluateExpression(node.Arguments[0]);
                    Console.WriteLine(message);
                }
                else if (_methods.TryGetValue(node.Method, out var method))
                {
                    var stackFrame = new Dictionary<VariableSymbol, object>();
                    for (int i = 0; i < node.Method.Parameters.Length; i++)
                    {
                        stackFrame[node.Method.Parameters[i]] = EvaluateExpression(node.Arguments[i]);
                    }

                    new Evaluator(_methods).Evaluate(method.Declaration, stackFrame);
                }
                else throw new Exception($"Unexpected method '{node.Method}'.");

                return 0; // cannot return null due to nullable reference types being enabled.
            }

            public object EvaluateExplicitCastExpression(BoundExplicitCastExpression node)
            {
                var value = EvaluateExpression(node.Expression);
                return LiteralEvaluator.EvaluateExplicitCastExpression(node.Type, value);
            }

            #endregion EvaluateExpression
        }
    }
}