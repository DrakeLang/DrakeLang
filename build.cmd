@echo off

dotnet build
dotnet vstest PHPSharp.Tests\bin\Debug\PHPSharp.Tests.dll