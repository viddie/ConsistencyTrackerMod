@echo off
setlocal enabledelayedexpansion

set NAMEARG=%1
:loop
    if "%NAMEARG%"=="" (
        echo Enter Mod Name:
        set /p MODNAME=""
        echo !MODNAME!>%~dp0/init.modname
    ) else (
        set MODNAME=%NAMEARG%
        set "NAMEARG="
    )

    :: skip verification
    ::goto exitloop

    :: findstr character classes are horribly cursed
    :: https://stackoverflow.com/a/8767815
    findstr /r /c:"[^0123-9aAb-Cd-EfFg-Ij-NoOp-St-Uv-YzZ _-]" %~dp0/init.modname >nul 2>&1
    if errorlevel 1 ( echo. ) else (
        echo !!! Name contains invalid characters.
        echo Valid characters include \`a-zA-Z0-9 _-\`
        set "MODNAME="
        goto loop
    )

    echo Checking for duplicates...
    if not exist init.modlist (
        :: first command for Win7 compat
        powershell -Command "& {[Net.ServicePointManager]::SecurityProtocol = ""Tls, Tls11, Tls12, Ssl3\"";Invoke-WebRequest (Invoke-WebRequest https://everestapi.github.io/modupdater.txt).Content -OutFile init.modlist}"
    )
    :: in case powershell fails
    if exist init.modlist (
        findstr /b /l /c:"%MODNAME%:" %~dp0/init.modlist >nul 2>&1
        if errorlevel 1 ( echo. ) else (
            echo Name already in use^^!
            goto loop
        )
    )

:exitloop

:: this is an absolute mess
setlocal enableextensions disabledelayedexpansion
:recurse
for %%f in (*) do (
    :: ignore .* files (.gitignore)
    echo %%f|findstr /b \.>nul 2>&1
    if errorlevel 1 (
        :: ignore init.* files (like this one!)
        echo %%f|findstr /b init>nul 2>&1
    )
    if errorlevel 1 (
        echo %%f
        >"%%f.tmp" (
            for /f "delims=" %%i in ('findstr /n "^" "%%f"') do (
                set "line=%%i"
                setlocal enabledelayedexpansion
                set "line=!line:*:=!"
                if defined line set "line=!line:$MODNAME$=%MODNAME%!"
                echo(!line!
                endlocal
            )
        )
        move /y "%%f.tmp" "%%f" >nul
        set "file=%%f"
        setlocal enabledelayedexpansion
        move /y "%%f" "!file:$MODNAME$=%MODNAME%!
        endlocal
    )
)
for /D %%d in (*) do (
    echo %%d|findstr /b \.>nul 2>&1
    if errorlevel 1 (
        pushd %%d
        call :recurse
        popd
    )
)

echo Initialization Complete.
del "%~dp0/README.md"
del "%~dp0/LICENSE"
(goto) 2>nul & del "%~dp0/init.*" rem delete init files