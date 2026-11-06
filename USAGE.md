# Usage

1. Install RobotStudio 2026 on Windows. Close the virtual controller belonging to an earlier copy of this cell before unpacking another copy. The runtime binds the actual mapped controller and its current system ID; leaving another copy running can cause confusion when selecting a controller.
2. Use RobotStudio's official Unpack & Work workflow to restore the Pack & Go in `Native` into an empty directory.
3. Install the package in `RuntimePackage` using RobotStudio's add-in package interface. Restart RobotStudio if requested. Keep the runtime companion files together.
4. Open the restored initial station. Use Auto mode, virtual time zero, zero virtual IO, and the original unarmed initial scene. The toolbar command checks these conditions and initializes the stopped virtual controller before it starts the sequence.
5. Select **Prepare and Run (6 parts)** once. Results and the cycle CSV are written to a fresh `Runs` directory beside the station directory. After a failed or completed attempt, restore the initial scene for a fresh run.

The supplied video and CSV document a verified complete source-cell run. Official unpacking to a new directory, cold-start loading of the installed add-in, toolbar registration and actual unarmed preparation on the restored controller all passed. The restored controller received a new system ID. No second complete six-part run is claimed.

For source builds, use .NET SDK 10 and the installed RobotStudio SDK assemblies. See `Source/README.md`. Build output is separate from the supplied reviewed runtime files.
