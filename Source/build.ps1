param(
    [string]$RobotStudioBin = (Join-Path ${env:ProgramFiles(x86)} 'ABB\RobotStudio 2026\Bin'),
    [string]$Dotnet = 'dotnet'
)
$ErrorActionPreference = 'Stop'
foreach ($project in @('Actor/ActorVisionSix.csproj', 'Runtime/Basic6PortableRuntime014.csproj', 'Addin/PortableSession014.csproj')) {
    & $Dotnet build (Join-Path $PSScriptRoot $project) "-p:RobotStudioBin=$RobotStudioBin" '-p:DebugType=None' '-p:DebugSymbols=false' --nologo -v:minimal
    if ($LASTEXITCODE -ne 0) { throw "Build failed: $project" }
}
