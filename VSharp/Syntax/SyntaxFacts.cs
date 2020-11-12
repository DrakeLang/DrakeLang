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

namespace VSharp.Syntax
{
    public static class SyntaxFacts
    {
        public static int GetUnaryOperatorPrecedence(this SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.PlusPlusToken:
                case SyntaxKind.MinusMinusToken:
                case SyntaxKind.PlusToken:
                case SyntaxKind.MinusToken:
                case SyntaxKind.BangToken:
                case SyntaxKind.TildeToken:
                    return 10;

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
                case SyntaxKind.PercentToken:
                    return 10;

                case SyntaxKind.PlusToken:
                case SyntaxKind.MinusToken:
                    return 9;

                case SyntaxKind.LessToken:
                case SyntaxKind.LessEqualsToken:
                case SyntaxKind.GreaterToken:
                case SyntaxKind.GreaterEqualsToken:
                    return 8;

                case SyntaxKind.EqualsEqualsToken:
                case SyntaxKind.BangEqualsToken:
                    return 7;

                case SyntaxKind.AmpersandToken:
                    return 6;

                case SyntaxKind.HatToken:
                    return 5;

                case SyntaxKind.PipeToken:
                    return 4;

                case SyntaxKind.AmpersandAmpersandToken:
                    return 3;

                case SyntaxKind.PipePipeToken:
                    return 2;

                case SyntaxKind.PipeGreaterToken:
                    return 1;

                default:
                    return 0;
            }
        }

        /// <summary>
        /// Returns a value indicating if the given syntax kind is an assignment operator (=, +=, |=).
        /// </summary>
        public static bool IsAssignmentOperator(this SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.EqualsToken:
                case SyntaxKind.PlusEqualsToken:
                case SyntaxKind.MinusEqualsToken:
                case SyntaxKind.StarEqualsToken:
                case SyntaxKind.SlashEqualsToken:
                case SyntaxKind.AmpersandEqualsToken:
                case SyntaxKind.PipeEqualsToken:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Returns a value indicating if the given syntax kind is an unary operator (+, -, ++, --, !).
        /// </summary>
        public static bool IsUnaryOperator(this SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.PlusToken:
                case SyntaxKind.PlusPlusToken:
                case SyntaxKind.MinusToken:
                case SyntaxKind.MinusMinusToken:
                case SyntaxKind.BangToken:
                case SyntaxKind.TildeToken:
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsTypeKeyword(this SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.BoolKeyword:
                case SyntaxKind.IntKeyword:
                case SyntaxKind.FloatKeyword:
                case SyntaxKind.StringKeyword:
                case SyntaxKind.VarKeyword:
                    return true;

                default:
                    return false;
            }
        }

        public static string? GetText(this SyntaxKind kind) => kind switch
        {
            SyntaxKind.PlusToken => "+",
            SyntaxKind.PlusPlusToken => "++",
            SyntaxKind.PlusEqualsToken => "+=",
            SyntaxKind.MinusToken => "-",
            SyntaxKind.MinusMinusToken => "--",
            SyntaxKind.MinusEqualsToken => "-=",
            SyntaxKind.StarToken => "*",
            SyntaxKind.StarEqualsToken => "*=",
            SyntaxKind.SlashToken => "/",
            SyntaxKind.SlashEqualsToken => "/=",
            SyntaxKind.PercentToken => "%",
            SyntaxKind.BangToken => "!",
            SyntaxKind.BangEqualsToken => "!=",
            SyntaxKind.EqualsToken => "=",
            SyntaxKind.TildeToken => "~",
            SyntaxKind.HatToken => "^",
            SyntaxKind.AmpersandToken => "&",
            SyntaxKind.AmpersandAmpersandToken => "&&",
            SyntaxKind.AmpersandEqualsToken => "&=",
            SyntaxKind.PipeToken => "|",
            SyntaxKind.PipePipeToken => "||",
            SyntaxKind.PipeEqualsToken => "|=",
            SyntaxKind.PipeGreaterToken => "|>",
            SyntaxKind.EqualsEqualsToken => "==",
            SyntaxKind.LessEqualsToken => "<=",
            SyntaxKind.GreaterEqualsToken => ">=",
            SyntaxKind.LessToken => "<",
            SyntaxKind.GreaterToken => ">",
            SyntaxKind.EqualGreaterToken => "=>",
            SyntaxKind.OpenParenthesisToken => "(",
            SyntaxKind.CloseParenthesisToken => ")",
            SyntaxKind.OpenBraceToken => "{",
            SyntaxKind.CloseBraceToken => "}",
            SyntaxKind.CommaToken => ",",
            SyntaxKind.ColonToken => ":",
            SyntaxKind.SemicolonToken => ";",

            SyntaxKind.BoolKeyword => "bool",
            SyntaxKind.IntKeyword => "int",
            SyntaxKind.FloatKeyword => "float",
            SyntaxKind.StringKeyword => "string",
            SyntaxKind.VarKeyword => "var",
            SyntaxKind.TrueKeyword => "true",
            SyntaxKind.FalseKeyword => "false",
            SyntaxKind.DefKeyword => "def",
            SyntaxKind.IfKeyword => "if",
            SyntaxKind.ElseKeyword => "else",
            SyntaxKind.WhileKeyword => "while",
            SyntaxKind.GoToKeyword => "goto",
            SyntaxKind.ForKeyword => "for",
            SyntaxKind.ReturnKeyword => "return",
            SyntaxKind.ContinueKeyword => "continue",
            SyntaxKind.BreakKeyword => "break",
            SyntaxKind.TypeofKeyword => "typeof",
            SyntaxKind.NameofKeyword => "nameof",

            _ => null,
        };

        public static bool TryGetKeywordKind(string word, out SyntaxKind keywordKind)
        {
            keywordKind = word switch
            {
                "def" => SyntaxKind.DefKeyword,
                "bool" => SyntaxKind.BoolKeyword,
                "int" => SyntaxKind.IntKeyword,
                "float" => SyntaxKind.FloatKeyword,
                "string" => SyntaxKind.StringKeyword,
                "var" => SyntaxKind.VarKeyword,

                "true" => SyntaxKind.TrueKeyword,
                "false" => SyntaxKind.FalseKeyword,

                "if" => SyntaxKind.IfKeyword,
                "else" => SyntaxKind.ElseKeyword,
                "while" => SyntaxKind.WhileKeyword,
                "goto" => SyntaxKind.GoToKeyword,
                "for" => SyntaxKind.ForKeyword,
                "return" => SyntaxKind.ReturnKeyword,
                "continue" => SyntaxKind.ContinueKeyword,
                "break" => SyntaxKind.BreakKeyword,

                "typeof" => SyntaxKind.TypeofKeyword,
                "nameof" => SyntaxKind.NameofKeyword,

                _ => SyntaxKind.BadToken,
            };
            return keywordKind != SyntaxKind.BadToken;
        }

        public static IEnumerable<SyntaxKind> GetUnaryOperatorKinds()
        {
            SyntaxKind[] kinds = (SyntaxKind[])Enum.GetValues(typeof(SyntaxKind));
            foreach (SyntaxKind kind in kinds)
            {
                if (kind.GetUnaryOperatorPrecedence() > 0)
                    yield return kind;
            }
        }

        public static IEnumerable<SyntaxKind> GetBinaryOperatorKinds()
        {
            SyntaxKind[] kinds = (SyntaxKind[])Enum.GetValues(typeof(SyntaxKind));
            foreach (SyntaxKind kind in kinds)
            {
                if (kind.GetBinaryOperatorPrecedence() > 0)
                    yield return kind;
            }
        }
    }
}