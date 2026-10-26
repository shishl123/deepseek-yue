@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo 以调试方式启动（带控制台输出，便于排查问题）...
DeepSeekPet.exe %*
echo.
echo 桌宠已退出。调试日志：%~dp0logs\debug.log
pause
