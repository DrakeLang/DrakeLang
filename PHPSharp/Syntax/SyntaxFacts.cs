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
                case "else":
                    return SyntaxKind.ElseKeyword;

                case "false":
                    return SyntaxKind.FalseKeyword;

                case "if":
                    return SyntaxKind.IfKeyword;

                case "let":
                    return SyntaxKind.LetKeyword;

                case "true":
                    return SyntaxKind.TrueKeyword;

                case "var":
                    return SyntaxKind.VarKeyword;

                default:
                    return SyntaxKind.IdentifierToken;
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

                case SyntaxKind.MinusToken:
                    return "-";

                case SyntaxKind.StarToken:
                    return "*";

                case SyntaxKind.SlashToken:
                    return "/";

                case SyntaxKind.BangToken:
                    return "!";

                case SyntaxKind.EqualsToken:
                    return "=";

                case SyntaxKind.AmpersandAmpersandToken:
                    return "&&";

                case SyntaxKind.PipePipeToken:
                    return "||";

                case SyntaxKind.EqualsEqualsToken:
                    return "==";

                case SyntaxKind.BangEqualsToken:
                    return "!=";

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

                case SyntaxKind.ElseKeyword:
                    return "else";

                case SyntaxKind.FalseKeyword:
                    return "false";

                case SyntaxKind.IfKeyword:
                    return "if";

                case SyntaxKind.LetKeyword:
                    return "let";

                case SyntaxKind.VarKeyword:
                    return "var";

                case SyntaxKind.TrueKeyword:
                    return "true";

                case SyntaxKind.SemicolonToken:
                    return ";";

                default:
                    return null;
            }
        }
    }
}