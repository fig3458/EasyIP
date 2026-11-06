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
  /out:bin\EasyIP.exe ^
  /win32manifest:src\app.manifest ^
  /reference:System.dll ^
  /reference:System.Core.dll ^
  /reference:System.Drawing.dll ^
  /reference:System.Windows.Forms.dll ^
  /reference:System.Net.dll ^
  src\Program.cs src\MainForm.cs src\NetworkHelper.cs src\ContactStore.cs

if errorlevel 1 (
  echo 编译失败
  exit /b 1
)

echo.
echo 编译成功: %~dp0bin\EasyIP.exe
echo 便携使用: 把 bin\EasyIP.exe 拷走即可；联系人库 contacts.db 会生成在同目录。
echo 目标机需已安装 ".NET Framework 4.0+"（Win7 SP1 / Win10 一般已有）。
exit /b 0
