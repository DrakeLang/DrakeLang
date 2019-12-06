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

using PHPSharp.Text;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace PHPSharp.Syntax
{
    internal class Parser
    {
        private readonly ImmutableArray<SyntaxToken> _tokens;
        private readonly SourceText _text;
        private int _position;

        public Parser(SourceText text)
        {
            List<SyntaxToken> tokens = new List<SyntaxToken>();

            Lexer lexer = new Lexer(text);
            SyntaxToken token;
            do
            {
                token = lexer.Lex();

                if (token.Kind != SyntaxKind.BadToken && token.Kind != SyntaxKind.WhitespaceToken)
                    tokens.Add(token);
            } while (token.Kind != SyntaxKind.EndOfFileToken);

            _tokens = tokens.ToImmutableArray();
            Diagnostics.AddRange(lexer.Diagnostics);
            _text = text;
        }

        #region Properties

        public DiagnosticBag Diagnostics { get; } = new DiagnosticBag();

        #endregion Properties

        #region Private properties

        private SyntaxToken Current => Peek(0);
        private SyntaxToken LookAhead => Peek(1);

        #endregion Private properties

        #region Methods

        public CompilationUnitSyntax ParseCompilationUnit()
        {
            StatementSyntax statement = ParseStatement();
            SyntaxToken endOfFileToken = MatchToken(SyntaxKind.EndOfFileToken);

            return new CompilationUnitSyntax(statement, endOfFileToken);
        }

        #endregion Methods

        #region ParseStatement

        private StatementSyntax ParseStatement(bool requireSemicolon = true)
        {
            switch (Current.Kind)
            {
                case SyntaxKind.OpenBraceToken:
                    return ParseBlockStatement();

                case SyntaxKind.BoolKeyword:
                case SyntaxKind.IntKeyword:
                case SyntaxKind.VarKeyword:
                    return ParseVariableDeclarationStatement(requireSemicolon);

                case SyntaxKind.IfKeyword:
                    return ParseIfStatement();

                case SyntaxKind.WhileKeyword:
                    return ParseWhileStatement();

                case SyntaxKind.ForKeyword:
                    return ParseForStatement();

                default:
                    return ParseExpressionStatement(requireSemicolon);
            }
        }

        private BlockStatementSyntax ParseBlockStatement()
        {
            ImmutableArray<StatementSyntax>.Builder statements = ImmutableArray.CreateBuilder<StatementSyntax>();

            SyntaxToken openBraceToken = MatchToken(SyntaxKind.OpenBraceToken);

            while (Current.Kind != SyntaxKind.CloseBraceToken &&
                   Current.Kind != SyntaxKind.EndOfFileToken)
            {
                SyntaxToken currentToken = Current;

                StatementSyntax statement = ParseStatement();
                statements.Add(statement);

                // If no tokens were consumed by the parse call,
                // we should escape the loop. Parse errors will
                // have already been reported.
                if (currentToken == Current)
                    break;
            }

            SyntaxToken closeBraceToken = MatchToken(SyntaxKind.CloseBraceToken);

            return new BlockStatementSyntax(openBraceToken, statements.ToImmutable(), closeBraceToken);
        }

        private VariableDeclarationStatementSyntax ParseVariableDeclarationStatement(bool requireSemicolon)
        {
            SyntaxToken keyword = NextToken();
            SyntaxToken identifier = MatchToken(SyntaxKind.IdentifierToken);
            SyntaxToken equals = MatchToken(SyntaxKind.EqualsToken);
            ExpressionSyntax initializer = ParseExpression();

            SyntaxToken? semicolonToken = null;
            if (requireSemicolon)
                semicolonToken = MatchToken(SyntaxKind.SemicolonToken);

            return new VariableDeclarationStatementSyntax(keyword, identifier, equals, initializer, semicolonToken);
        }

        private IfStatementSyntax ParseIfStatement()
        {
            SyntaxToken keyword = MatchToken(SyntaxKind.IfKeyword);
            ParenthesizedExpressionSyntax condition = ParseParenthesizedExpression();
            StatementSyntax statement = ParseStatement();

            if (statement.Kind == SyntaxKind.VariableDeclarationStatement)
                Diagnostics.ReportCannotDeclareConditional(statement.Span);

            ElseClauseSyntax? elseClause = ParseElseClause();

            return new IfStatementSyntax(keyword, condition, statement, elseClause);
        }

        private ElseClauseSyntax? ParseElseClause()
        {
            if (Current.Kind != SyntaxKind.ElseKeyword)
                return null;

            SyntaxToken keyword = NextToken();
            StatementSyntax statement = ParseStatement();

            if (statement.Kind == SyntaxKind.VariableDeclarationStatement)
                Diagnostics.ReportCannotDeclareConditional(statement.Span);

            return new ElseClauseSyntax(keyword, statement);
        }

        private WhileStatementSyntax ParseWhileStatement()
        {
            SyntaxToken keyword = MatchToken(SyntaxKind.WhileKeyword);
            ParenthesizedExpressionSyntax condition = ParseParenthesizedExpression();
            StatementSyntax statement = ParseStatement();

            if (statement.Kind == SyntaxKind.VariableDeclarationStatement)
                Diagnostics.ReportCannotDeclareConditional(statement.Span);

            return new WhileStatementSyntax(keyword, condition, statement);
        }

        private ForStatementSyntax ParseForStatement()
        {
            SyntaxToken keyword = MatchToken(SyntaxKind.ForKeyword);

            SyntaxToken leftParenthesis = MatchToken(SyntaxKind.OpenParenthesisToken);

            StatementSyntax initStatement = ParseStatement(requireSemicolon: false);
            SyntaxToken initSemicolon = MatchToken(SyntaxKind.SemicolonToken);
            if (initStatement.Kind != SyntaxKind.VariableDeclarationStatement &&
                (initStatement.Kind != SyntaxKind.ExpressionStatement || ((ExpressionStatementSyntax)initStatement).Expression.Kind != SyntaxKind.AssignmentExpression))
            {
                Diagnostics.ReportDeclarationOrAssignmentOnly(initStatement.Span, initStatement.Kind);
            }

            ExpressionSyntax condition = ParseExpression();
            SyntaxToken conditionSemicolon = MatchToken(SyntaxKind.SemicolonToken);

            ExpressionStatementSyntax updateStatement = ParseExpressionStatement(requireSemicolon: false);

            SyntaxToken rightParenthesis = MatchToken(SyntaxKind.CloseParenthesisToken);

            StatementSyntax statement = ParseStatement();
            if (statement.Kind == SyntaxKind.VariableDeclarationStatement)
                Diagnostics.ReportCannotDeclareConditional(statement.Span);

            return new ForStatementSyntax(
                keyword,
                leftParenthesis,
                initStatement, initSemicolon,
                condition, conditionSemicolon,
                updateStatement,
                rightParenthesis,
                statement);
        }

        private ExpressionStatementSyntax ParseExpressionStatement(bool requireSemicolon)
        {
            ExpressionSyntax expression = ParseExpression();

            SyntaxToken? semicolonToken = null;
            if (requireSemicolon)
                semicolonToken = MatchToken(SyntaxKind.SemicolonToken);

            return new ExpressionStatementSyntax(expression, semicolonToken);
        }

        #endregion ParseStatement

        #region ParseExpression

        private ExpressionSyntax ParseExpression(int parentPrecedence = 0)
        {
            ExpressionSyntax left;
            if (Current.Kind == SyntaxKind.IdentifierToken && SyntaxFacts.GetKindIsAssignmentOperator(LookAhead.Kind))
            {
                left = ParseAssignmentExpression();
            }
            else
            {
                int unaryOperatorPrecedence = Current.Kind.GetUnaryOperatorPrecedence();
                if (unaryOperatorPrecedence != 0 && unaryOperatorPrecedence > parentPrecedence)
                {
                    left = ParseUnaryExpression(unaryOperatorPrecedence);
                }
                else
                {
                    left = ParsePrimaryExpression();
                }
            }

            while (true)
            {
                int precedence = Current.Kind.GetBinaryOperatorPrecedence();
                if (precedence != 0 && precedence > parentPrecedence)
                {
                    SyntaxToken operatorToken = NextToken();
                    ExpressionSyntax right = ParseExpression(precedence);
                    left = new BinaryExpressionSyntax(left, operatorToken, right);
                }
                else break;
            }

            return left;
        }

        private AssignmentExpressionSyntax ParseAssignmentExpression()
        {
            SyntaxToken identifierToken = MatchToken(SyntaxKind.IdentifierToken);
            SyntaxToken operatorToken = NextToken();
            ExpressionSyntax right = ParseExpression();

            return new AssignmentExpressionSyntax(identifierToken, operatorToken, right);
        }

        private UnaryExpressionSyntax ParseUnaryExpression(int unaryOperatorPrecedence)
        {
            SyntaxToken operatorToken = NextToken();

            ExpressionSyntax operand;
            if (operatorToken.Kind == SyntaxKind.MinusMinusToken || operatorToken.Kind == SyntaxKind.PlusPlusToken)
                operand = ParseNameExpression();
            else
                operand = ParseExpression(unaryOperatorPrecedence);

            return new UnaryExpressionSyntax(operatorToken, operand);
        }

        private ExpressionSyntax ParsePrimaryExpression()
        {
            switch (Current.Kind)
            {
                case SyntaxKind.OpenParenthesisToken:
                    return ParseParenthesizedExpression();

                case SyntaxKind.TrueKeyword:
                case SyntaxKind.FalseKeyword:
                    return ParseBooleanLiteral();

                case SyntaxKind.NumberToken:
                    return ParseNumberLiteral();

                case SyntaxKind.IdentifierToken:
                default:
                    return ParseNameExpression();
            }
        }

        private ParenthesizedExpressionSyntax ParseParenthesizedExpression()
        {
            SyntaxToken leftParenthesis = MatchToken(SyntaxKind.OpenParenthesisToken);
            ExpressionSyntax expression = ParseExpression();
            SyntaxToken rightParenthesis = MatchToken(SyntaxKind.CloseParenthesisToken);

            return new ParenthesizedExpressionSyntax(leftParenthesis, expression, rightParenthesis);
        }

        private LiteralExpressionSyntax ParseBooleanLiteral()
        {
            bool isTrue = Current.Kind == SyntaxKind.TrueKeyword;
            SyntaxToken keywordToken = MatchToken(isTrue ? SyntaxKind.TrueKeyword : SyntaxKind.FalseKeyword);

            return new LiteralExpressionSyntax(keywordToken, isTrue);
        }

        private LiteralExpressionSyntax ParseNumberLiteral()
        {
            SyntaxToken numberToken = MatchToken(SyntaxKind.NumberToken);
            if (!int.TryParse(numberToken.Text, out int value))
                Diagnostics.ReportInvalidNumber(numberToken.Span, numberToken.Text, typeof(int));

            return new LiteralExpressionSyntax(numberToken, value);
        }

        private NameExpressionSyntax ParseNameExpression()
        {
            SyntaxToken identifierToken = MatchToken(SyntaxKind.IdentifierToken);
            return new NameExpressionSyntax(identifierToken);
        }

        #endregion ParseExpression

        #region Helper methods

        private SyntaxToken Peek(int offset)
        {
            int index = _position + offset;
            if (index >= _tokens.Length)
                return _tokens[_tokens.Length - 1];

            return _tokens[index];
        }

        private SyntaxToken NextToken()
        {
            SyntaxToken current = Current;
            _position++;

            return current;
        }

        private SyntaxToken MatchToken(SyntaxKind kind)
        {
            if (Current.Kind == kind)
                return NextToken();

            Diagnostics.ReportUnexpectedToken(Current.Span, Current.Kind, kind);
            return new SyntaxToken(kind, Current.Position, null, null);
        }

        #endregion Helper methods
    }
}