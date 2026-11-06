@echo off
chcp 65001 >nul
setlocal
cd /d "%~dp0"

set "CSC=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo 未找到 .NET Framework 4.0 的 csc.exe
  exit /b 1
)

if not exist "bin" mkdir bin

"%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ ^
  /out:bin\EasyIP-Lite.exe ^
  /win32manifest:src\app.manifest ^
  /reference:System.dll ^
  /reference:System.Core.dll ^
  /reference:System.Drawing.dll ^
  /reference:System.Windows.Forms.dll ^
  src\ProgramLite.cs src\MainFormLite.cs src\NetworkHelper.cs

if errorlevel 1 (
  echo 编译失败
  exit /b 1
)

echo.
echo 编译成功: %~dp0bin\EasyIP-Lite.exe
echo 用法: 打开后只改后两段数字（如 16.25），回车即可应用。
exit /b 0
