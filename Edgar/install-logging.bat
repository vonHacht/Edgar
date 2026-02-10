@echo off
echo ================================
echo Installing logging packages...
echo ================================

dotnet add package Microsoft.Extensions.Logging
dotnet add package Serilog
dotnet add package Serilog.Extensions.Logging
dotnet add package Serilog.Sinks.File
dotnet add package Serilog.Sinks.Console

echo.
echo ================================
echo Logging packages installed!
echo ================================
pause
