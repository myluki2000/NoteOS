@echo off

REM Run devcmd.bat to set up the environment for any msvc tools

REM Set vswhere.exe path
set "VSWHERE_PATH=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"

REM Check if vswhere.exe exists
if not exist "%VSWHERE_PATH%" (
    echo vswhere.exe not found. Exiting...
    exit /b 1
)

REM Run vswhere.exe to find the Visual Studio install path
for /f "usebackq tokens=* delims=" %%i in (`"%VSWHERE_PATH%" -latest -property installationPath`) do (
    set "VS_PATH=%%i"
)

REM Check if VS_PATH variable is set
if not defined VS_PATH (
    echo Visual Studio not found. Exiting...
    exit /b 1
)

REM Set vsdevcmd.bat path
set "VSDEVCMD_PATH=%VS_PATH%\VC\Auxiliary\Build\vcvars64.bat"

REM Check if vsdevcmd.bat exists
if not exist "%VSDEVCMD_PATH%" (
    echo vsdevcmd.bat not found. Exiting...
    exit /b 1
)

REM Call vsdevcmd.bat to set up the environment for cl.exe
call "%VSDEVCMD_PATH%"




REM actual build commands

cd lib

del baselib.lib
del baselib.obj

REM Compile baselib.c using cl.exe
cl.exe /c /EHsc ./../lib/baselib.c

REM link baselib.obj
lib.exe baselib.obj

REM Check if compilation was successful
if %ERRORLEVEL% neq 0 (
    echo C lib compilation failed.
    PAUSE
    exit /b 1
)

echo C lib compilation successful.
cd ./../

cd ./src

.\..\bflat\layouts\windows-x64\bflat.exe build --no-optimization -o ../bin/efi/boot/bootx64.efi --stdlib zero --os uefi --ldflags C:/Users/lukas/Desktop/NoteOS/lib/baselib.lib -i baselib --verbose -x

PAUSE