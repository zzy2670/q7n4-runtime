# Runtime source notes

The supplied add-in and actor implement the workcell's twenty-part handling sequence. For installation and normal execution, use the repository `USAGE.md` and the runtime packages in the Release.

## Components

| File | Responsibility |
|---|---|
| `AddInMain012.cs` | Registers the run command and checks the fresh station before execution. |
| `ActorDemo012.cs` | Tracks individual workpieces, contact, attachment, feed and box clearing. |
| `VacuumRulesDemo012.cs` | Computes contact and release-pose checks without snapping the workpiece. |
| `DemoMonitor012.cs` | Checks collision pairs according to the measured handling phase. |
| `SessionBinding012.cs` | Binds the restored station to its actual mapped virtual controller. |
| `SuperviseDemo012.cs` | Coordinates one run and checks controller, material and terminal states. |
| `DemoLedger012.cs` | Verifies ordered material events against the controller CSV. |
| `ManualViewRecorder012.cs` | Records only the station viewport and frame timing. |
| `FinalFreeze012.cs` | Pins the exact release, policy and actor assets used by the supplied runtime. |

## Geometry and signals

Configuration positions are expressed in millimetres and converted to metres at the RobotStudio API boundary. Quaternions use the stored W, X, Y, Z order. C1 and C2 are defined in the same workpiece frame; the current workpiece transform determines each contact's world pose.

The handling lifecycle is source pickup at C1, release at the interim fixture, pickup at C2, indexed box release and return to Ready. Each physical CAD part has its own identifier. Program stage counters alone do not count a successfully packed part.

The vacuum, feed and clear requests use corresponding feedback signals. The actor raises feedback only after the measured condition is satisfied. Contact dwell uses simulation time, so a paused simulation does not accumulate dwell. Releasing a part preserves its observed world pose.

## Execution and failure handling

Loading a station registers the user interface but does not start motion. The run command requires a fresh station, a matching virtual controller, a valid release policy and unchanged embedded assets. Native restoration can create a new controller identifier, so the binding is established from the actual restored session rather than a hard-coded export GUID.

The supervisor owns one execution attempt. A repeated command cannot overwrite an existing run or silently reset its counters. On a failure, the scene and evidence remain available for inspection. A final accepted state requires the controller to be stopped, the simulator paused, no held workpiece and verified box placement.

Collision checks cover the registered pairs and phase-dependent contact permissions. They are not a proof of continuous swept-volume clearance or physical suction performance. Vacuum is modelled as an ideal rigid attachment.

## Build and maintenance

Use `source/build_current.ps1` for the two current production projects. The files in `release_final012` are the supplied frozen runtime inputs. Their bytes and the constants in `FinalFreeze012.cs` must agree; editing a source comment does not update or replace the published binaries.

`build_candidate_rspak.py` and `refresh_prepared_policy_embedding012.py` are retained development utilities for earlier candidate assets. They are not the current installation entry and should not be run over the supplied release directory.

The tests check identity handling, session binding, recorder ownership, tracking decisions and release-policy rejection. Rebuilding can change debug metadata; verify a new binary and its complete hash chain before distributing it as a replacement runtime.
