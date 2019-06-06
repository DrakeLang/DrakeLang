@echo off

dotnet build
dotnet vstest PHPSharp.Tests\bin\Debug\netcoreapp2.1\PHPSharp.Tests.dll