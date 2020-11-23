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
using System.Collections.Immutable;
using System.Globalization;
using VSharp.Symbols;
using VSharp.Text;
using static VSharp.Symbols.SystemSymbols;

namespace VSharp.Syntax
{
    internal class Parser
    {
        private static readonly ImmutableHashSet<SyntaxKind> _ignoredTokens = new[]
        {
            SyntaxKind.BadToken,
            SyntaxKind.WhitespaceToken,
            SyntaxKind.LineCommentToken,
            SyntaxKind.MultiLineCommentToken,
        }.ToImmutableHashSet();

        private static readonly ImmutableHashSet<SyntaxKind> _closeBraceEscapeCondition = ImmutableHashSet.Create(SyntaxKind.CloseBraceToken);
        private static readonly ImmutableHashSet<SyntaxKind> _namespaceKeywordEscapeCondition = ImmutableHashSet.Create(SyntaxKind.CloseBraceToken);

        private readonly DiagnosticBag _diagnostics = new DiagnosticBag();

        private readonly ImmutableArray<SyntaxToken> _tokens;
        private int _position;

        public Parser(SourceText text)
        {
            var tokens = ImmutableArray.CreateBuilder<SyntaxToken>();

            var lexer = new Lexer(text);
            SyntaxToken token;
            do
            {
                token = lexer.Lex();

                if (!_ignoredTokens.Contains(token.Kind))
                    tokens.Add(token);
            } while (token.Kind != SyntaxKind.EndOfFileToken);

            _tokens = tokens.ToImmutable();
            _diagnostics.AddRange(lexer.GetDiagnostics());
        }

        #region Private properties

        private SyntaxToken Current => Peek(0);
        private SyntaxToken LookAhead => Peek(1);

        #endregion Private properties

        #region Methods

        public CompilationUnitSyntax ParseCompilationUnit()
        {
            var statements = ParseStatements();
            var endOfFileToken = MatchToken(SyntaxKind.EndOfFileToken);

            return new CompilationUnitSyntax(statements, endOfFileToken);
        }

        public ImmutableArray<Diagnostic> GetDiagnostics() => _diagnostics.ToImmutableArray();

        #endregion Methods

        #region ParseStatement

        private ImmutableArray<StatementSyntax> ParseStatements(ISet<SyntaxKind>? optionalEscapeConditions = null)
        {
            var statements = ImmutableArray.CreateBuilder<StatementSyntax>();
            while (Current.Kind != SyntaxKind.EndOfFileToken &&
                (optionalEscapeConditions is null || !optionalEscapeConditions.Contains(Current.Kind)))
            {
                var currentToken = Current;

                var statement = ParseStatement();
                statements.Add(statement);

                // If no tokens were consumed by the parse call,
                // we should escape the loop. Parse errors will
                // have already been reported.
                if (currentToken == Current)
                    break;
            }

            return statements.ToImmutable();
        }

        private StatementSyntax ParseStatement(bool requireSemicolon = true)
        {
            if (Current.Kind.IsTypeKeyword())
            {
                if (LookAhead.Kind == SyntaxKind.IdentifierToken && Peek(2).Kind == SyntaxKind.OpenParenthesisToken)
                    return ParseMethodDeclarationStatement();
                else
                    return ParseVariableDeclarationStatement(requireSemicolon);
            }

            return Current.Kind switch
            {
                SyntaxKind.OpenBraceToken => ParseBlockStatement(),
                SyntaxKind.NamespaceKeyword => ParseNamespaceDeclarationStatement(),
                SyntaxKind.DefKeyword => ParseMethodDeclarationStatement(),
                SyntaxKind.IfKeyword => ParseIfStatement(),
                SyntaxKind.WhileKeyword => ParseWhileStatement(),
                SyntaxKind.ForKeyword => ParseForStatement(),
                SyntaxKind.GoToKeyword => ParseGoToStatement(),
                SyntaxKind.IdentifierToken when LookAhead.Kind == SyntaxKind.ColonToken => ParseLabelDeclarationStatement(),
                SyntaxKind.ReturnKeyword => ParseReturnStatement(),
                SyntaxKind.ContinueKeyword => ParseContinueStatement(),
                SyntaxKind.BreakKeyword => ParseBreakStatement(),

                _ => ParseExpressionStatement(requireSemicolon),
            };
        }

        private BlockStatementSyntax ParseBlockStatement()
        {
            var openBraceToken = MatchToken(SyntaxKind.OpenBraceToken);
            var statements = ParseStatements(_closeBraceEscapeCondition);
            var closeBraceToken = MatchToken(SyntaxKind.CloseBraceToken);

            return new BlockStatementSyntax(openBraceToken, statements, closeBraceToken);
        }

        private static readonly ImmutableHashSet<SyntaxKind> _namespaceNameEscapeConditions = ImmutableHashSet.Create(SyntaxKind.OpenBraceToken, SyntaxKind.SemicolonToken);

        private NamespaceDeclarationStatementSyntax ParseNamespaceDeclarationStatement()
        {
            var namespaceToken = NextToken();
            var names = ParseSyntaxList(() => MatchToken(SyntaxKind.IdentifierToken), SyntaxKind.DotToken, _namespaceNameEscapeConditions);

            if (Current.Kind == SyntaxKind.OpenBraceToken)
            {
                var body = ParseBlockStatement();
                return new BodiedNamespaceDeclarationStatementSyntax(namespaceToken, names, body);
            }
            else
            {
                var semicolon = MatchToken(SyntaxKind.SemicolonToken);
                var statements = ParseStatements(_namespaceKeywordEscapeCondition);
                return new SimpleNamespaceDeclarationStatementSyntax(namespaceToken, names, semicolon, statements);
            }
        }

        private VariableDeclarationStatementSyntax ParseVariableDeclarationStatement(bool requireSemicolon)
        {
            var keyword = NextToken();
            var explicitType = Current.Kind.IsExplicitTypeKeyword() ? NextToken() : null;
            var identifier = MatchToken(SyntaxKind.IdentifierToken);
            var equals = MatchToken(SyntaxKind.EqualsToken);
            var initializer = ParseExpression();
            var semicolonToken = requireSemicolon ? MatchToken(SyntaxKind.SemicolonToken) : null;

            return new VariableDeclarationStatementSyntax(keyword, explicitType, identifier, equals, initializer, semicolonToken);
        }

        private IfStatementSyntax ParseIfStatement()
        {
            var keyword = MatchToken(SyntaxKind.IfKeyword);
            var condition = ParseParenthesizedExpression();
            var statement = ParseStatement();

            if (statement.Kind == SyntaxKind.VariableDeclarationStatement)
                _diagnostics.ReportCannotDeclareConditional(statement.Span);

            var elseClause = ParseElseClause();

            return new IfStatementSyntax(keyword, condition, statement, elseClause);
        }

        private ElseClauseSyntax? ParseElseClause()
        {
            if (Current.Kind != SyntaxKind.ElseKeyword)
                return null;

            var keyword = NextToken();
            var statement = ParseStatement();

            if (statement.Kind == SyntaxKind.VariableDeclarationStatement)
                _diagnostics.ReportCannotDeclareConditional(statement.Span);

            return new ElseClauseSyntax(keyword, statement);
        }

        private WhileStatementSyntax ParseWhileStatement()
        {
            var keyword = MatchToken(SyntaxKind.WhileKeyword);
            var condition = ParseParenthesizedExpression();
            var statement = ParseStatement();

            if (statement.Kind == SyntaxKind.VariableDeclarationStatement)
                _diagnostics.ReportCannotDeclareConditional(statement.Span);

            return new WhileStatementSyntax(keyword, condition, statement);
        }

        private ForStatementSyntax ParseForStatement()
        {
            var keyword = MatchToken(SyntaxKind.ForKeyword);

            var leftParenthesis = MatchToken(SyntaxKind.OpenParenthesisToken);

            var initStatement = Current.Kind == SyntaxKind.SemicolonToken ? null : ParseStatement(requireSemicolon: false);
            var initSemicolon = MatchToken(SyntaxKind.SemicolonToken);

            var condition = Current.Kind == SyntaxKind.SemicolonToken ? null : ParseExpression();
            var conditionSemicolon = MatchToken(SyntaxKind.SemicolonToken);

            var updateStatement = Current.Kind == SyntaxKind.CloseParenthesisToken ? null : ParseStatement(requireSemicolon: false);

            var rightParenthesis = MatchToken(SyntaxKind.CloseParenthesisToken);

            var statement = ParseStatement();
            if (statement.Kind == SyntaxKind.VariableDeclarationStatement)
                _diagnostics.ReportCannotDeclareConditional(statement.Span);

            return new ForStatementSyntax(
                keyword,
                leftParenthesis,
                initStatement, initSemicolon,
                condition, conditionSemicolon,
                updateStatement,
                rightParenthesis,
                statement);
        }

        private ReturnStatementSyntax ParseReturnStatement()
        {
            var keyword = MatchToken(SyntaxKind.ReturnKeyword);
            ExpressionSyntax? expression = null;
            if (Current.Kind != SyntaxKind.SemicolonToken)
                expression = ParseExpression();

            var semicolon = MatchToken(SyntaxKind.SemicolonToken);

            return new ReturnStatementSyntax(keyword, expression, semicolon);
        }

        private ContinueStatementSyntax ParseContinueStatement()
        {
            var keyword = MatchToken(SyntaxKind.ContinueKeyword);
            LiteralExpressionSyntax? layerExpression = null;
            if (Current.Kind == SyntaxKind.IntegerToken)
                layerExpression = ParseIntegerLiteral();

            var semicolon = MatchToken(SyntaxKind.SemicolonToken);

            return new ContinueStatementSyntax(keyword, layerExpression, semicolon);
        }

        private BreakStatementSyntax ParseBreakStatement()
        {
            var keyword = MatchToken(SyntaxKind.BreakKeyword);
            LiteralExpressionSyntax? layerExpression = null;
            if (Current.Kind == SyntaxKind.IntegerToken)
                layerExpression = ParseIntegerLiteral();

            var semicolon = MatchToken(SyntaxKind.SemicolonToken);

            return new BreakStatementSyntax(keyword, layerExpression, semicolon);
        }

        private GoToStatementSyntax ParseGoToStatement()
        {
            var keyword = MatchToken(SyntaxKind.GoToKeyword);
            var label = MatchToken(SyntaxKind.IdentifierToken);
            var semicolon = MatchToken(SyntaxKind.SemicolonToken);

            return new GoToStatementSyntax(keyword, label, semicolon);
        }

        private LabelStatementSyntax ParseLabelDeclarationStatement()
        {
            var identifier = MatchToken(SyntaxKind.IdentifierToken);
            var colonToken = MatchToken(SyntaxKind.ColonToken);

            return new LabelStatementSyntax(identifier, colonToken);
        }

        private static readonly ImmutableHashSet<SyntaxKind> _parameterListEscapeCondition = ImmutableHashSet.Create(SyntaxKind.CloseParenthesisToken);

        private MethodDeclarationStatementSyntax ParseMethodDeclarationStatement()
        {
            var defKeyword = Current.Kind.IsTypeKeyword() ? NextToken() : MatchToken(SyntaxKind.DefKeyword);
            var identifier = MatchToken(SyntaxKind.IdentifierToken);
            var leftParenthesis = MatchToken(SyntaxKind.OpenParenthesisToken);
            var parameters = ParseSyntaxList(ParseParameter, SyntaxKind.CommaToken, _parameterListEscapeCondition);
            var rightParenthesis = MatchToken(SyntaxKind.CloseParenthesisToken);
            var declaration = Current.Kind == SyntaxKind.EqualsGreaterToken ? (BodyStatementSyntax)ParseExpressionBody() : ParseBlockBody();

            return new MethodDeclarationStatementSyntax(defKeyword, identifier, leftParenthesis, parameters, rightParenthesis, declaration);
        }

        private ParameterSyntax ParseParameter()
        {
            var type = ParseTypeExpression();
            var identifier = MatchToken(SyntaxKind.IdentifierToken);

            return new ParameterSyntax(type, identifier);
        }

        private ExpressionBodyStatementSyntax ParseExpressionBody()
        {
            var lambdaOperator = MatchToken(SyntaxKind.EqualsGreaterToken);
            var statement = ParseStatement();

            return new ExpressionBodyStatementSyntax(lambdaOperator, statement);
        }

        private BlockBodyStatementSyntax ParseBlockBody()
        {
            var blockStatement = ParseBlockStatement();
            return new BlockBodyStatementSyntax(blockStatement.OpenBraceToken, blockStatement.Statements, blockStatement.CloseBraceToken);
        }

        private ExpressionStatementSyntax ParseExpressionStatement(bool requireSemicolon)
        {
            var expression = ParseExpression();

            var semicolonToken = requireSemicolon ? MatchToken(SyntaxKind.SemicolonToken) : null;
            return new ExpressionStatementSyntax(expression, semicolonToken);
        }

        #endregion ParseStatement

        #region ParseExpression

        private ExpressionSyntax ParseExpression(int parentPrecedence = 0)
        {
            ExpressionSyntax left;
            if (Current.Kind == SyntaxKind.IdentifierToken && LookAhead.Kind.IsAssignmentOperator())
            {
                left = ParseAssignmentExpression();
            }
            else if (CanParseSuffixUnaryExpression())
            {
                left = ParsePostIncrementOrDecrement();
            }
            else if (CanParsePrefixUnaryExpression(parentPrecedence, out int unaryOperatorPrecedence))
            {
                left = ParsePreUnaryExpression(unaryOperatorPrecedence);
            }
            else
            {
                left = ParsePrimaryExpression();
            }

            while (true)
            {
                int precedence = Current.Kind.GetBinaryOperatorPrecedence();
                if (precedence == 0 || precedence <= parentPrecedence)
                    break;

                SyntaxToken operatorToken = NextToken();
                ExpressionSyntax right = ParseExpression(precedence);
                left = new BinaryExpressionSyntax(left, operatorToken, right);
            }

            return left;
        }

        private bool CanParseSuffixUnaryExpression()
        {
            if (Current.Kind != SyntaxKind.IdentifierToken)
                return false;

            if (!LookAhead.Kind.IsUnaryOperator())
                return false;

            return LookAhead.Kind == SyntaxKind.PlusPlusToken || LookAhead.Kind == SyntaxKind.MinusMinusToken;
        }

        private bool CanParsePrefixUnaryExpression(int parentPrecedence, out int unaryOperatorPrecedence)
        {
            unaryOperatorPrecedence = Current.Kind.GetUnaryOperatorPrecedence();
            return unaryOperatorPrecedence != 0 && unaryOperatorPrecedence > parentPrecedence;
        }

        private AssignmentExpressionSyntax ParseAssignmentExpression()
        {
            var identifierToken = MatchToken(SyntaxKind.IdentifierToken);
            var operatorToken = NextToken();
            var right = ParseExpression();

            return new AssignmentExpressionSyntax(identifierToken, operatorToken, right);
        }

        private UnaryExpressionSyntax ParsePostIncrementOrDecrement()
        {
            var identifierToken = ParseNameExpression();
            var operatorToken = NextToken();

            return new UnaryExpressionSyntax(operatorToken, identifierToken, UnaryType.Post);
        }

        private UnaryExpressionSyntax ParsePreUnaryExpression(int unaryOperatorPrecedence)
        {
            var operatorToken = NextToken();

            ExpressionSyntax operand;
            if (operatorToken.Kind == SyntaxKind.MinusMinusToken || operatorToken.Kind == SyntaxKind.PlusPlusToken)
                operand = ParseNameExpression();
            else
                operand = ParseExpression(unaryOperatorPrecedence);

            return new UnaryExpressionSyntax(operatorToken, operand, UnaryType.Pre);
        }

        private ExpressionSyntax ParsePrimaryExpression()
        {
            return Current.Kind switch
            {
                SyntaxKind.OpenParenthesisToken when LookAhead.Kind.IsTypeKeyword() => ParseExplicitCastExpression(),
                SyntaxKind.OpenParenthesisToken => ParseParenthesizedExpression(),

                SyntaxKind.TypeofKeyword => ParseTypeofExpression(),
                SyntaxKind.NameofKeyword => ParseNameofExpression(),

                SyntaxKind.TrueKeyword => ParseBooleanLiteral(isTrue: true),
                SyntaxKind.FalseKeyword => ParseBooleanLiteral(isTrue: false),

                SyntaxKind.IntegerToken => ParseIntegerLiteral(),
                SyntaxKind.FloatToken => ParseFloatLiteral(),
                SyntaxKind.StringToken => ParseStringLiteral(),

                SyntaxKind.IdentifierToken when LookAhead.Kind == SyntaxKind.OpenParenthesisToken => ParseCallExpression(),
                _ => ParseNameExpression(),
            };
        }

        private ExplicitCastExpressionSyntax ParseExplicitCastExpression()
        {
            var leftParenthesis = MatchToken(SyntaxKind.OpenParenthesisToken);
            var typeExpression = ParseTypeExpression();
            var rightParenthesis = MatchToken(SyntaxKind.CloseParenthesisToken);
            var expression = ParseExpression();

            return new ExplicitCastExpressionSyntax(leftParenthesis, typeExpression, rightParenthesis, expression);
        }

        private ParenthesizedExpressionSyntax ParseParenthesizedExpression()
        {
            var leftParenthesis = MatchToken(SyntaxKind.OpenParenthesisToken);
            var expression = ParseExpression();
            var rightParenthesis = MatchToken(SyntaxKind.CloseParenthesisToken);

            return new ParenthesizedExpressionSyntax(leftParenthesis, expression, rightParenthesis);
        }

        private TypeofExpressionSyntax ParseTypeofExpression()
        {
            var typeofKeyword = MatchToken(SyntaxKind.TypeofKeyword);
            var leftParenthesis = MatchToken(SyntaxKind.OpenParenthesisToken);
            var typeLiteral = ParseTypeExpression();
            var rightParenthesis = MatchToken(SyntaxKind.CloseParenthesisToken);

            return new TypeofExpressionSyntax(typeofKeyword, leftParenthesis, typeLiteral, rightParenthesis);
        }

        private NameofExpressionSyntax ParseNameofExpression()
        {
            var nameofKeyword = MatchToken(SyntaxKind.NameofKeyword);
            var leftParenthesis = MatchToken(SyntaxKind.OpenParenthesisToken);
            var identifierToken = MatchToken(SyntaxKind.IdentifierToken);
            var rightParenthesis = MatchToken(SyntaxKind.CloseParenthesisToken);

            return new NameofExpressionSyntax(nameofKeyword, leftParenthesis, identifierToken, rightParenthesis);
        }

        private TypeExpressionSyntax ParseTypeExpression()
        {
            if (!Current.Kind.IsExplicitTypeKeyword())
            {
                _diagnostics.ReportExplicitTypeExpected(Current.Span, Current.Kind);
            }

            var typeToken = NextToken();
            return new TypeExpressionSyntax(typeToken);
        }

        private LiteralExpressionSyntax ParseBooleanLiteral(bool isTrue)
        {
            var keywordToken = MatchToken(isTrue ? SyntaxKind.TrueKeyword : SyntaxKind.FalseKeyword);

            return new LiteralExpressionSyntax(keywordToken, isTrue);
        }

        private LiteralExpressionSyntax ParseIntegerLiteral()
        {
            var integerToken = MatchToken(SyntaxKind.IntegerToken);
            if (!int.TryParse(integerToken.Text, out int value))
                _diagnostics.ReportInvalidValue(integerToken.Span, integerToken.Text, Types.Int);

            return new LiteralExpressionSyntax(integerToken, value);
        }

        private LiteralExpressionSyntax ParseFloatLiteral()
        {
            var floatToken = MatchToken(SyntaxKind.FloatToken);

            // Remove eventual 'f' character.
            string? floatString = floatToken.Text?.Replace("f", "", ignoreCase: false, CultureInfo.InvariantCulture);
            if (!double.TryParse(floatString, out double value))
                _diagnostics.ReportInvalidValue(floatToken.Span, floatToken.Text, Types.Float);

            return new LiteralExpressionSyntax(floatToken, value);
        }

        private LiteralExpressionSyntax ParseStringLiteral()
        {
            var stringToken = MatchToken(SyntaxKind.StringToken);
            return new LiteralExpressionSyntax(stringToken);
        }

        private CallExpressionSyntax ParseCallExpression()
        {
            var identifierToken = MatchToken(SyntaxKind.IdentifierToken);
            var leftParenthesis = MatchToken(SyntaxKind.OpenParenthesisToken);
            var arguments = ParseArguments();
            var rightParenthesis = MatchToken(SyntaxKind.CloseParenthesisToken);

            return new CallExpressionSyntax(identifierToken, leftParenthesis, arguments, rightParenthesis);
        }

        private SeparatedSyntaxList<SyntaxNode> ParseArguments()
        {
            var builder = ImmutableArray.CreateBuilder<SyntaxNode>();

            while (Current.Kind != SyntaxKind.CloseParenthesisToken &&
                   Current.Kind != SyntaxKind.EndOfFileToken)
            {
                if (Current.Kind == SyntaxKind.UnderscoreToken)
                {
                    var pipeArgument = NextToken();
                    builder.Add(pipeArgument);
                }
                else
                {
                    var currentToken = Current;

                    var expression = ParseExpression();
                    builder.Add(expression);

                    // If no tokens were consumed by the parse call,
                    // we should escape the loop. Parse errors will
                    // have already been reported.
                    if (currentToken == Current)
                        break;
                }

                // Don't expect comma after final argument.
                if (Current.Kind != SyntaxKind.CloseParenthesisToken &&
                    Current.Kind != SyntaxKind.EndOfFileToken)
                {
                    var comma = MatchToken(SyntaxKind.CommaToken);
                    builder.Add(comma);
                }
            }

            return new SeparatedSyntaxList<SyntaxNode>(builder.ToImmutable());
        }

        private NameExpressionSyntax ParseNameExpression()
        {
            var identifierToken = MatchToken(SyntaxKind.IdentifierToken);
            return new NameExpressionSyntax(identifierToken);
        }

        #endregion ParseExpression

        #region Helper methods

        private SyntaxToken Peek(int offset)
        {
            int index = _position + offset;
            if (index >= _tokens.Length)
                return _tokens[^1];

            return _tokens[index];
        }

        private SyntaxToken NextToken()
        {
            var current = Current;
            _position++;

            return current;
        }

        private SyntaxToken MatchToken(SyntaxKind kind)
        {
            if (Current.Kind == kind)
                return NextToken();

            _diagnostics.ReportUnexpectedToken(Current.Span, Current.Kind, kind);
            return new SyntaxToken(kind, Current.Position, null, null);
        }

        private SeparatedSyntaxList<T> ParseSyntaxList<T>(Func<T> valueParser, SyntaxKind seperator, ISet<SyntaxKind> escapeConditions)
            where T : SyntaxNode
        {
            var builder = ImmutableArray.CreateBuilder<SyntaxNode>();

            while (Current.Kind != SyntaxKind.EndOfFileToken &&
                   !escapeConditions.Contains(Current.Kind))
            {
                var currentToken = Current;

                var value = valueParser();
                builder.Add(value);

                // Don't expect comma after final argument.
                if (Current.Kind != SyntaxKind.EndOfFileToken &&
                   !escapeConditions.Contains(Current.Kind))
                {
                    var dot = MatchToken(seperator);
                    builder.Add(dot);
                }

                // If no tokens were consumed by the parse call,
                // we should escape the loop. Parse errors will
                // have already been reported.
                if (currentToken == Current)
                    break;
            }

            return new SeparatedSyntaxList<T>(builder.ToImmutable());
        }

        #endregion Helper methods
    }
}