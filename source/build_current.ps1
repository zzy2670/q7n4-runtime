# Compile the add-in and actor without launching RobotStudio or connecting a controller.
# Keep build output outside the source tree so the supplied runtime inputs remain unchanged.
param([Parameter(Mandatory=$true)][string]$RobotStudioBin,[string]$Dotnet='dotnet',[Parameter(Mandatory=$true)][string]$OutputRoot)
$ErrorActionPreference='Stop'
if(-not [IO.Path]::IsPathRooted($RobotStudioBin) -or -not (Test-Path -LiteralPath $RobotStudioBin -PathType Container)){throw 'RobotStudioBin must be the installed SDK Bin absolute directory'}
if(-not [IO.Path]::IsPathRooted($OutputRoot)){throw 'OutputRoot must be an explicit absolute output directory outside source'}
$projectRoot=Join-Path $PSScriptRoot 'FinalCell012'
$resolvedOutput=[IO.Path]::GetFullPath($OutputRoot).TrimEnd([IO.Path]::DirectorySeparatorChar)
$resolvedSource=[IO.Path]::GetFullPath($projectRoot).TrimEnd([IO.Path]::DirectorySeparatorChar)
if($resolvedOutput.Equals($resolvedSource,[StringComparison]::OrdinalIgnoreCase) -or $resolvedOutput.StartsWith($resolvedSource+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'OutputRoot must be outside the frozen project source'}
foreach($item in @(@('MA6212_FinalCell012.csproj','addin'),@('MA6212_ActorDemo012.csproj','actor'))){
 $targetOutput=Join-Path $OutputRoot $item[1]
 & $Dotnet build (Join-Path $projectRoot $item[0]) -c Release -v minimal "-p:RobotStudioBin=$RobotStudioBin" "-p:OutputPath=$targetOutput/" "-p:BaseIntermediateOutputPath=$targetOutput/obj/"
 if($LASTEXITCODE -ne 0){throw ('Build failed: '+$item[0])}
}
# Compilation does not assert reproduced DLL SHA or native restoration/Demo acceptance.
