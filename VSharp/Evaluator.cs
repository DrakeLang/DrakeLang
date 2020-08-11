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

using VSharp.Binding;
using VSharp.Symbols;
using System;
using System.Collections.Generic;

namespace VSharp
{
    internal sealed class Evaluator : IEvaluator
    {
        public Evaluator()
        {
        }

        #region IEvaluator

        public void Evaluate(BoundBlockStatement root, Dictionary<VariableSymbol, object> variables)
        {
            // Create label-index mapping for goto statements.
            Dictionary<LabelSymbol, int> labelToIndex = new Dictionary<LabelSymbol, int>();
            for (int i = 0; i < root.Statements.Length; i++)
            {
                if (root.Statements[i] is BoundLabelStatement l)
                {
                    labelToIndex.Add(l.Label, i + 1);
                }
            }

            // Evaluate program.
            int index = 0;
            while (index < root.Statements.Length)
            {
                BoundStatement s = root.Statements[index];
                switch (s.Kind)
                {
                    case BoundNodeKind.VariableDeclarationStatement:
                        EvaluateVariableDeclarationStatement((BoundVariableDeclarationStatement)s, variables);
                        index++;
                        break;

                    case BoundNodeKind.ExpressionStatement:
                        EvaluateExpressionStatement((BoundExpressionStatement)s, variables);
                        index++;
                        break;

                    case BoundNodeKind.LabelStatement:
                        // do nothing.
                        index++;
                        break;

                    case BoundNodeKind.GotoStatement:
                        BoundGotoStatement gotoStatement = (BoundGotoStatement)s;
                        index = labelToIndex[gotoStatement.Label];
                        break;

                    case BoundNodeKind.ConditionalGotoStatement:
                        BoundConditionalGotoStatement conGotoStatement = (BoundConditionalGotoStatement)s;
                        if (EvaluateConditionalGotoStatement(conGotoStatement, variables))
                            index = labelToIndex[conGotoStatement.Label];
                        else
                            index++;
                        break;

                    case BoundNodeKind.NoOpStatement:
                        index++;
                        break;

                    default:
                        throw new Exception($"Unexpected node '{s.Kind}'.");
                }
            }
        }

        #endregion IEvaluator

        #region EvaluateStatement

        private static void EvaluateVariableDeclarationStatement(BoundVariableDeclarationStatement node, Dictionary<VariableSymbol, object> variables)
        {
            object value = EvaluateExpression(node.Initializer, variables);
            variables[node.Variable] = value;
        }

        private static void EvaluateExpressionStatement(BoundExpressionStatement node, Dictionary<VariableSymbol, object> variables)
        {
            EvaluateExpression(node.Expression, variables);
        }

        private static bool EvaluateConditionalGotoStatement(BoundConditionalGotoStatement s, Dictionary<VariableSymbol, object> variables)
        {
            bool condition = (bool)EvaluateExpression(s.Condition, variables);

            return condition ^ s.JumpIfFalse;
        }

        #endregion EvaluateStatement

        #region EvaluateExpression

        private static object EvaluateExpression(BoundExpression node, Dictionary<VariableSymbol, object> variables) => node.Kind switch
        {
            BoundNodeKind.LiteralExpression => EvaluateLiteralExpression((BoundLiteralExpression)node),
            BoundNodeKind.VariableExpression => EvaluateVariableExpression((BoundVariableExpression)node, variables),
            BoundNodeKind.AssignmentExpression => EvaluateAssignmentExpression((BoundAssignmentExpression)node, variables),
            BoundNodeKind.UnaryExpression => EvaluateUnaryExpression((BoundUnaryExpression)node, variables),
            BoundNodeKind.BinaryExpression => EvaluateBinaryExpression((BoundBinaryExpression)node, variables),
            BoundNodeKind.CallExpression => EvaluateCallExpression((BoundCallExpression)node, variables),
            BoundNodeKind.ExplicitCastExpression => EvaluateExplicitCastExpression((BoundExplicitCastExpression)node, variables),

            _ => throw new Exception($"Unexpected node '{node.Kind}'."),
        };

        private static object EvaluateLiteralExpression(BoundLiteralExpression node)
        {
            return node.Value;
        }

        private static object EvaluateVariableExpression(BoundVariableExpression node, Dictionary<VariableSymbol, object> variables)
        {
            return variables[node.Variable];
        }

        private static object EvaluateAssignmentExpression(BoundAssignmentExpression node, Dictionary<VariableSymbol, object> variables)
        {
            object value = EvaluateExpression(node.Expression, variables);
            variables[node.Variable] = value;

            return value;
        }

        private static object EvaluateUnaryExpression(BoundUnaryExpression node, Dictionary<VariableSymbol, object> variables)
        {
            // Pre- and post increment/decrement.
            if (node.Op.Kind == BoundUnaryOperatorKind.PreDecrement || node.Op.Kind == BoundUnaryOperatorKind.PreIncrement)
            {
                BoundVariableExpression variableExpression = (BoundVariableExpression)node.Operand;

                if (node.Type == TypeSymbol.Int)
                    variables[variableExpression.Variable] = (int)variables[variableExpression.Variable] + (node.Op.Kind == BoundUnaryOperatorKind.PreIncrement ? 1 : -1);
                else
                    variables[variableExpression.Variable] = (double)variables[variableExpression.Variable] + (node.Op.Kind == BoundUnaryOperatorKind.PreIncrement ? 1 : -1);

                return variables[variableExpression.Variable];
            }
            else if (node.Op.Kind == BoundUnaryOperatorKind.PostDecrement || node.Op.Kind == BoundUnaryOperatorKind.PostIncrement)
            {
                BoundVariableExpression variableExpression = (BoundVariableExpression)node.Operand;

                object value = variables[variableExpression.Variable];
                if (node.Type == TypeSymbol.Int)
                {
                    variables[variableExpression.Variable] = (int)variables[variableExpression.Variable] + (node.Op.Kind == BoundUnaryOperatorKind.PostIncrement ? 1 : -1);
                }
                else
                {
                    variables[variableExpression.Variable] = (double)variables[variableExpression.Variable] + (node.Op.Kind == BoundUnaryOperatorKind.PostIncrement ? 1 : -1);
                }

                return value;
            }

            // Other unary operations.
            object operand = EvaluateExpression(node.Operand, variables);
            return LiteralEvaluator.EvaluateUnaryExpression(node.Op, operand);
        }

        private static object EvaluateBinaryExpression(BoundBinaryExpression node, Dictionary<VariableSymbol, object> variables)
        {
            object left = EvaluateExpression(node.Left, variables);
            object right = EvaluateExpression(node.Right, variables);

            return LiteralEvaluator.EvaluateBinaryExpression(node.Op, left, right);
        }

        private static object EvaluateCallExpression(BoundCallExpression node, Dictionary<VariableSymbol, object> variables)
        {
            if (node.Method == BuiltinMethods.Input)
            {
                return Console.ReadLine();
            }
            else if (node.Method == BuiltinMethods.Print)
            {
                string message = (string)EvaluateExpression(node.Arguments[0], variables);
                Console.WriteLine(message);
                return 0; // cannot return null due to nullable reference types being enabled.
            }
            else throw new Exception($"Unexpected method '{node.Method}'.");
        }

        private static object EvaluateExplicitCastExpression(BoundExplicitCastExpression node, Dictionary<VariableSymbol, object> variables)
        {
            var value = EvaluateExpression(node.Expression, variables);
            return LiteralEvaluator.EvaluateExplicitCastExpression(node.Type, value);
        }

        #endregion EvaluateExpression
    }
}