param(
    [string]$RobotStudioExe = (Join-Path ${env:ProgramFiles(x86)} 'ABB\RobotStudio 2026\Bin\RobotStudio.exe')
)
$ErrorActionPreference = 'Stop'
if ((Split-Path $PSScriptRoot -Leaf) -ne 'Runtime014') {
    throw 'Keep the complete companion folder named Runtime014.'
}
if (-not (Test-Path -LiteralPath $RobotStudioExe -PathType Leaf)) {
    throw 'Supply -RobotStudioExe with the installed RobotStudio 2026 executable path.'
}
$descriptor = Join-Path $PSScriptRoot 'Q7n4_PortableCell014m.rsaddin'
foreach ($name in @('Q7n4_PortableCell014m.dll', 'MA6212_Basic6PortableRuntime014r6.dll', 'MA6212_Model014_ActorVisionSixM5.dll', 'PortableCell014.json', 'Q7n4_PortableCell014m.rsaddin')) {
    if (-not (Test-Path -LiteralPath (Join-Path $PSScriptRoot $name) -PathType Leaf)) {
        throw "Missing required runtime file: $name"
    }
}
# ABB's explicit add-in loading argument; this does not change installation files.
$addonArgument = '/addin:"' + [IO.Path]::GetFullPath($descriptor) + '"'
Start-Process -FilePath $RobotStudioExe -ArgumentList $addonArgument -WorkingDirectory $PSScriptRoot -WindowStyle Normal
