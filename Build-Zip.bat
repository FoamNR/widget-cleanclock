@echo off
echo Building CleanClock standalone executable...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o ./dist/CleanClock-win-x64

if exist "dist\CleanClock-win-x64\widget.exe" (
    move /y "dist\CleanClock-win-x64\widget.exe" "dist\CleanClock-win-x64\CleanClock.exe" >nul
)
if exist "dist\CleanClock-win-x64\widget.pdb" (
    del /f /q "dist\CleanClock-win-x64\widget.pdb" >nul
)

echo Creating CleanClock-Windows.zip...
powershell -Command "Compress-Archive -Path 'dist\CleanClock-win-x64\CleanClock.exe' -DestinationPath 'CleanClock-Windows.zip' -Force"

echo Done! Output file: CleanClock-Windows.zip
pause
