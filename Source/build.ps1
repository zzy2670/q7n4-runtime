param([string]$RobotStudioBin = "C:\Program Files (x86)\ABB\RobotStudio 2026\Bin", [string]$Dotnet = "dotnet")
$ErrorActionPreference = "Stop"
foreach ($project in @("Actor/ActorVisionSix.csproj", "Addin/PortableSession014.csproj")) {
    & $Dotnet build (Join-Path $PSScriptRoot $project) "-p:RobotStudioBin=$RobotStudioBin" --nologo -v:minimal
    if ($LASTEXITCODE -ne 0) { throw "Build failed: $project" }
}
