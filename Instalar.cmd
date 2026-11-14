@echo off
setlocal
set "APP=%LOCALAPPDATA%\Programs\FondosDePantallas"
if not exist "%APP%\Cuadros" mkdir "%APP%\Cuadros"
"%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe" /nologo /target:winexe /out:"%APP%\Backdrop Studio for Windows.exe" "%~dp0Fondos.cs"
if errorlevel 1 (pause & exit /b 1)
copy /Y "%~dp0Bloqueo.ps1" "%APP%\" >nul || exit /b 1
powershell -NoProfile -Command "$s=(New-Object -ComObject WScript.Shell).CreateShortcut((Join-Path ([Environment]::GetFolderPath('Programs')) 'Backdrop Studio for Windows.lnk'));$s.TargetPath=(Join-Path $env:APP 'Backdrop Studio for Windows.exe');$s.WorkingDirectory=$env:APP;$s.Save()"
start "" "%APP%\Backdrop Studio for Windows.exe"
