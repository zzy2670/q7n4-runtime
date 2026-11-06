using System;using System.Text.Json.Serialization;using System.IO;using System.Linq;using System.Globalization;using System.Text.Json;using System.Threading.Tasks;
using ABB.Robotics.Math;using ABB.Robotics.RobotStudio.Stations;using ABB.Robotics.Controllers;
namespace MA6212.Model014.Actor {
 // Feed randomness is not a sensor result and is never sent to RAPID.
 public static class RandomFeedSix {
  public static double[][] Offsets(int seed){var random=new Random(seed);return Enumerable.Range(0,6).Select(i=>i==0?new[]{0d,0d}:new[]{10*random.NextDouble()-5,10*random.NextDouble()-5}).ToArray();}
  public static Matrix4 ExpectedInitialSourcePose(string layoutJson,int seed){
   using var document=JsonDocument.Parse(layoutJson);var p=document.RootElement.GetProperty("part_poses").GetProperty("source");var xyz=p.GetProperty("position_mm").EnumerateArray().Select(v=>v.GetDouble()/1000).ToArray();var quat=p.GetProperty("quaternion_wxyz").EnumerateArray().Select(v=>v.GetDouble()).ToArray();var offset=Offsets(seed)[0];xyz[0]+=offset[0]/1000;xyz[1]+=(offset[1]-1380)/1000;return new Matrix4(new Vector3(xyz),new Quaternion(quat));
  }
 }
 public sealed record VisionObservationSix(string PartId,int Sequence,string Frame,double VirtualTimeMs,double DxMm,double DyMm,[property: JsonIgnore] Matrix4 ActualPartWorld) {
  public double[] MeasuredWorldM=>ActualPartWorld.Elements.ToArray();
 }
 public static class SimulatedVisionSix {
  // Ideal simulated camera: observe the actual, stationary CAD pose on the UI thread.
  // This method has no seed, feed offset table or desired material pose as an input.
  public static VisionObservationSix Observe(Part actualPart,Matrix4 nominalSourceFrame,int sequence,double virtualMs){
   if(actualPart==null||sequence<1||sequence>6||!double.IsFinite(virtualMs)||virtualMs<0||!Guid.TryParse(actualPart.UniqueId,out var id)||id==Guid.Empty)throw new InvalidDataException("Actual material identity and sample time required");
   var actual=actualPart.Transform.GlobalMatrix;var e=VacuumContactRules.Error(actual,nominalSourceFrame);var delta=actual.Translation-nominalSourceFrame.Translation;double dx=1000*delta.x,dy=1000*delta.y;
   if(!double.IsFinite(dx)||!double.IsFinite(dy)||Math.Abs(dx)>5.000001||Math.Abs(dy)>5.000001||Math.Abs(delta.z)>.0001||e.AngleRadians>1e-5)throw new InvalidDataException("Measured stationary part outside supported world-XY camera area");
   return new VisionObservationSix(actualPart.UniqueId,sequence,"station_world",virtualMs,dx,dy,actual);
  }
 }
 public sealed class VisionPublisherSix {
  // Shared only with current runtime polling connections; protective Start/Stop never acquire it.
  // Hold across Connect/Logon, the complete packet transaction and disposal; never retry a partial packet.
  public static object ControllerAccessGate {get;} = new object();
  const string Module="MA6212_OnePiece014";readonly Guid systemId;readonly string home;readonly Func<bool> mayPublish;
  public VisionPublisherSix(Guid sid,string systemPath,Func<bool> permission){systemId=sid;home=Path.GetFullPath(Path.Combine(systemPath,"HOME"));mayPublish=permission??throw new ArgumentNullException(nameof(permission));}
  public Task Publish(VisionObservationSix sample)=>Task.Run(()=>{lock(ControllerAccessGate){
   using var c=Controller.Connect(systemId,ConnectionType.Standalone);c.Logon(UserInfo.DefaultUser);if(!c.IsVirtual||c.SystemId!=systemId||!String.Equals(Path.GetFullPath(c.GetEnvironmentVariable("HOME")),home,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Only the current virtual cell may receive simulated camera data");
   using var master=Mastership.Request(c);var task=c.Rapid.GetTask("T_ROB1");
   void Guard(){if(!mayPublish()||c.OperatingMode!=ControllerOperatingMode.Auto||c.Rapid.ExecutionStatus!=ABB.Robotics.Controllers.RapidDomain.ExecutionStatus.Running||c.IOSystem.GetSignal("doFeedRequest").Value!=1||double.Parse(task.GetRapidData(new[]{Module,"cycleIndex"}).StringValue,CultureInfo.InvariantCulture)!=sample.Sequence||double.Parse(task.GetRapidData(new[]{Module,"stage"}).StringValue,CultureInfo.InvariantCulture)!=8)throw new InvalidOperationException("Camera request no longer belongs to the waiting current cycle");}
   void Number(string name,double value){Guard();task.GetRapidData(new[]{Module,name}).Value=new ABB.Robotics.Controllers.RapidDomain.Num(value);}
   void Text(string name,string value){Guard();task.GetRapidData(new[]{Module,name}).Value=new ABB.Robotics.Controllers.RapidDomain.String(value);}
   Guard();task.GetRapidData(new[]{Module,"visionReady"}).Value=new ABB.Robotics.Controllers.RapidDomain.Bool(false);
   Number("visionDxMm",sample.DxMm);Number("visionDyMm",sample.DyMm);Number("visionSequence",sample.Sequence);Number("visionVirtualTimeMs",sample.VirtualTimeMs);Text("visionPartId",sample.PartId);Text("visionFrame",sample.Frame);
   // Validate while readiness is still false: RAPID is allowed to consume and clear it immediately after commit.
   Guard();
   void ReadNumber(string name,double expected,double tolerance=0){var data=task.GetRapidData(new[]{Module,name});string raw=data.StringValue;var typed=data.Value;double actual=typed is ABB.Robotics.Controllers.RapidDomain.Num number?number.Value:double.NaN;if(!double.IsFinite(actual)||Math.Abs(actual-expected)>tolerance)throw new InvalidOperationException("Actual RAPID camera numeric readback failed: "+name+" actual="+actual.ToString("R",CultureInfo.InvariantCulture)+" expected="+expected.ToString("R",CultureInfo.InvariantCulture)+" tolerance="+tolerance.ToString("R",CultureInfo.InvariantCulture)+" raw="+raw+" type="+(typed?.GetType().FullName??"null"));}
   void ReadText(string name,string expected){if(task.GetRapidData(new[]{Module,name}).Value is not ABB.Robotics.Controllers.RapidDomain.String actual||actual.Value!=expected)throw new InvalidOperationException("Actual RAPID camera text readback failed: "+name);}
   if(task.GetRapidData(new[]{Module,"visionReady"}).Value is not ABB.Robotics.Controllers.RapidDomain.Bool ready||ready.Value)throw new InvalidOperationException("Camera packet must remain uncommitted during field validation");
   ReadNumber("visionDxMm",sample.DxMm,.001);ReadNumber("visionDyMm",sample.DyMm,.001);ReadNumber("visionSequence",sample.Sequence);ReadNumber("visionVirtualTimeMs",sample.VirtualTimeMs,20);ReadText("visionPartId",sample.PartId);ReadText("visionFrame",sample.Frame);
   Guard();task.GetRapidData(new[]{Module,"visionReady"}).Value=new ABB.Robotics.Controllers.RapidDomain.Bool(true);
   // Successful setter return commits this one verified packet. Do not race the consumer's legitimate clear.

  }});
 }
}
