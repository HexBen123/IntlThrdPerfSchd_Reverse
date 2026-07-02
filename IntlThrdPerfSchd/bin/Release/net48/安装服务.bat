@echo off
chcp 65001 >nul
echo 正在安装智能线程调度服务...
echo.

:: 1. 停止服务（如果已存在）
net stop IntlThrdPerfSchd >nul 2>&1

:: 2. 删除旧服务（如果已存在）
sc delete IntlThrdPerfSchd >nul 2>&1

:: 3. 创建新服务（LocalSystem 权限，开机自启）
sc create IntlThrdPerfSchd binPath= "%~dp0IntlThrdSchd.exe" start= auto obj= LocalSystem

:: 4. 设置服务描述
sc description IntlThrdPerfSchd "基于AI的高性能多线程任务调度服务，自动优化大小核CPU任务分配"



echo.
echo 服务安装完成！按任意键退出...
pause >nul