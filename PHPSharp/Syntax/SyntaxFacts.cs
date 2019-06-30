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

using System;
using System.Collections.Generic;

namespace PHPSharp.Syntax
{
    public static class SyntaxFacts
    {
        public static int GetUnaryOperatorPrecedence(this SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.PlusPlusToken:
                case SyntaxKind.MinusMinusToken:
                    return 7;

                case SyntaxKind.PlusToken:
                case SyntaxKind.MinusToken:
                case SyntaxKind.BangToken:
                    return 6;

                default:
                    return 0;
            }
        }

        public static int GetBinaryOperatorPrecedence(this SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.StarToken:
                case SyntaxKind.SlashToken:
                    return 5;

                case SyntaxKind.PlusToken:
                case SyntaxKind.MinusToken:
                    return 4;

                case SyntaxKind.EqualsEqualsToken:
                case SyntaxKind.BangEqualsToken:
                case SyntaxKind.LessToken:
                case SyntaxKind.LessOrEqualsToken:
                case SyntaxKind.GreaterToken:
                case SyntaxKind.GreaterOrEqualsToken:
                    return 3;

                case SyntaxKind.AmpersandAmpersandToken:
                    return 2;

                case SyntaxKind.PipePipeToken:
                    return 1;

                default:
                    return 0;
            }
        }

        public static SyntaxKind GetKeywordKind(string word)
        {
            switch (word)
            {
                case "bool":
                    return SyntaxKind.BoolKeyword;

                case "else":
                    return SyntaxKind.ElseKeyword;

                case "false":
                    return SyntaxKind.FalseKeyword;

                case "for":
                    return SyntaxKind.ForKeyword;

                case "if":
                    return SyntaxKind.IfKeyword;

                case "int":
                    return SyntaxKind.IntKeyword;

                case "true":
                    return SyntaxKind.TrueKeyword;

                case "var":
                    return SyntaxKind.VarKeyword;

                case "while":
                    return SyntaxKind.WhileKeyword;

                default:
                    return SyntaxKind.IdentifierToken;
            }
        }

        /// <summary>
        /// Returns a value indicating if the given syntax kind is an assignment operator (=, +=, |=).
        /// </summary>
        public static bool GetKindIsAssignmentOperator(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.PlusEqualsToken:
                case SyntaxKind.MinusEqualsToken:
                case SyntaxKind.StarEqualsToken:
                case SyntaxKind.SlashEqualsToken:
                case SyntaxKind.EqualsToken:
                case SyntaxKind.AmpersandEqualsToken:
                case SyntaxKind.PipeEqualsToken:
                    return true;

                default:
                    return false;
            }
        }

        public static IEnumerable<SyntaxKind> GetUnaryOperatorKinds()
        {
            SyntaxKind[] kinds = (SyntaxKind[])Enum.GetValues(typeof(SyntaxKind));
            foreach (SyntaxKind kind in kinds)
            {
                if (GetUnaryOperatorPrecedence(kind) > 0)
                    yield return kind;
            }
        }

        public static IEnumerable<SyntaxKind> GetBinaryOperatorKinds()
        {
            SyntaxKind[] kinds = (SyntaxKind[])Enum.GetValues(typeof(SyntaxKind));
            foreach (SyntaxKind kind in kinds)
            {
                if (GetBinaryOperatorPrecedence(kind) > 0)
                    yield return kind;
            }
        }

        public static string GetText(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.PlusToken:
                    return "+";

                case SyntaxKind.PlusPlusToken:
                    return "++";

                case SyntaxKind.PlusEqualsToken:
                    return "+=";

                case SyntaxKind.MinusToken:
                    return "-";

                case SyntaxKind.MinusMinusToken:
                    return "--";

                case SyntaxKind.MinusEqualsToken:
                    return "-=";

                case SyntaxKind.StarToken:
                    return "*";

                case SyntaxKind.StarEqualsToken:
                    return "*=";

                case SyntaxKind.SlashToken:
                    return "/";

                case SyntaxKind.SlashEqualsToken:
                    return "/=";

                case SyntaxKind.BangToken:
                    return "!";

                case SyntaxKind.BangEqualsToken:
                    return "!=";

                case SyntaxKind.EqualsToken:
                    return "=";

                case SyntaxKind.AmpersandToken:
                    return "&";

                case SyntaxKind.AmpersandAmpersandToken:
                    return "&&";

                case SyntaxKind.AmpersandEqualsToken:
                    return "&=";

                case SyntaxKind.PipeToken:
                    return "|";

                case SyntaxKind.PipePipeToken:
                    return "||";

                case SyntaxKind.PipeEqualsToken:
                    return "|=";

                case SyntaxKind.EqualsEqualsToken:
                    return "==";

                case SyntaxKind.LessToken:
                    return "<";

                case SyntaxKind.LessOrEqualsToken:
                    return "<=";

                case SyntaxKind.GreaterToken:
                    return ">";

                case SyntaxKind.GreaterOrEqualsToken:
                    return ">=";

                case SyntaxKind.OpenParenthesisToken:
                    return "(";

                case SyntaxKind.CloseParenthesisToken:
                    return ")";

                case SyntaxKind.OpenBraceToken:
                    return "{";

                case SyntaxKind.CloseBraceToken:
                    return "}";

                case SyntaxKind.BoolKeyword:
                    return "bool";

                case SyntaxKind.ElseKeyword:
                    return "else";

                case SyntaxKind.FalseKeyword:
                    return "false";

                case SyntaxKind.ForKeyword:
                    return "for";

                case SyntaxKind.IfKeyword:
                    return "if";

                case SyntaxKind.IntKeyword:
                    return "int";

                case SyntaxKind.TrueKeyword:
                    return "true";

                case SyntaxKind.VarKeyword:
                    return "var";

                case SyntaxKind.WhileKeyword:
                    return "while";

                case SyntaxKind.SemicolonToken:
                    return ";";

                default:
                    return null;
            }
        }
    }
}