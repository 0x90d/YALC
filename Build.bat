CD /D YetAnotherLosslessCutter
dotnet publish -c Release -v q --self-contained -r win-x64 -f netcoreapp3.1 -o "..\Releases\YALC_x64"

CD /D ..

@echo off
REM Copy ffmpeg windows binaries
if not exist ".\Ffmpeg\" goto end
if not exist "Releases\YALC_x64\bin" mkdir ".\Releases\YALC_x64\bin"
xcopy /q /y ".\Ffmpeg\*.*" ".\Releases\YALC_x64\bin"

:end