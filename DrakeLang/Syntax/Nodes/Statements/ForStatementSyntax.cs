﻿//------------------------------------------------------------------------------
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

using System.Collections.Generic;

namespace DrakeLang.Syntax
{
    public sealed class ForStatementSyntax : LoopStatementSyntax
    {
        internal ForStatementSyntax(
            SyntaxToken forKeyword,
            SyntaxToken openParenthesisToken,
            StatementSyntax? initializationStatement, SyntaxToken initializationSemicolon,
            ExpressionSyntax? condition, SyntaxToken conditionSemicolon,
            StatementSyntax? updateStatement,
            SyntaxToken closeParenthesisToken,
            StatementSyntax body)
            : base(body)
        {
            ForKeyword = forKeyword;
            OpenParenthesisToken = openParenthesisToken;
            InitializationStatement = initializationStatement;
            InitializationSemicolon = initializationSemicolon;
            Condition = condition;
            ConditionSemicolon = conditionSemicolon;
            UpdateStatement = updateStatement;
            CloseParenthesisToken = closeParenthesisToken;
        }

        #region Properties

        public override SyntaxKind Kind => SyntaxKind.ForStatement;

        public SyntaxToken ForKeyword { get; }
        public SyntaxToken OpenParenthesisToken { get; }
        public StatementSyntax? InitializationStatement { get; }
        public SyntaxToken InitializationSemicolon { get; }
        public ExpressionSyntax? Condition { get; }
        public SyntaxToken ConditionSemicolon { get; }
        public StatementSyntax? UpdateStatement { get; }
        public SyntaxToken CloseParenthesisToken { get; }

        #endregion Properties

        #region Methods

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return ForKeyword;

            yield return OpenParenthesisToken;

            if (InitializationStatement is not null) yield return InitializationStatement;
            yield return InitializationSemicolon;
            if (Condition is not null) yield return Condition;
            yield return ConditionSemicolon;
            if (UpdateStatement is not null) yield return UpdateStatement;

            yield return CloseParenthesisToken;

            yield return Body;
        }

        #endregion Methods
    }
}