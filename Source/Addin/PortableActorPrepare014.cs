// Ordinary unarmed Actor construction for a freshly restored initial scene.
using System;using System.IO;using System.Linq;using System.Text;using System.Text.Json.Nodes;using System.Threading;
using ABB.Robotics.RobotStudio;using ABB.Robotics.RobotStudio.Stations;
namespace Q7n4.Portable014m {
 public static class PortableActorPrepare014 {
  const string AssetName="MA6212_Model014_ActorVisionSixM5.dll",ConfigName="Model014.ActorConfig.json",Location="MA6212.Model014.Actor.CellCodeBehind,MA6212_Model014_ActorVisionSixM5.dll";
  public static SmartComponent Prepare(PortableSession014 session,byte[] actorDll,byte[] persistedConfig){
   if(!session.IsCurrent||SynchronizationContext.Current is not System.Windows.Forms.WindowsFormsSynchronizationContext||Simulator.State!=SimulationState.Stopped||Simulator.CurrentTime!=0||session.Station.CollisionSets.Any())throw new InvalidOperationException("Fresh restored stopped session without active monitor required.");
   var station=session.Station;var cfg=session.Configuration;var vc=session.Controller;
   var robot=station.GraphicComponents.OfType<Mechanism>().Single();var source=station.GraphicComponents.OfType<Part>().Single(p=>p.Name=="SourcePart");var tool=station.GraphicComponents.OfType<Part>().Single(p=>p.Name=="VacuumTool");
   if(station.GraphicComponents.OfType<Part>().Count()!=30||station.Attachments.Any(a=>ReferenceEquals(a.AttachmentChild,source))||station.GraphicComponents.OfType<Part>().Any(p=>p.Name.StartsWith("Model014_Part_",StringComparison.Ordinal)))throw new InvalidOperationException("Restore the original one-source initial scene; completed geometry cannot be prepared in place.");
   var originalQ=robot.GetJointValues().ToArray();var originalWorld=station.GraphicComponents.ToDictionary(g=>g,g=>g.Transform.GlobalMatrix.Elements.ToArray());
   var data=JsonNode.Parse(persistedConfig).AsObject();if(data["Armed"].GetValue<bool>()||data["MaximumParts"].GetValue<int>()!=6||!data["AllowFeed"].GetValue<bool>()||data["AllowBoxClear"].GetValue<bool>())throw new InvalidDataException("Embedded six-piece unarmed configuration required.");
   data["RunId"]=session.RunId;data["LogDirectory"]=Path.Combine(session.EvidenceDirectory,"actor");string nativeConfigurationId=session.Configuration.UniqueId.ToString();if(Guid.Parse(nativeConfigurationId)!=session.ConfigurationId)throw new InvalidOperationException("Actual native configuration changed before Actor preparation.");data["ConfigurationId"]=nativeConfigurationId;data["Armed"]=false;
   if(Directory.Exists(data["LogDirectory"].GetValue<string>()))throw new IOException("Run directory already exists; restore initial station for a new run.");Directory.CreateDirectory(data["LogDirectory"].GetValue<string>());
   if(data["ConfigurationId"].GetValue<string>()!=session.Configuration.UniqueId.ToString())throw new InvalidDataException("Actor configuration text must retain the actual SDK UniqueId representation.");var actors=station.GraphicComponents.OfType<SmartComponent>().ToArray();SmartComponent actor;
   if(actors.Length==0){
    if(station.GraphicComponents.Count!=31||station.Connections.Any())throw new InvalidOperationException("Only the exact initial cell may receive a new Actor.");
    actor=new SmartComponent{Name="Model014_Actor",CanBeSimulated=true,PerInstanceCodeBehind=true};
    actor.Assets.Add(new Asset{Name=AssetName});actor.Assets.Last().SetData(actorDll);
    actor.Assets.Add(new Asset{Name=ConfigName});actor.Assets.Last().SetData(Encoding.UTF8.GetBytes(data.ToJsonString()));
    foreach(string name in new[]{"doVacuum","doFeedRequest","doBoxReset"})actor.IOSignals.Add(new IOSignal(name,IOSignalType.DigitalInput));
    foreach(string name in new[]{"diVacuumOK","diFeedReady","diBoxReady"})actor.IOSignals.Add(new IOSignal(name,IOSignalType.DigitalOutput));
    station.GraphicComponents.Add(actor);if(!cfg.SimulatedObjects.Contains(actor))cfg.SimulatedObjects.Add(actor);
    foreach(string name in new[]{"doVacuum","doFeedRequest","doBoxReset"})station.Connections.Add(new IOConnection(vc,name,actor,name));
    foreach(string name in new[]{"diVacuumOK","diFeedReady","diBoxReady"})station.Connections.Add(new IOConnection(actor,name,vc,name));
   }else{
    actor=actors.Single();if(actor.Name!="Model014_Actor"||actor.CodeBehindLocation!=Location||actor.StateCache.ContainsKey("Model014.Runtime")||actor.StateCache.Keys.Any(k=>k.EndsWith("Hook",StringComparison.Ordinal)||k=="Model014.MonitorOwner")||!actor.Assets.Single(a=>a.Name==AssetName).GetData().SequenceEqual(actorDll))throw new InvalidOperationException("Only the untouched unarmed packaged Actor may be initialized.");
    actor.Assets.Single(a=>a.Name==ConfigName).SetData(Encoding.UTF8.GetBytes(data.ToJsonString()));
   }
   if(!session.IsCurrent)throw new InvalidOperationException("Restored session changed while preparing.");
   actor.StateCache["Model014.PrepareOnUI"]=true;actor.StateCache["Model014.UIContext"]=SynchronizationContext.Current;actor.StateCache["Model014.UIThread"]=Environment.CurrentManagedThreadId;actor.CodeBehindLocation=Location;actor.InitializeCodeBehind();
   if(actor.CodeBehindStatus!=CodeBehindStatus.Loaded||!actor.StateCache.TryGetValue("Model014.Runtime",out var runtime)||runtime==null||runtime.GetType().Assembly.GetName().Name!="MA6212_Model014_ActorVisionSixM5"||runtime.GetType().GetProperty("Armed")?.GetValue(runtime) is not false||runtime.GetType().GetProperty("Faulted")?.GetValue(runtime) is not false||!originalQ.SequenceEqual(robot.GetJointValues())||originalWorld.Any(e=>!station.GraphicComponents.Contains(e.Key)||!e.Value.SequenceEqual(e.Key.Transform.GlobalMatrix.Elements)))throw new InvalidOperationException("Actual unarmed Actor initialization or unchanged physical scene check failed; retain for inspection.");
   return actor;
  }
 }
}
