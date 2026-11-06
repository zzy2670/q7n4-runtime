# Usage

1. Install RobotStudio 2026 on Windows. Close the virtual controller for any earlier copy of this cell before unpacking another copy.
2. Use RobotStudio’s **Unpack & Work** workflow to restore `Native/q7n4-workcell.rspag` into an empty directory.
3. Install `RuntimePackage/q7n4-runtime-0.15.0.rspak` through RobotStudio’s add-in package interface. Restart RobotStudio if requested.
4. Open the restored initial station with the mapped virtual controller in Auto mode. The runtime requires the original unarmed initial scene, a stopped simulation at virtual time zero, and zero virtual I/O.
5. Select **Prepare and Run (6 parts)** once. The command checks the station and controller before starting. It writes the run results and cycle CSV to a new `Runs` directory beside the station directory. For a fresh run after an attempt, restore the initial scene.

The source-cell video and CSV show a completed six-part run. A second run also completed on a separately restored station and virtual controller using the supplied runtime DLL directly. The add-in package was built and checked, but its installation through the package interface and toolbar registration have not been tested for this version.

For source builds, use .NET SDK 10 with the installed RobotStudio SDK assemblies. See [source instructions](Source/README.md). Keep the supplied runtime and its geometry assets together.
