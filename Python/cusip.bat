@echo off
REM ==============================
REM Download CUSIP
REM ==============================

REM --- setup ---
setlocal

REM --- your code here ---
python cik_to_cusip.py --csv output/samples_2010_2023.csv --cik-col CIK --out output/samples_2010_2023_with_cusip.csv

REM --- cleanup ---
endlocal
exit /b 0

