@echo off
echo ================================
echo Installing packages & tools
echo ================================

dotnet add package Microsoft.Extensions.Logging
dotnet add package Serilog
dotnet add package Serilog.Extensions.Logging
dotnet add package Serilog.Sinks.File
dotnet add package Serilog.Sinks.Console

dotnet tool install -g dotnet-format

echo.
echo ================================
echo Done!
echo ================================
pause
