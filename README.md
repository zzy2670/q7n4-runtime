# Workbench cell

A RobotStudio cell that moves elbow pipes from a conveyor to an interim fixture, changes the vacuum contact point, and places each pipe in a box position. Simulated vision measures the actual CAD position. One nominal feed and five feeds with random offsets were completed.

**Verified:** six distinct parts in slots 0–5, 24 contact/fixture/placement events, six CSV rows with zero errors, final Ready, normal stop and three fresh stable stopped-state samples. Mean cycle time: **231.130 s**.

[Watch the complete run](Demo/q7n4-full-run.mp4) · [Cycle data](RunData/cycles.csv) · [Run summary](RunData/run_summary.json)

![Final scene with six placed parts](Demo/final-scene.png)

## Run

Use RobotStudio 2026 on Windows. **Close the previous copy's virtual controller before restoring this copy** to avoid connecting to an unintended running cell. Restore `Native/q7n4-workcell.rspag` into an empty directory, install the runtime package in `RuntimePackage`, and restart RobotStudio when requested. Open the restored initial station and select **Prepare and Run (6 parts)**. See [usage](USAGE.md).

The six-part run above was verified on the source cell. Official unpacking into a new directory, cold-start loading of the installed add-in, toolbar registration and actual unarmed preparation on the restored controller have all **passed**. The restored controller received a new system ID. A second complete six-part run on the restored copy was not performed.

## Files

- `Native`: official Pack & Go project.
- `RuntimePackage`: installable RobotStudio add-in package.
- `Runtime014`: complete companion runtime and geometry assets.
- `Source`: editable C# projects and build instructions.
- `Demo` and `RunData`: actual video, final scene, cycle data and placements.

Reports and assignment documents are not included. No whole-project ZIP is required.
