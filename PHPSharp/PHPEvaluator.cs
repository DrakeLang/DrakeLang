//------------------------------------------------------------------------------
// PHP Sharp. Because PHP isn't good enough.
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

using PHPSharp.Binding;
using System;
using System.Text;

namespace PHPSharp
{
    internal sealed class PHPEvaluator : IEvaluator
    {
        private readonly BoundStatement _root;

        public PHPEvaluator(BoundStatement root)
        {
            _root = root;
        }

        #region IEvaluator

        public object Evaluate()
        {
            string result = EvaluateStatement(_root);
            return $"<?php {result} ?>";
        }

        #endregion IEvaluator

        #region EvaluateStatement

        private string EvaluateStatement(BoundStatement node)
        {
            return node.Kind switch
            {
                BoundNodeKind.BlockStatement => EvaluateBlockStatement((BoundBlockStatement)node),
                BoundNodeKind.VariableDeclarationStatement => EvaluateVariableDeclarationStatement((BoundVariableDeclarationStatement)node),
                BoundNodeKind.IfStatement => EvaluateIfStatement((BoundIfStatement)node),
                BoundNodeKind.WhileStatement => EvaluateWhileStatement((BoundWhileStatement)node),
                BoundNodeKind.ExpressionStatement => EvaluateExpressionStatement((BoundExpressionStatement)node),

                _ => throw new Exception($"Unexpected node '{node.Kind}'."),
            };
        }

        private string EvaluateBlockStatement(BoundBlockStatement node)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("{");
            foreach (BoundStatement statement in node.Statements)
            {
                string evaluatedStatement = EvaluateStatement(statement);
                sb.AppendLine(evaluatedStatement);
            }
            sb.AppendLine("}");

            return sb.ToString();
        }

        private string EvaluateVariableDeclarationStatement(BoundVariableDeclarationStatement node)
        {
            string value = EvaluateExpression(node.Initializer);

            return "$" + node.Variable.Name + " = " + value + ";";
        }

        private string EvaluateIfStatement(BoundIfStatement node)
        {
            string condition = EvaluateExpression(node.Condition);
            string thenStatement = EvaluateStatement(node.ThenStatement);
            string? elseStatement = node.ElseStatement == null ? null : EvaluateStatement(node.ElseStatement);

            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"if ({condition})");
            sb.AppendLine(thenStatement);
            if (elseStatement != null)
            {
                sb.AppendLine("else");
                sb.AppendLine(elseStatement);
            }

            return sb.ToString();
        }

        private string EvaluateWhileStatement(BoundWhileStatement node)
        {
            string condition = EvaluateExpression(node.Condition);
            string body = EvaluateStatement(node.Body);

            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"while ({condition})");
            sb.AppendLine(body);

            return sb.ToString();
        }

        private string EvaluateExpressionStatement(BoundExpressionStatement node)
        {
            return EvaluateExpression(node.Expression) + ";";
        }

        #endregion EvaluateStatement

        #region EvaluateExpression

        private string EvaluateExpression(BoundExpression node)
        {
            return node.Kind switch
            {
                BoundNodeKind.LiteralExpression => EvaluateLiteralExpression((BoundLiteralExpression)node),
                BoundNodeKind.VariableExpression => EvaluateVariableExpression((BoundVariableExpression)node),
                BoundNodeKind.AssignmentExpression => EvaluateAssignmentExpression((BoundAssignmentExpression)node),
                BoundNodeKind.UnaryExpression => EvaluateUnaryExpression((BoundUnaryExpression)node),
                BoundNodeKind.BinaryExpression => EvaluateBinaryExpression((BoundBinaryExpression)node),

                _ => throw new Exception($"Unexpected node '{node.Kind}'."),
            };
        }

        private static string EvaluateLiteralExpression(BoundLiteralExpression node)
        {
            if (node.Type != typeof(bool))
                return node.Value.ToString() ?? string.Empty;
            else
            {
                bool boolVal = (bool)node.Value;
                return boolVal ? "true" : "false";
            }
        }

        private static string EvaluateVariableExpression(BoundVariableExpression node)
        {
            return "$" + node.Variable.Name;
        }

        private string EvaluateAssignmentExpression(BoundAssignmentExpression node)
        {
            return "$" + node.Variable.Name + "=" + EvaluateExpression(node.Expression);
        }

        private string EvaluateUnaryExpression(BoundUnaryExpression node)
        {
            string operand = EvaluateExpression(node.Operand);
            if (node.Op.Kind == BoundUnaryOperatorKind.PreDecrement || node.Op.Kind == BoundUnaryOperatorKind.PreIncrement)
            {
                BoundVariableExpression variableExpression = (BoundVariableExpression)node.Operand;
                return (node.Op.Kind == BoundUnaryOperatorKind.PreDecrement ? "--" : "++") + "$" + variableExpression.Variable.Name;
            }
            else if (node.Op.Kind == BoundUnaryOperatorKind.PostDecrement || node.Op.Kind == BoundUnaryOperatorKind.PostIncrement)
            {
                BoundVariableExpression variableExpression = (BoundVariableExpression)node.Operand;
                return "$" + variableExpression.Variable.Name + (node.Op.Kind == BoundUnaryOperatorKind.PreDecrement ? "--" : "++");
            }

            return node.Op.Kind switch
            {
                BoundUnaryOperatorKind.Identity => operand,
                BoundUnaryOperatorKind.Negation => "-" + operand,
                BoundUnaryOperatorKind.LogicalNegation => "!" + operand,
                BoundUnaryOperatorKind.OnesComplement => "~" + operand,

                _ => throw new Exception($"Unexpected unary operator '{node.Op.Kind}'."),
            };
        }

        private string EvaluateBinaryExpression(BoundBinaryExpression node)
        {
            string left = "(" + EvaluateExpression(node.Left);
            string right = EvaluateExpression(node.Right) + ")";

            return node.Op.Kind switch
            {
                BoundBinaryOperatorKind.Addition => ret("+"),
                BoundBinaryOperatorKind.Subtraction => ret("-"),
                BoundBinaryOperatorKind.Multiplication => ret("*"),
                BoundBinaryOperatorKind.Division => ret("/"),
                BoundBinaryOperatorKind.BitwiseAnd => ret("&"),
                BoundBinaryOperatorKind.BitwiseOr => ret("|"),
                BoundBinaryOperatorKind.BitwiseXor => ret("^"),
                BoundBinaryOperatorKind.LogicalAnd => ret("&&"),
                BoundBinaryOperatorKind.LogicalOr => ret("||"),
                BoundBinaryOperatorKind.Equals => ret("=="),
                BoundBinaryOperatorKind.NotEquals => ret("!="),
                BoundBinaryOperatorKind.LessThan => ret("<"),
                BoundBinaryOperatorKind.LessThanOrEquals => ret("<="),
                BoundBinaryOperatorKind.GreaterThan => ret(">"),
                BoundBinaryOperatorKind.GreaterThanOrEquals => ret(">="),

                _ => throw new Exception($"Unexpected binary operator '{node.Op.Kind}'."),
            };

            string ret(string op)
            {
                return left + op + right;
            }
        }

        #endregion EvaluateExpression
    }
}