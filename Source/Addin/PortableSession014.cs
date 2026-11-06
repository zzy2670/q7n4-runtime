// Bound from the currently restored station, never from a development controller path.
using System;
using System.IO;
using System.Linq;
using System.Threading;
using ABB.Robotics.RobotStudio;
using ABB.Robotics.RobotStudio.Stations;
namespace Q7n4.Portable014i {
 public sealed class PortableSession014 {
  public Station Station {get;}
  public VirtualController Controller {get;}
  public SimulationConfiguration Configuration {get;}
  public Guid SystemId {get;}
  public string StationPath {get;}
  public string SystemPath {get;}
  public string HomePath => Path.Combine(SystemPath,"HOME");
  public Guid ConfigurationId {get;}
  public string RunId {get;}
  public int NumericRunId {get;}
  public string EvidenceDirectory {get;}
  PortableSession014(Station s,VirtualController vc,SimulationConfiguration cfg,string evidenceRoot,int numericRunId) {
   Station=s;Controller=vc;Configuration=cfg;SystemId=Guid.Parse(vc.SystemId);
   StationPath=Path.GetFullPath(s.FileInfo.FullName);SystemPath=Path.GetFullPath(vc.SystemPath);
   ConfigurationId=Guid.Parse(cfg.UniqueId);NumericRunId=numericRunId;
   RunId="basic6_"+numericRunId+"_"+Guid.NewGuid().ToString("N");
   EvidenceDirectory=Path.Combine(evidenceRoot,RunId);
  }
  public static PortableSession014 FromMappedStation(int numericRunId) {
   if(SynchronizationContext.Current is not System.Windows.Forms.WindowsFormsSynchronizationContext)throw new InvalidOperationException("Use the RobotStudio Prepare and Run button.");
   var s=Station.ActiveStation??throw new InvalidOperationException("Open the restored initial station first.");
   var vc=s.VirtualControllers.Single();var cfg=Simulator.ActiveConfiguration??throw new InvalidOperationException("No active simulation configuration.");
   if(!ReferenceEquals(Project.ActiveProject,s)||s.FileInfo==null||!Path.IsPathRooted(s.FileInfo.FullName)||!Path.IsPathRooted(vc.SystemPath)||Guid.Parse(vc.SystemId)==Guid.Empty||vc.SystemState.ToString()!="Started"||vc.MappingState.ToString()!="Mapped"||(Simulator.State!=SimulationState.Stopped&&Simulator.State!=SimulationState.Ready)||Simulator.CurrentTime!=0||numericRunId<=0)throw new InvalidOperationException("The exact restored initial stopped virtual cell is required.");
   if(cfg.ControllerConfigurations[vc].AutoStartProgram||cfg.ControllerConfigurations[vc].AutoStopSimulation||cfg.VirtualTimeMode.ToString()!="VirtualTimeSlice"||!String.IsNullOrEmpty(cfg.InitialState))throw new InvalidOperationException("Explicit controller start with virtual-time simulation is required.");
   string stationDirectory=Path.GetDirectoryName(Path.GetFullPath(s.FileInfo.FullName));string runParent=Directory.GetParent(stationDirectory)?.FullName??throw new InvalidDataException("Restore the station in a project folder, not directly in a drive root.");
   return new PortableSession014(s,vc,cfg,Path.Combine(runParent,"Runs"),numericRunId);
  }
  public bool IsCurrent => ReferenceEquals(Station.ActiveStation,Station)&&ReferenceEquals(Project.ActiveProject,Station)&&ReferenceEquals(Simulator.ActiveConfiguration,Configuration)&&Station.VirtualControllers.Count==1&&ReferenceEquals(Station.VirtualControllers.Single(),Controller)&&Guid.Parse(Controller.SystemId)==SystemId&&Guid.Parse(Configuration.UniqueId)==ConfigurationId&&String.Equals(Station.FileInfo?.FullName,StationPath,StringComparison.OrdinalIgnoreCase)&&String.Equals(Path.GetFullPath(Controller.SystemPath),SystemPath,StringComparison.OrdinalIgnoreCase);
 }
}
