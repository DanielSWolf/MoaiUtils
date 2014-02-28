:: Remember current working directory
pushd .

:: Delete old output
del /q MoaiUtils.zip

:: Build
set msBuildPath=%windir%\Microsoft.NET\Framework64\v4.0.30319
%msBuildPath%\msbuild ..\MoaiUtils.sln /property:Configuration=Release /verbosity:minimal

:: Copy files to bin\MoaiUtils
md MoaiUtils
copy ..\bin\Release\*.exe MoaiUtils
copy ..\bin\Release\*.dll MoaiUtils
%LOCALAPPDATA%\Pandoc\pandoc.exe --from markdown_github --to html --standalone --title-prefix MoaiUtils --output MoaiUtils\README.html ..\..\README.md
7za.exe a MoaiUtils.zip MoaiUtils

:: Clean up
rmdir /s /q MoaiUtils

:: Restore original working directory
popd