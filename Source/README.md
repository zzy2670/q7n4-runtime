# Build from source

Use Windows, .NET SDK 10, and RobotStudio 2026. Run `build.ps1` in this directory, with `-RobotStudioBin` pointing to the installed RobotStudio `Bin` directory. Use `-Dotnet` if the SDK executable is not on PATH.

The Actor project builds the simulated conveyor and camera logic. The Runtime project compiles the supervisor and Monitor sources together. The Addin project references Runtime and builds the toolbar command. Source files retain the technical comments used by the implementation.

The DLLs in `../Runtime014` are the reviewed candidates. A local rebuild can have different file hashes because of source paths and debug metadata; validate a rebuilt version before replacing those files. No ABB SDK DLLs are redistributed in the source projects.
