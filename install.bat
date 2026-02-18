@echo off
echo ================================
echo Installing packages & tools
echo ================================
cd Edgar

dotnet add package Microsoft.Extensions.Logging
dotnet add package Serilog
dotnet add package Serilog.Extensions.Logging
dotnet add package Serilog.Sinks.File
dotnet add package Serilog.Sinks.Console
dotnet add package MongoDB.Driver

dotnet tool install -g dotnet-format

dotnet add package DotNetEnv

echo.
echo ================================
echo Done!
echo ================================
pause
