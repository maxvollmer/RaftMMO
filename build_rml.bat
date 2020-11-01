:: RaftMMO RML Mod Build Script by Max Vollmer, based on RML Mod Build Script by TeKGameR

@echo off
title RaftMMO RML Mod Build Script

mkdir "Build"

rmdir /s /q "build_rml"
mkdir "build_rml"

robocopy "RaftMMO" "build_rml/RaftMMO" /E /XF RMLCompatibility /XF UMMModEntry.cs RMLCompatibilityFile.cs
robocopy "Data" "build_rml" *.*
robocopy "." "build_rml" README.txt LICENSE.txt modinfo.json

if exist "Build\\RaftMMO.rmod" ( del "Build\\RaftMMO.rmod" )
powershell "[System.Reflection.Assembly]::LoadWithPartialName('System.IO.Compression.FileSystem');[System.IO.Compression.ZipFile]::CreateFromDirectory(\"build_rml\", \"Build/RaftMMO.rmod\", 0, 0)"

copy "Build\\RaftMMO.rmod" "%SteamAppsLocation%common\\Raft\\mods\\RaftMMO.rmod"

rmdir /s /q "build_rml"

EXIT