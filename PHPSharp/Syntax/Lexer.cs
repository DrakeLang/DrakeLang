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
// along with this program.  If not, see https://www.gnu.org/licenses/.
//------------------------------------------------------------------------------

namespace PHPSharp.Syntax
{
    internal class Lexer
    {
        private readonly string _text;
        private int _position;

        public Lexer(string text)
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

        public SyntaxToken Lex()
        {
            int start = _position;

            if (char.IsWhiteSpace(Current))
            {
                while (char.IsWhiteSpace(Current))
                    Next();

                int length = _position - start;
                string whitespace = _text.Substring(start, length);
                return new SyntaxToken(SyntaxKind.WhitespaceToken, start, whitespace, null);
            }

            if (_position >= _text.Length)
                return new SyntaxToken(SyntaxKind.EndOfFileToken, _position, "\0", null);

            if (char.IsDigit(Current))
            {
                while (char.IsDigit(Current))
                    Next();

                int length = _position - start;
                string numberStr = _text.Substring(start, length);
                if (!int.TryParse(numberStr, out int value))
                    Diagnostics.ReportInvalidNumber(new TextSpan(start, length), _text, typeof(int));

                return new SyntaxToken(SyntaxKind.NumberToken, start, numberStr, value);
            }

            if (char.IsLetter(Current))
            {
                while (char.IsLetter(Current))
                    Next();

                var length = _position - start;
                string word = _text.Substring(start, length);
                SyntaxKind kind = SyntaxFacts.GetKeywordKind(word);
                return new SyntaxToken(kind, start, word, null);
            }

            switch (Current)
            {
                case '+':
                    return new SyntaxToken(SyntaxKind.PlusToken, _position++, "+", null);

                case '-':
                    return new SyntaxToken(SyntaxKind.MinusToken, _position++, "-", null);

                case '*':
                    return new SyntaxToken(SyntaxKind.StarToken, _position++, "*", null);

                case '/':
                    return new SyntaxToken(SyntaxKind.SlashToken, _position++, "/", null);

                case '(':
                    return new SyntaxToken(SyntaxKind.OpenParenthesisToken, _position++, "(", null);

                case ')':
                    return new SyntaxToken(SyntaxKind.CloseParenthesisToken, _position++, ")", null);

                case '&':
                    if (LookAhead == '&')
                    {
                        _position += 2;
                        return new SyntaxToken(SyntaxKind.AmpersandAmpersandToken, start, "&&", null);
                    }

                    break;

                case '|':
                    if (LookAhead == '|')
                    {
                        _position += 2;
                        return new SyntaxToken(SyntaxKind.PipePipeToken, start, "||", null);
                    }

                    break;

                case '=':
                    if (LookAhead == '=')
                    {
                        _position += 2;
                        return new SyntaxToken(SyntaxKind.EqualsEqualsToken, start, "==", null);
                    }
                    else
                        return new SyntaxToken(SyntaxKind.EqualsToken, _position++, "=", null);

                case '!':
                    if (LookAhead == '=')
                    {
                        _position += 2;
                        return new SyntaxToken(SyntaxKind.BangEqualsToken, start, "!=", null);
                    }
                    else
                        return new SyntaxToken(SyntaxKind.BangToken, _position++, "!", null);
            }

            Diagnostics.ReportBadCharacter(_position, Current);
            string text = _text.Substring(_position, 1);
            return new SyntaxToken(SyntaxKind.BadToken, _position++, text, null);
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

        #endregion Private methods
    }
}