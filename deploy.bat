
@echo off

rem set H=R:\KSP_1.4.3_IFI_Test
set GAMEDIR=IFILS
set DEPEND1=CommunityCategoryKit
set DEPEND2=Squad
set DEPEND3=CommunityResourcePack

echo %H%

mkdir "GameData\%GAMEDIR%\Plugins"
copy /Y "%1%2" "GameData\%GAMEDIR%\Plugins"
copy /Y "%1%2.mdb" "GameData\%GAMEDIR%\Plugins"
copy /Y %GAMEDIR%.version GameData\%GAMEDIR%

xcopy /y /s /I GameData\%GAMEDIR% "%1\GameData\%GAMEDIR%"
xcopy /y /s /I GameData\%DEPEND1% "%1\GameData\%DEPEND1%"
xcopy /y /s /I GameData\%DEPEND2% "%1\GameData\%DEPEND2%"
xcopy /y /s /I GameData\%DEPEND3% "%1\GameData\%DEPEND3%"
