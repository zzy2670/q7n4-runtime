# Workcell operation

## Environment

Windows, ABB RobotStudio 2026.3, RobotWare 7.15.4, official IRB1300 model package 2.8 and an available simulation license are required. The robot is IRB1300_7_140 with a compatible OmniCore virtual controller.

## Restore and execute

Download the `.rspak` add-in and `.rspag` workcell from the Release. Install the add-in normally and restart RobotStudio. Use File → Share → Unpack & Work to restore the workcell into a new empty folder; select the included libraries, matching RobotWare and controller backup. If prompted, allow the project Actor code for this session.

Wait until the station and virtual controller are mapped. Click Prepare & Run Demo once. Do not change the layout, view size, program or speed while it runs. The workcell feeds material, releases it at the interim fixture, grasps at C2 and packs indexed box locations. It clears completed contents on its recorded schedule and finishes at Ready with RAPID stopped and simulation paused.

The restored project writes execution data under its demonstration-log folder. If it stops unexpectedly, preserve the log and current scene before troubleshooting; do not repeatedly press the run button or manually force feedback signals. No ABB installers or SDK are distributed here.

## Build

Development additionally requires .NET 10 SDK and the RobotStudio SDK assemblies from the installed application. From this repository, run:

```powershell
.\source\build_current.ps1 -RobotStudioBin "C:\Program Files (x86)\ABB\RobotStudio 2026\Bin" -OutputRoot "C:\Temp\WorkspaceBuild"
```

Compilation is optional for using the supplied runtime. Rebuilt debugging metadata need not be byte-identical to published binaries. The source README_PREPARED file describes the implementation; this guide and the current Release describe normal operation.
