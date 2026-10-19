@echo off
:: 一键以管理员权限将 FluentPass.dll 复制到 Program Files 全局目录并清理历史残留
set "SOURCE=%~dp0src\FluentPass\bin\Release\net48\FluentPass.dll"
set "DEST=C:\Program Files\KeePass Password Safe 2\Plugins\FluentPass.dll"
set "OLD_DEST=C:\Program Files\KeePass Password Safe 2\Plugins\KeePassFluentUI.dll"

net session >nul 2>&1
if %errorLevel% == 0 (
    echo [INFO] 已获得管理员权限，正在部署...
    copy /Y "%SOURCE%" "%DEST%"
    if %errorLevel% == 0 (
        if exist "%OLD_DEST%" (
            del /F /Q "%OLD_DEST%" >nul 2>&1
            echo [INFO] 已自动清理历史旧版本残留: KeePassFluentUI.dll
        )
        echo [SUCCESS] 部署成功到: %DEST%
    ) else (
        echo [ERROR] 部署失败，请检查目标目录是否存在。
    )
    pause
) else (
    echo [INFO] 正在请求管理员权限以部署到 Program Files...
    powershell -Command "Start-Process cmd -ArgumentList '/c \"\"%~f0\"\"' -Verb RunAs"
)
