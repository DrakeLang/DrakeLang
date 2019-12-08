﻿//------------------------------------------------------------------------------
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

namespace PHPSharpO
{
    public static class ConsoleExt
    {
        internal static void Write(string value, ConsoleColor foreground)
        {
            ConsoleColor oldColor = Console.ForegroundColor;
            Console.ForegroundColor = foreground;

            Console.Write(value);

            Console.ForegroundColor = oldColor;
        }

        public static void WriteLine(string value, ConsoleColor foreground)
        {
            ConsoleColor oldColor = Console.ForegroundColor;
            Console.ForegroundColor = foreground;

            Console.WriteLine(value);

            Console.ForegroundColor = oldColor;
        }

   
    }
}