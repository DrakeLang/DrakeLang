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

namespace VSharp.Syntax
{
    public enum SyntaxKind
    {
        // Tokens

        BadToken,
        EndOfFileToken,
        IntegerToken,
        FloatToken,
        StringToken,
        ColonToken,
        SemicolonToken,
        PlusToken,
        PlusPlusToken,
        PlusEqualsToken,
        MinusToken,
        MinusMinusToken,
        MinusEqualsToken,
        StarToken,
        StarEqualsToken,
        SlashToken,
        SlashEqualsToken,
        PercentToken,
        BangToken,
        TildeToken,
        HatToken,
        EqualsToken,
        AmpersandToken,
        AmpersandAmpersandToken,
        AmpersandEqualsToken,
        PipeToken,
        PipePipeToken,
        PipeEqualsToken,
        EqualsEqualsToken,
        BangEqualsToken,
        LessEqualsToken,
        GreaterEqualsToken,
        LessToken,
        GreaterToken,
        OpenParenthesisToken,
        CloseParenthesisToken,
        OpenBraceToken,
        CloseBraceToken,
        IdentifierToken,
        CommaToken,
        WhitespaceToken,
        LineCommentToken,
        MultiLineCommentToken,

        // Keywords

        BoolKeyword,
        IntKeyword,
        FloatKeyword,
        StringKeyword,
        VarKeyword,
        TrueKeyword,
        FalseKeyword,
        DefKeyword,
        IfKeyword,
        ElseKeyword,
        WhileKeyword,
        ForKeyword,
        ReturnKeyword,
        ContinueKeyword,
        BreakKeyword,
        TypeofKeyword,
        NameofKeyword,

        // Nodes

        CompilationUnit,
        ElseClause,
        Parameter,

        // Statements

        BlockStatement,
        VariableDeclarationStatement,
        MethodDeclarationStatement,
        IfStatement,
        WhileStatement,
        ForStatement,
        GoToKeyword,
        GoToStatement,
        LabelStatement,
        ReturnStatement,
        ContinueStatement,
        BreakStatement,
        ExpressionStatement,

        // Expressions

        LiteralExpression,
        NameExpression,
        UnaryExpression,
        BinaryExpression,
        ParenthesizedExpression,
        TypeofExpression,
        NameofExpression,
        TypeExpression,
        AssignmentExpression,
        CallExpression,
        ExplicitCastExpression,
    }
}