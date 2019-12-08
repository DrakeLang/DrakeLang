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
using System.Text;

namespace PHPSharp.Syntax
{
    internal sealed class Lexer
    {
        private readonly SourceText _text;

        private int _position;
        private int _start;
        private SyntaxKind _kind;
        private object? _value;

        public Lexer(SourceText text)
        {
            _text = text;
        }

        #region Public properties

        public DiagnosticBag Diagnostics { get; } = new DiagnosticBag();

        #endregion Public properties

        #region Private properties

        private char Current => Peek(0);

        private char LookAhead => Peek(1);

        #endregion Private properties

        #region Methods

        /// <summary>
        /// Lexes the next token in the source text.
        /// </summary>
        public SyntaxToken Lex()
        {
            _start = _position;
            _kind = SyntaxKind.BadToken;
            _value = null;

            if (TryLookupTokenKind(out SyntaxKind kind))
                _kind = kind;
            else
            {
                switch (Current)
                {
                    case '"':
                        ReadString();
                        break;

                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                    case '.':
                        ReadNumberToken();
                        break;

                    case ' ':
                    case '\t':
                    case '\r':
                    case '\n':
                        ReadWhitespace();
                        break;

                    default:
                        if (char.IsLetter(Current))
                            ReadIdentifierOrKeyword();
                        else if (char.IsWhiteSpace(Current))
                            ReadWhitespace();
                        else
                        {
                            Diagnostics.ReportBadCharacter(_position, Current);
                            Next();
                        }
                        break;
                }
            }

            string? text = SyntaxFacts.GetText(_kind);
            if (text is null)
            {
                int length = _position - _start;
                text = _text.ToString(_start, length);
            }

            return new SyntaxToken(_kind, _start, text, _value);
        }

        private bool TryLookupTokenKind(out SyntaxKind syntaxKind)
        {
            SyntaxKind? result;
            switch (Current)
            {
                case '\0':
                    result = SyntaxKind.EndOfFileToken;
                    break;

                case ';':
                    Next();
                    result = SyntaxKind.SemicolonToken;
                    break;

                case '+':
                    Next();
                    if (Current == '+')
                    {
                        Next();
                        result = SyntaxKind.PlusPlusToken;
                    }
                    else if (Current == '=')
                    {
                        Next();
                        result = SyntaxKind.PlusEqualsToken;
                    }
                    else
                        result = SyntaxKind.PlusToken;
                    break;

                case '-':
                    Next();
                    if (Current == '-')
                    {
                        Next();
                        result = SyntaxKind.MinusMinusToken;
                    }
                    else if (Current == '=')
                    {
                        Next();
                        result = SyntaxKind.MinusEqualsToken;
                    }
                    else
                        result = SyntaxKind.MinusToken;
                    break;

                case '*':
                    Next();
                    if (Current == '=')
                    {
                        Next();
                        result = SyntaxKind.StarEqualsToken;
                    }
                    else
                        result = SyntaxKind.StarToken;
                    break;

                case '/':
                    Next();
                    if (Current == '=')
                    {
                        Next();
                        result = SyntaxKind.SlashEqualsToken;
                    }
                    else if (Current == '/')
                    {
                        do
                        {
                            Next();
                        }
                        while (Current != '\n' && Current != '\r' && Current != '\0');

                        result = SyntaxKind.LineCommentToken;
                    }
                    else
                        result = SyntaxKind.SlashToken;
                    break;

                case '(':
                    Next();
                    result = SyntaxKind.OpenParenthesisToken;
                    break;

                case ')':
                    Next();
                    result = SyntaxKind.CloseParenthesisToken;
                    break;

                case '{':
                    Next();
                    result = SyntaxKind.OpenBraceToken;
                    break;

                case '}':
                    Next();
                    result = SyntaxKind.CloseBraceToken;
                    break;

                case '~':
                    Next();
                    result = SyntaxKind.TildeToken;
                    break;

                case '^':
                    Next();
                    result = SyntaxKind.HatToken;
                    break;

                case '&':
                    Next();
                    if (Current == '&')
                    {
                        Next();
                        result = SyntaxKind.AmpersandAmpersandToken;
                    }
                    else if (Current == '=')
                    {
                        Next();
                        result = SyntaxKind.AmpersandEqualsToken;
                    }
                    else
                        result = SyntaxKind.AmpersandToken;
                    break;

                case '|':
                    Next();
                    if (Current == '|')
                    {
                        Next();
                        result = SyntaxKind.PipePipeToken;
                    }
                    else if (Current == '=')
                    {
                        Next();
                        result = SyntaxKind.PipeEqualsToken;
                    }
                    else
                        result = SyntaxKind.PipeToken;
                    break;

                case '=':
                    Next();
                    if (Current == '=')
                    {
                        Next();
                        result = SyntaxKind.EqualsEqualsToken;
                    }
                    else
                        result = SyntaxKind.EqualsToken;
                    break;

                case '!':
                    Next();
                    if (Current == '=')
                    {
                        Next();
                        result = SyntaxKind.BangEqualsToken;
                    }
                    else
                        result = SyntaxKind.BangToken;
                    break;

                case '<':
                    Next();
                    if (Current == '=')
                    {
                        Next();
                        result = SyntaxKind.LessOrEqualsToken;
                    }
                    else
                        result = SyntaxKind.LessToken;
                    break;

                case '>':
                    Next();
                    if (Current == '=')
                    {
                        Next();
                        result = SyntaxKind.GreaterOrEqualsToken;
                    }
                    else
                        result = SyntaxKind.GreaterToken;
                    break;

                default:
                    syntaxKind = default;
                    return false;
            }

            syntaxKind = result.Value;
            return true;
        }

        #endregion Methods

        #region Private methods

        private char Peek(int offset)
        {
            int index = _position + offset;
            if (index >= _text.Length)
                return '\0';

            return _text[index];
        }

        private void Next()
        {
            _position++;
        }

        private void ReadString()
        {
            // Skip quote start.
            _position++;

            StringBuilder sb = new StringBuilder();

            bool done = false;
            bool escape = false;
            while (!done)
            {
                switch (Current)
                {
                    case '\0':
                    case '\r':
                    case '\n':
                        TextSpan span = new TextSpan(_start, 1);
                        Diagnostics.ReportUnterminatedString(span);
                        done = true;
                        break;

                    case '"':
                        _position++;
                        if (escape)
                        {
                            sb.Append('"');
                            escape = false;
                        }
                        else
                        {
                            done = true;
                        }
                        break;

                    case '\\':
                        _position++;
                        if (escape)
                        {
                            sb.Append('\\');
                            escape = false;
                        }
                        else
                        {
                            escape = true;
                        }

                        break;

                    default:
                        sb.Append(Current);
                        _position++;
                        escape = false;
                        break;
                }
            }

            _kind = SyntaxKind.StringToken;
            _value = sb.ToString();
        }

        private void ReadNumberToken()
        {
            // Keep reading digits as long as their available,
            // as well as a single decimal separator *if* the following char is a digit as well.
            bool isFloat = false;
            while (char.IsDigit(Current) || (!isFloat && Current == '.' && char.IsDigit(LookAhead)))
            {
                Next();
                isFloat |= Peek(-1) == '.';
            }

            if (Current == 'f')
            {
                Next();
                isFloat = true;
            }

            _kind = isFloat ? SyntaxKind.FloatToken : SyntaxKind.IntegerToken;
        }

        private void ReadWhitespace()
        {
            while (char.IsWhiteSpace(Current))
                Next();

            _kind = SyntaxKind.WhitespaceToken;
        }

        private void ReadIdentifierOrKeyword()
        {
            while (char.IsLetter(Current))
                Next();

            int length = _position - _start;
            string word = _text.ToString(_start, length);

            _kind = SyntaxFacts.GetKeywordKind(word);
        }

        #endregion Private methods
    }
}