:: RaftMMO UMM Mod Build Script by Max Vollmer, based on RML Mod Build Script by TeKGameR

@echo off
title RaftMMO UMM Mod Build Script

mkdir "Build"

rmdir /s /q "build_umm"
mkdir "build_umm/"
mkdir "build_umm/RaftMMO/"
mkdir "build_umm/RaftMMO/Data"

robocopy "bin" "build_umm/RaftMMO" RaftMMO.dll README.txt CHANGELOG.txt LICENSE.txt Info.json
robocopy "./Data" "build_umm/RaftMMO/Data" *.*

if exist "Build\\RaftMMO.zip" ( del "Build\\RaftMMO.zip" )
powershell "[System.Reflection.Assembly]::LoadWithPartialName('System.IO.Compression.FileSystem');[System.IO.Compression.ZipFile]::CreateFromDirectory(\"build_umm\", \"Build/RaftMMO.zip\", 0, 0)"

rmdir /s /q "%SteamAppsLocation%common\\Raft\\mods\\RaftMMO"
robocopy "build_umm\\RaftMMO" "%SteamAppsLocation%common\\Raft\\mods\\RaftMMO" /E

rmdir /s /q "build_umm"

EXIT