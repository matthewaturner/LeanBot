@echo off
REM Quick wrapper for run-backtest.ps1
powershell -ExecutionPolicy Bypass -File "%~dp0run-backtest.ps1" %*
