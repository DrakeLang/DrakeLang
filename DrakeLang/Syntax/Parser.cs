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

using DrakeLang.Text;
using System;
using System.Collections.Immutable;
using static DrakeLang.Symbols.SystemSymbols;

namespace DrakeLang.Syntax
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

        private readonly SourceText _text;
        private readonly ImmutableArray<SyntaxToken> _tokens;
        private int _position;

        public Parser(SourceText text)
        {
            _text = text;
            Diagnostics = new(text);

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
            Diagnostics.AddRange(lexer.Diagnostics);
        }

        #region Properties

        public DiagnosticsBuilder Diagnostics { get; }

        #endregion Properties

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

        #endregion Methods

        #region ParseStatement

        private ImmutableArray<StatementSyntax> ParseStatements(Func<bool>? escapeCondition = null)
        {
            var statements = ImmutableArray.CreateBuilder<StatementSyntax>();
            while (Current.Kind != SyntaxKind.EndOfFileToken &&
                (escapeCondition is null || !escapeCondition()))
            {
                var currentToken = Current;

                var statement = ParseStatement();
                statements.Add(statement);

                // If no tokens were consumed by the parse call,
                // we skip forward to the next statement (after the following semicolon)
                // Parse errors will have already been reported.
                if (currentToken == Current)
                {
                    while (Current.Kind is not SyntaxKind.SemicolonToken and not SyntaxKind.EndOfFileToken)
                        NextToken();

                    if (Current.Kind is SyntaxKind.EndOfFileToken)
                        break;

                    NextToken();
                }
            }

            return statements.ToImmutable();
        }

        private StatementSyntax ParseStatement(bool requireSemicolon = true)
        {
            if (Current.Kind.IsTypeKeyword())
            {
                if (LookAhead.Kind == SyntaxKind.IdentifierToken && Peek(2).Kind == SyntaxKind.OpenParenthesisToken)
                    return ParseMethodDeclarationStatement();
                else if (LookAhead.Kind.IsExplicitTypeKeyword() || LookAhead.Kind == SyntaxKind.IdentifierToken)
                    return ParseVariableDeclarationStatement(requireSemicolon);
                else if (LookAhead.Kind == SyntaxKind.OpenBracketToken && Peek(2).Kind == SyntaxKind.CloseBracketToken && Peek(4).Kind == SyntaxKind.EqualsToken)
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
                SyntaxKind.WithKeyword when Peek(2).Kind == SyntaxKind.EqualsToken => ParseWithMethodAliasStatement(),
                SyntaxKind.WithKeyword => ParseWithNamespaceStatement(),
                SyntaxKind.ContinueKeyword => ParseContinueStatement(),
                SyntaxKind.BreakKeyword => ParseBreakStatement(),

                _ => ParseExpressionStatement(requireSemicolon),
            };
        }

        private BlockStatementSyntax ParseBlockStatement()
        {
            var openBraceToken = MatchToken(SyntaxKind.OpenBraceToken);
            var statements = ParseStatements(() => Current.Kind is SyntaxKind.CloseBraceToken);
            var closeBraceToken = MatchToken(SyntaxKind.CloseBraceToken);

            return new BlockStatementSyntax(openBraceToken, statements, closeBraceToken);
        }

        private NamespaceDeclarationSyntax ParseNamespaceDeclarationStatement()
        {
            var namespaceToken = NextToken();
            var names = ParseSyntaxList(() => MatchToken(SyntaxKind.IdentifierToken), SyntaxKind.DotToken, () => Current.Kind is SyntaxKind.OpenBraceToken);

            if (Current.Kind == SyntaxKind.OpenBraceToken)
            {
                var body = ParseBlockStatement();
                return new BodiedNamespaceDeclarationStatementSyntax(namespaceToken, names, body);
            }
            else
            {
                var semicolon = MatchToken(SyntaxKind.SemicolonToken);
                var statements = ParseStatements(() => Current.Kind is SyntaxKind.NamespaceKeyword or SyntaxKind.CloseBraceToken);
                return new SimpleNamespaceDeclarationStatementSyntax(namespaceToken, names, semicolon, statements);
            }
        }

        private VariableDeclarationStatementSyntax ParseVariableDeclarationStatement(bool requireSemicolon)
        {
            var keyword = Current.Kind.IsImplicitTypeKeyword() ? NextToken() : null;
            var explicitType = (keyword is null || Current.Kind.IsExplicitTypeKeyword()) ? ParseTypeExpression() : null;
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
                Diagnostics.ReportCannotDeclareConditional(statement.Span);

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
                Diagnostics.ReportCannotDeclareConditional(statement.Span);

            return new ElseClauseSyntax(keyword, statement);
        }

        private WhileStatementSyntax ParseWhileStatement()
        {
            var keyword = MatchToken(SyntaxKind.WhileKeyword);
            var condition = ParseParenthesizedExpression();
            var statement = ParseStatement();

            if (statement.Kind == SyntaxKind.VariableDeclarationStatement)
                Diagnostics.ReportCannotDeclareConditional(statement.Span);

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

        private ReturnStatementSyntax ParseReturnStatement()
        {
            var keyword = MatchToken(SyntaxKind.ReturnKeyword);
            ExpressionSyntax? expression = null;
            if (Current.Kind != SyntaxKind.SemicolonToken)
                expression = ParseExpression();

            var semicolon = MatchToken(SyntaxKind.SemicolonToken);

            return new ReturnStatementSyntax(keyword, expression, semicolon);
        }

        private WithAliasStatementSyntax ParseWithMethodAliasStatement()
        {
            var keyword = MatchToken(SyntaxKind.WithKeyword);
            var identifier = MatchToken(SyntaxKind.IdentifierToken);
            var equalsToken = MatchToken(SyntaxKind.EqualsToken);

            var namespaceNames = ParseOptionalNamespacePrefix();
            var methodName = MatchToken(SyntaxKind.IdentifierToken);

            if (Current.Kind == SyntaxKind.OpenBraceToken)
            {
                var body = ParseBlockStatement();
                return new BodiedWithMethodAliasStatementSyntax(keyword, identifier, equalsToken, namespaceNames, methodName, body);
            }
            else
            {
                var semicolon = MatchToken(SyntaxKind.SemicolonToken);
                var statements = ParseStatements(() => Current.Kind is SyntaxKind.CloseBraceToken);
                return new SimpleWithMethodAliasStatementSyntax(keyword, identifier, equalsToken, namespaceNames, methodName, semicolon, statements);
            }
        }

        private WithNamespaceStatementSyntax ParseWithNamespaceStatement()
        {
            var keyword = MatchToken(SyntaxKind.WithKeyword);
            var names = ParseSyntaxList(() => MatchToken(SyntaxKind.IdentifierToken), SyntaxKind.DotToken,
                () => Current.Kind is SyntaxKind.OpenBraceToken);
            if (Current.Kind == SyntaxKind.OpenBraceToken)
            {
                var body = ParseBlockStatement();
                return new BodiedWithNamespaceStatementSyntax(keyword, names, body);
            }
            else
            {
                var semicolon = MatchToken(SyntaxKind.SemicolonToken);
                var statements = ParseStatements(() => Current.Kind is SyntaxKind.CloseBraceToken);
                return new SimpleWithNamespaceStatementSyntax(keyword, names, semicolon, statements);
            }
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

        private MethodDeclarationSyntax ParseMethodDeclarationStatement()
        {
            var defKeyword = Current.Kind.IsTypeKeyword() ? NextToken() : MatchToken(SyntaxKind.DefKeyword);
            var identifier = MatchToken(SyntaxKind.IdentifierToken);
            var leftParenthesis = MatchToken(SyntaxKind.OpenParenthesisToken);
            var parameters = ParseSyntaxList(ParseParameter, SyntaxKind.CommaToken);
            var rightParenthesis = MatchToken(SyntaxKind.CloseParenthesisToken);
            var declaration = Current.Kind == SyntaxKind.EqualsGreaterToken ? (BodyStatementSyntax)ParseExpressionBody() : ParseBlockBody();

            return new MethodDeclarationSyntax(defKeyword, identifier, leftParenthesis, parameters, rightParenthesis, declaration);
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
            var statement = ParseExpressionStatement(requireSemicolon: true);

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

            var semicolon = requireSemicolon ? MatchToken(SyntaxKind.SemicolonToken) : null;
            return new ExpressionStatementSyntax(expression, semicolon);
        }

        #endregion ParseStatement

        #region ParseExpression

        private static readonly ImmutableHashSet<SyntaxKind> _listTerminators = ImmutableHashSet.Create(new[] {
            SyntaxKind.EndOfFileToken,

            SyntaxKind.CloseParenthesisToken,
            SyntaxKind.CloseBraceToken,
            SyntaxKind.CloseBracketToken,

            SyntaxKind.SemicolonToken,
        });

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

            while (Current.Kind == SyntaxKind.OpenBracketToken)
                left = ParseIndexerExpression(left);

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
                SyntaxKind.CharToken => ParseCharLiteral(),

                SyntaxKind.IdentifierToken when LookAhead.Kind is
                    SyntaxKind.OpenParenthesisToken or SyntaxKind.DotToken
                    => ParseCallExpression(),

                _ when Current.Kind is SyntaxKind.OpenBracketToken ||
                    (Current.Kind.IsExplicitTypeKeyword() && LookAhead.Kind is SyntaxKind.OpenBracketToken)
                    => ParseArrayInitializationExpression(),

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

        private TypeExpressionSyntax ParseTypeExpression(bool isArrayDeclaration = false)
        {
            if (!Current.Kind.IsExplicitTypeKeyword())
            {
                Diagnostics.ReportExplicitTypeExpected(Current.Span, Current.Kind);
            }

            var typeToken = NextToken();
            return TryParseArrayTypeExpression(typeToken, isArrayDeclaration);
        }

        private TypeExpressionSyntax TryParseArrayTypeExpression(SyntaxToken typeToken, bool isArrayDeclaration = false)
        {
            var bracketBuilder = ImmutableArray.CreateBuilder<SyntaxToken>();
            while (true)
            {
                if (Current.Kind != SyntaxKind.OpenBracketToken ||
                    LookAhead.Kind != SyntaxKind.CloseBracketToken ||
                    (isArrayDeclaration && Peek(2).Kind != SyntaxKind.OpenBracketToken))
                {
                    if (bracketBuilder.Count == 0)
                        return TypeExpressionSyntax.Create(typeToken);
                    else
                        return TypeExpressionSyntax.CreateArray(typeToken, bracketBuilder.ToImmutable());
                }

                var openBracket = NextToken();
                var closeBracket = NextToken();

                bracketBuilder.Add(openBracket);
                bracketBuilder.Add(closeBracket);
            }
        }

        private LiteralExpressionSyntax ParseBooleanLiteral(bool isTrue)
        {
            var keywordToken = MatchToken(isTrue ? SyntaxKind.TrueKeyword : SyntaxKind.FalseKeyword);

            return new LiteralExpressionSyntax(keywordToken, isTrue);
        }

        private LiteralExpressionSyntax ParseIntegerLiteral()
        {
            var integerToken = MatchToken(SyntaxKind.IntegerToken);
            if (!int.TryParse(integerToken.TokenText, out int value))
                Diagnostics.ReportInvalidNumberValue(integerToken.Span, integerToken.TokenText, Types.Int);

            return new LiteralExpressionSyntax(integerToken, value);
        }

        private LiteralExpressionSyntax ParseFloatLiteral()
        {
            var floatToken = MatchToken(SyntaxKind.FloatToken);

            // Remove eventual 'f' character.
            string floatString = floatToken.TokenText.Replace("f", "", StringComparison.InvariantCulture);
            if (!double.TryParse(floatString, out double value))
                Diagnostics.ReportInvalidNumberValue(floatToken.Span, floatToken.TokenText, Types.Float);

            return new LiteralExpressionSyntax(floatToken, value);
        }

        private LiteralExpressionSyntax ParseStringLiteral()
        {
            var stringToken = MatchToken(SyntaxKind.StringToken);
            return new LiteralExpressionSyntax(stringToken);
        }

        private LiteralExpressionSyntax ParseCharLiteral()
        {
            var charToken = MatchToken(SyntaxKind.CharToken);
            return new LiteralExpressionSyntax(charToken);
        }

        private CallExpressionSyntax ParseCallExpression()
        {
            var namespaceNames = ParseOptionalNamespacePrefix();
            var identifierToken = MatchToken(SyntaxKind.IdentifierToken);
            var leftParenthesis = MatchToken(SyntaxKind.OpenParenthesisToken);
            var arguments = ParseSyntaxList(ParseArgument, SyntaxKind.CommaToken);
            var rightParenthesis = MatchToken(SyntaxKind.CloseParenthesisToken);

            return new CallExpressionSyntax(namespaceNames, identifierToken, leftParenthesis, arguments, rightParenthesis);

            SyntaxNode ParseArgument()
            {
                return Current.Kind == SyntaxKind.UnderscoreToken ? NextToken() : ParseExpression();
            }
        }

        private IndexerExpressionSyntax ParseIndexerExpression(ExpressionSyntax operand)
        {
            var openBracket = MatchToken(SyntaxKind.OpenBracketToken);
            var parameters = ParseSyntaxList(() => ParseExpression(), SyntaxKind.CommaToken);
            var closeBracket = MatchToken(SyntaxKind.CloseBracketToken);

            return new(operand, openBracket, parameters, closeBracket);
        }

        private ArrayInitializationExpressionSyntax ParseArrayInitializationExpression()
        {
            var typeKeyword = Current.Kind.IsExplicitTypeKeyword() ? ParseTypeExpression(isArrayDeclaration: true) : null;
            var openBracket = MatchToken(SyntaxKind.OpenBracketToken);

            if (Current.Kind == SyntaxKind.CloseBracketToken)
            {
                // Bodied initializer

                var closeBracket = NextToken();

                var openBrace = MatchToken(SyntaxKind.OpenBraceToken);
                var initializer = ParseSyntaxList(() => ParseExpression(), SyntaxKind.CommaToken, allowTrailingSeparator: true);
                var closeBrace = MatchToken(SyntaxKind.CloseBraceToken);

                return new BodiedArrayInitializationExpressionSyntax(typeKeyword, openBracket, closeBracket, openBrace, initializer, closeBrace);
            }

            var sizeOrFirstElementExpression = ParseExpression();

            if (LookAhead.Kind == SyntaxKind.EqualsGreaterToken)
            {
                // Dynamically Sized initializer

                var sizeExpression = sizeOrFirstElementExpression;
                var closeBracket = MatchToken(SyntaxKind.CloseBracketToken);
                var lamdaToken = MatchToken(SyntaxKind.EqualsGreaterToken);
                var initExpression = ParseExpression();

                return new DynamicallySizedArrayInitializationExpressionSyntax(typeKeyword, openBracket, sizeExpression, closeBracket, lamdaToken, initExpression);
            }
            else
            {
                // Simple array initializer

                var firstElementExpression = sizeOrFirstElementExpression;

                // We already parsed the first expression,
                // So we give the list parser the first element (and optionally the first separator).
                SeparatedSyntaxList<ExpressionSyntax> initializer;
                if (Current.Kind == SyntaxKind.CommaToken)
                {
                    var builder = ImmutableArray.CreateBuilder<SyntaxNode>();
                    builder.Add(firstElementExpression);
                    builder.Add(NextToken());
                    initializer = ParseSyntaxList(() => ParseExpression(), SyntaxKind.CommaToken, builder: builder);
                }
                else
                {
                    initializer = new(ImmutableArray.Create<SyntaxNode>(firstElementExpression));
                }
                var closeBracket = MatchToken(SyntaxKind.CloseBracketToken);

                return new SimpleArrayInitializerExpressionSyntax(typeKeyword, openBracket, initializer, closeBracket);
            }
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

            Diagnostics.ReportUnexpectedToken(Current.Span, Current.Kind, kind);
            return new SyntaxToken(_text, kind, Current.Position, "", null);
        }

        private SeparatedSyntaxList<T> ParseSyntaxList<T>(Func<T> valueParser,
                                                          SyntaxKind seperator,
                                                          Func<bool>? escapeConditions = null,
                                                          ImmutableArray<SyntaxNode>.Builder? builder = null,
                                                          bool allowTrailingSeparator = false)
            where T : SyntaxNode
        {
            builder ??= ImmutableArray.CreateBuilder<SyntaxNode>();

            var extendedEscapeCondition = escapeConditions;
            escapeConditions = () => _listTerminators.Contains(Current.Kind) || (extendedEscapeCondition?.Invoke() ?? false);

            if (builder.Count == 0 && escapeConditions())
                return new(ImmutableArray<SyntaxNode>.Empty);

            do
            {
                var currentToken = Current;

                var value = valueParser();
                builder.Add(value);

                // Don't expect separator after final argument.
                if (escapeConditions())
                    break;

                var dot = MatchToken(seperator);
                builder.Add(dot);

                // If no tokens were consumed by the parse call,
                // we should escape the loop. Parse errors will
                // have already been reported.
                if (currentToken == Current)
                    break;

                if (allowTrailingSeparator && escapeConditions())
                    break;
            } while (true);

            return new(builder.ToImmutable());
        }

        private SeparatedSyntaxList<SyntaxToken> ParseNamespacePrefix()
        {
            return ParseSyntaxList(() => MatchToken(SyntaxKind.IdentifierToken), SyntaxKind.DotToken,
                () => LookAhead.Kind is not SyntaxKind.IdentifierToken and not SyntaxKind.DotToken,
                allowTrailingSeparator: true);
        }

        private SeparatedSyntaxList<SyntaxToken>? ParseOptionalNamespacePrefix()
        {
            if (LookAhead.Kind == SyntaxKind.DotToken)
                return ParseNamespacePrefix();

            return null;
        }

        #endregion Helper methods
    }
}