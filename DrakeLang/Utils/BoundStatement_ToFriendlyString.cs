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

using System;
using System.Linq;

namespace DrakeLang.Binding
{
    internal static class BoundStatement_ToFriendlyString
    {
        public static string ToFriendlyString(this BoundNode node) => node.Kind switch
        {
            BoundNodeKind.VariableDeclarationStatement => StringifyVariableDeclarationStatement((BoundVariableDeclarationStatement)node),
            BoundNodeKind.LabelStatement => StringifyLabelStatement((BoundLabelStatement)node),
            BoundNodeKind.GotoStatement => StringifyGotoStatement((BoundGotoStatement)node),
            BoundNodeKind.ConditionalGotoStatement => StringifyConditionalGotoStatement((BoundConditionalGotoStatement)node),
            BoundNodeKind.ReturnStatement => StringifyReturnStatement((BoundReturnStatement)node),
            BoundNodeKind.ExpressionStatement => StringifyExpressionStatement((BoundExpressionStatement)node),

            BoundNodeKind.ErrorExpression => StringifyErrorExpression((BoundErrorExpression)node),
            BoundNodeKind.LiteralExpression => StringifyLiteralExpression((BoundLiteralExpression)node),
            BoundNodeKind.VariableExpression => StringifyVariableExpression((BoundVariableExpression)node),
            BoundNodeKind.AssignmentExpression => StringifyAssignmentExpression((BoundAssignmentExpression)node),
            BoundNodeKind.UnaryExpression => StringifyUnaryExpression((BoundUnaryExpression)node),
            BoundNodeKind.BinaryExpression => StringifyBinaryExpression((BoundBinaryExpression)node),
            BoundNodeKind.CallExpression => StringifyCallExpression((BoundCallExpression)node),
            BoundNodeKind.ExplicitCastExpression => StringifyExplicitCastExpression((BoundExplicitCastExpression)node),
            BoundNodeKind.ArrayInitializationExpression => StringifyArrayInitializationExpression((BoundArrayInitializationExpression)node),

            _ => throw new Exception($"Unexpected node '{node.Kind}'."),
        };

        private static string StringifyVariableDeclarationStatement(BoundVariableDeclarationStatement statement)
        {
            return statement.Variable + " = " + ToFriendlyString(statement.Initializer);
        }

        private static string StringifyLabelStatement(BoundLabelStatement statement)
        {
            return statement.Label + ":";
        }

        private static string StringifyGotoStatement(BoundGotoStatement statement)
        {
            return "goto " + statement.Label;
        }

        private static string StringifyConditionalGotoStatement(BoundConditionalGotoStatement statement)
        {
            return "goto " + statement.Label + (statement.JumpIfFalse ? " on false" : " on true");
        }

        private static string StringifyReturnStatement(BoundReturnStatement statement)
        {
            if (statement.Expression is null)
                return "return";
            else
                return "return " + ToFriendlyString(statement.Expression);
        }

        private static string StringifyExpressionStatement(BoundExpressionStatement statement)
        {
            return ToFriendlyString(statement.Expression);
        }

        private static string StringifyErrorExpression(BoundErrorExpression node)
        {
            GC.KeepAlive(node);
            return "??";
        }

        private static string StringifyLiteralExpression(BoundLiteralExpression node)
        {
            return node.Value is string strValue
                ? "\"" + strValue + "\""
                : node.Value.ToString() ?? string.Empty;
        }

        private static string StringifyVariableExpression(BoundVariableExpression node)
        {
            return node.Variable.Name;
        }

        private static string StringifyAssignmentExpression(BoundAssignmentExpression node)
        {
            return node.Variable.Name + " = " + ToFriendlyString(node.Expression);
        }

        private static string StringifyUnaryExpression(BoundUnaryExpression node)
        {
            return node.Op.Kind switch
            {
                BoundUnaryOperatorKind.Identity => ToFriendlyString(node.Operand),
                BoundUnaryOperatorKind.Negation => "-" + ToFriendlyString(node.Operand),
                BoundUnaryOperatorKind.PreIncrement => "++" + ToFriendlyString(node.Operand),
                BoundUnaryOperatorKind.PreDecrement => "--" + ToFriendlyString(node.Operand),
                BoundUnaryOperatorKind.PostDecrement => ToFriendlyString(node.Operand) + "--",
                BoundUnaryOperatorKind.PostIncrement => ToFriendlyString(node.Operand) + "++",
                BoundUnaryOperatorKind.LogicalNegation => "!" + ToFriendlyString(node.Operand),
                BoundUnaryOperatorKind.OnesComplement => "~" + ToFriendlyString(node.Operand),

                _ => throw new Exception($"Unexpected unary operator '{node.Op.Kind}'."),
            };
        }

        private static string StringifyBinaryExpression(BoundBinaryExpression node)
        {
            return node.Op.Kind switch
            {
                BoundBinaryOperatorKind.Addition => ToFriendlyString(node.Left) + " + " + ToFriendlyString(node.Right),
                BoundBinaryOperatorKind.Subtraction => ToFriendlyString(node.Left) + " - " + ToFriendlyString(node.Right),
                BoundBinaryOperatorKind.Multiplication => ToFriendlyString(node.Left) + " * " + ToFriendlyString(node.Right),
                BoundBinaryOperatorKind.Division => ToFriendlyString(node.Left) + " / " + ToFriendlyString(node.Right),
                BoundBinaryOperatorKind.Modulo => ToFriendlyString(node.Left) + " % " + ToFriendlyString(node.Right),
                BoundBinaryOperatorKind.BitwiseAnd => ToFriendlyString(node.Left) + " & " + ToFriendlyString(node.Right),
                BoundBinaryOperatorKind.LogicalAnd => ToFriendlyString(node.Left) + " && " + ToFriendlyString(node.Right),
                BoundBinaryOperatorKind.BitwiseOr => ToFriendlyString(node.Left) + " | " + ToFriendlyString(node.Right),
                BoundBinaryOperatorKind.LogicalOr => ToFriendlyString(node.Left) + " || " + ToFriendlyString(node.Right),
                BoundBinaryOperatorKind.BitwiseXor => ToFriendlyString(node.Left) + " ^ " + ToFriendlyString(node.Right),
                BoundBinaryOperatorKind.Equals => ToFriendlyString(node.Left) + " == " + ToFriendlyString(node.Right),
                BoundBinaryOperatorKind.NotEquals => ToFriendlyString(node.Left) + " != " + ToFriendlyString(node.Right),
                BoundBinaryOperatorKind.LessThan => ToFriendlyString(node.Left) + " < " + ToFriendlyString(node.Right),
                BoundBinaryOperatorKind.LessThanOrEquals => ToFriendlyString(node.Left) + " <= " + ToFriendlyString(node.Right),
                BoundBinaryOperatorKind.GreaterThan => ToFriendlyString(node.Left) + " > " + ToFriendlyString(node.Right),
                BoundBinaryOperatorKind.GreaterThanOrEquals => ToFriendlyString(node.Left) + " >= " + ToFriendlyString(node.Right),

                _ => throw new Exception($"Unexpected binary operator '{node.Op.Kind}'."),
            };
        }

        private static string StringifyCallExpression(BoundCallExpression node)
        {
            var method = node.Method;
            var arguments = method.Parameters.Zip(node.Arguments, (parameterSymbol, argument) => (parameterSymbol, argument))
                .Select(pair => pair.parameterSymbol.Type + ": " + pair.argument.ToFriendlyString());
            var formattedArguments = string.Join(", ", arguments);

            var str = $"{method.FullName}({formattedArguments})";
            if (node.Operand is not null)
                return node.Operand.ToFriendlyString() + "." + str;
            else
                return str;
        }

        private static string StringifyExplicitCastExpression(BoundExplicitCastExpression node)
        {
            return "(" + node.Type + ")" + ToFriendlyString(node.Expression);
        }

        private static string StringifyArrayInitializationExpression(BoundArrayInitializationExpression node)
        {
            return node.Type.GenericTypeArguments[0] + "[" + node.SizeExpression.ToFriendlyString() + "]" +
                " => " + string.Join(", ", node.Initializer.Select(init => init.ToFriendlyString()));
        }
    }
}