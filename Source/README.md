# Build from source

Use Windows, .NET SDK 10 and RobotStudio 2026. Run `build.ps1` in this directory. Set `-RobotStudioBin` if RobotStudio is installed elsewhere, and set `-Dotnet` if the SDK executable is not on PATH.

The Actor project builds the simulated conveyor and vision logic. Runtime compiles the supervisor and Monitor sources; Addin references Runtime and provides the toolbar command. No ABB SDK assemblies are included in these source projects.

The supplied companion runtime is configured for exact asset hashes. Rebuilding can produce different DLL hashes. Before replacing a supplied runtime DLL, update its dependent hash bindings and verify the resulting installation and restored station. Source compilation alone does not prove a simulation run.
