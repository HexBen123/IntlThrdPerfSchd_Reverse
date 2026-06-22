@echo off
chcp 65001 >nul
echo 正在卸载智能线程调度服务...
echo.

:: 1. 停止服务
net stop IntlThrdPerfSchd

:: 2. 删除服务
sc delete IntlThrdPerfSchd

echo.
echo 服务卸载完成！按任意键退出...
pause >nul