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
        public static int GetUnaryOperatorPrecedence(this SyntaxKind kind) => kind switch
        {
            SyntaxKind.PlusPlusToken or
            SyntaxKind.MinusMinusToken or
            SyntaxKind.PlusToken or
            SyntaxKind.MinusToken or
            SyntaxKind.BangToken or
            SyntaxKind.TildeToken => 10,

            _ => 0,
        };

        public static int GetBinaryOperatorPrecedence(this SyntaxKind kind) => kind switch
        {
            SyntaxKind.StarToken or
            SyntaxKind.SlashToken or
            SyntaxKind.PercentToken => 10,
            SyntaxKind.PlusToken or
            SyntaxKind.MinusToken => 9,
            SyntaxKind.LessToken or
            SyntaxKind.LessEqualsToken or
            SyntaxKind.GreaterToken or
            SyntaxKind.GreaterEqualsToken => 8,
            SyntaxKind.EqualsEqualsToken or
            SyntaxKind.BangEqualsToken => 7,
            SyntaxKind.AmpersandToken => 6,
            SyntaxKind.HatToken => 5,
            SyntaxKind.PipeToken => 4,
            SyntaxKind.AmpersandAmpersandToken => 3,
            SyntaxKind.PipePipeToken => 2,
            SyntaxKind.PipeGreaterToken => 1,

            _ => 0,
        };

        /// <summary>
        /// Returns a value indicating if the given syntax kind is an assignment operator (=, +=, |=).
        /// </summary>
        public static bool IsAssignmentOperator(this SyntaxKind kind) => kind is
            SyntaxKind.EqualsToken or
            SyntaxKind.PlusEqualsToken or
            SyntaxKind.MinusEqualsToken or
            SyntaxKind.StarEqualsToken or
            SyntaxKind.SlashEqualsToken or
            SyntaxKind.AmpersandEqualsToken or
            SyntaxKind.PipeEqualsToken;

        /// <summary>
        /// Returns a value indicating if the given syntax kind is an unary operator (+, -, ++, --, !).
        /// </summary>
        public static bool IsUnaryOperator(this SyntaxKind kind) => kind is
            SyntaxKind.PlusToken or
            SyntaxKind.PlusPlusToken or
            SyntaxKind.MinusToken or
            SyntaxKind.MinusMinusToken or
            SyntaxKind.BangToken or
            SyntaxKind.TildeToken;

        public static bool IsTypeKeyword(this SyntaxKind kind) => kind.IsImplicitTypeKeyword() || kind.IsExplicitTypeKeyword();

        public static bool IsExplicitTypeKeyword(this SyntaxKind kind) => kind is
            SyntaxKind.ObjectKeyword or
            SyntaxKind.BoolKeyword or
            SyntaxKind.IntKeyword or
            SyntaxKind.FloatKeyword or
            SyntaxKind.StringKeyword or
            SyntaxKind.CharKeyword;

        public static bool IsImplicitTypeKeyword(this SyntaxKind kind) => kind is
            SyntaxKind.VarKeyword or
            SyntaxKind.SetKeyword;

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
            SyntaxKind.EqualsGreaterToken => "=>",
            SyntaxKind.OpenParenthesisToken => "(",
            SyntaxKind.CloseParenthesisToken => ")",
            SyntaxKind.OpenBraceToken => "{",
            SyntaxKind.CloseBraceToken => "}",
            SyntaxKind.OpenBracketToken => "[",
            SyntaxKind.CloseBracketToken => "]",
            SyntaxKind.DotToken => ".",
            SyntaxKind.CommaToken => ",",
            SyntaxKind.UnderscoreToken => "_",
            SyntaxKind.ColonToken => ":",
            SyntaxKind.SemicolonToken => ";",

            SyntaxKind.ObjectKeyword => "object",
            SyntaxKind.BoolKeyword => "bool",
            SyntaxKind.IntKeyword => "int",
            SyntaxKind.FloatKeyword => "float",
            SyntaxKind.StringKeyword => "string",
            SyntaxKind.CharKeyword => "char",
            SyntaxKind.VarKeyword => "var",
            SyntaxKind.SetKeyword => "set",
            SyntaxKind.TrueKeyword => "true",
            SyntaxKind.FalseKeyword => "false",
            SyntaxKind.DefKeyword => "def",
            SyntaxKind.NamespaceKeyword => "namespace",
            SyntaxKind.IfKeyword => "if",
            SyntaxKind.ElseKeyword => "else",
            SyntaxKind.WhileKeyword => "while",
            SyntaxKind.GoToKeyword => "goto",
            SyntaxKind.ForKeyword => "for",
            SyntaxKind.ReturnKeyword => "return",
            SyntaxKind.WithKeyword => "with",
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
                "object" => SyntaxKind.ObjectKeyword,
                "bool" => SyntaxKind.BoolKeyword,
                "int" => SyntaxKind.IntKeyword,
                "float" => SyntaxKind.FloatKeyword,
                "string" => SyntaxKind.StringKeyword,
                "char" => SyntaxKind.CharKeyword,
                "var" => SyntaxKind.VarKeyword,
                "set" => SyntaxKind.SetKeyword,

                "def" => SyntaxKind.DefKeyword,
                "namespace" => SyntaxKind.NamespaceKeyword,

                "true" => SyntaxKind.TrueKeyword,
                "false" => SyntaxKind.FalseKeyword,

                "if" => SyntaxKind.IfKeyword,
                "else" => SyntaxKind.ElseKeyword,
                "while" => SyntaxKind.WhileKeyword,
                "goto" => SyntaxKind.GoToKeyword,
                "for" => SyntaxKind.ForKeyword,
                "return" => SyntaxKind.ReturnKeyword,
                "with" => SyntaxKind.WithKeyword,
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