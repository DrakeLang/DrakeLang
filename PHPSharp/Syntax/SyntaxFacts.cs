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
                case SyntaxKind.TildeToken:
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

                case SyntaxKind.AmpersandToken:
                case SyntaxKind.AmpersandAmpersandToken:
                    return 2;

                case SyntaxKind.PipeToken:
                case SyntaxKind.PipePipeToken:
                case SyntaxKind.HatToken:
                    return 1;

                default:
                    return 0;
            }
        }

        public static SyntaxKind GetKeywordKind(string word)
        {
            return word switch
            {
                "bool" => SyntaxKind.BoolKeyword,
                "int" => SyntaxKind.IntKeyword,
                "string" => SyntaxKind.StringKeyword,
                "var" => SyntaxKind.VarKeyword,

                "true" => SyntaxKind.TrueKeyword,
                "false" => SyntaxKind.FalseKeyword,
                "if" => SyntaxKind.IfKeyword,
                "else" => SyntaxKind.ElseKeyword,
                "while" => SyntaxKind.WhileKeyword,
                "for" => SyntaxKind.ForKeyword,

                "typeof" => SyntaxKind.TypeofKeyword,
                "nameof" => SyntaxKind.NameofKeyword,

                _ => SyntaxKind.IdentifierToken,
            };
        }

        /// <summary>
        /// Returns a value indicating if the given syntax kind is an assignment operator (=, +=, |=).
        /// </summary>
        public static bool GetKindIsAssignmentOperator(SyntaxKind kind)
        {
            return kind switch
            {
                SyntaxKind.PlusEqualsToken => true,
                SyntaxKind.MinusEqualsToken => true,
                SyntaxKind.StarEqualsToken => true,
                SyntaxKind.SlashEqualsToken => true,
                SyntaxKind.EqualsToken => true,
                SyntaxKind.AmpersandEqualsToken => true,
                SyntaxKind.PipeEqualsToken => true,

                _ => false,
            };
        }

        /// <summary>
        /// Returns a value indicating if the given syntax kind is an unary operator (+, -, ++, --, !).
        /// </summary>
        public static bool GetKindIsUnaryOperator(SyntaxKind kind)
        {
            return kind switch
            {
                SyntaxKind.PlusToken => true,
                SyntaxKind.PlusPlusToken => true,
                SyntaxKind.MinusToken => true,
                SyntaxKind.MinusMinusToken => true,
                SyntaxKind.BangToken => true,
                SyntaxKind.TildeToken => true,

                _ => false,
            };
        }

        public static bool GetKindIsTypeKeyword(SyntaxKind kind)
        {
            return kind switch
            {
                SyntaxKind.BoolKeyword => true,
                SyntaxKind.IntKeyword => true,
                SyntaxKind.StringKeyword => true,
                SyntaxKind.VarKeyword => true,

                _ => false,
            };
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

        public static string? GetText(SyntaxKind kind)
        {
            return kind switch
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
                SyntaxKind.EqualsEqualsToken => "==",
                SyntaxKind.LessToken => "<",
                SyntaxKind.LessOrEqualsToken => "<=",
                SyntaxKind.GreaterToken => ">",
                SyntaxKind.GreaterOrEqualsToken => ">=",
                SyntaxKind.OpenParenthesisToken => "(",
                SyntaxKind.CloseParenthesisToken => ")",
                SyntaxKind.OpenBraceToken => "{",
                SyntaxKind.CloseBraceToken => "}",
                SyntaxKind.SemicolonToken => ";",

                SyntaxKind.BoolKeyword => "bool",
                SyntaxKind.IntKeyword => "int",
                SyntaxKind.StringKeyword => "string",
                SyntaxKind.VarKeyword => "var",
                SyntaxKind.TrueKeyword => "true",
                SyntaxKind.FalseKeyword => "false",
                SyntaxKind.IfKeyword => "if",
                SyntaxKind.ElseKeyword => "else",
                SyntaxKind.WhileKeyword => "while",
                SyntaxKind.ForKeyword => "for",
                SyntaxKind.TypeofKeyword => "typeof",
                SyntaxKind.NameofKeyword => "nameof",

                _ => null,
            };
        }
    }
}