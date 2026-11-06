// Geometry adapter only: no AddinMain/Run entry, signals, timer, controller, or runtime auto-enable.
// Invoke on RobotStudio UI thread only, after exact station/tool/workpiece identity guards.
using System;using System.Linq;using ABB.Robotics.Math;using ABB.Robotics.RobotStudio.Stations;
namespace MA6212.Model014.Actor {
 public sealed class PoseError {
  public double PositionMetres,AngleRadians;
  public bool Within(double positionMetres,double angleRadians)=>PositionMetres<=positionMetres&&AngleRadians<=angleRadians;
 }
 public static class VacuumContactRules {
  public static PoseError Error(Matrix4 actual,Matrix4 expected){
   Vector3 a=actual.Translation,b=expected.Translation;double dx=a.x-b.x,dy=a.y-b.y,dz=a.z-b.z;
   Quaternion qa=actual.Quaternion,qb=expected.Quaternion;Vector3 va=qa.Vector,vb=qb.Vector;
   double dot=qa.Scalar*qb.Scalar+va.x*vb.x+va.y*vb.y+va.z*vb.z;
   double normA=System.Math.Sqrt(qa.Scalar*qa.Scalar+va.x*va.x+va.y*va.y+va.z*va.z),normB=System.Math.Sqrt(qb.Scalar*qb.Scalar+vb.x*vb.x+vb.y*vb.y+vb.z*vb.z);
   if(normA<1e-12||normB<1e-12)throw new InvalidOperationException("Invalid measured orientation.");
   return new PoseError{PositionMetres=System.Math.Sqrt(dx*dx+dy*dy+dz*dz),AngleRadians=2*System.Math.Acos(System.Math.Clamp(System.Math.Abs(dot/(normA*normB)),0,1))};
  }
  public static Matrix4 CurrentTcp(Part physicalTool,Matrix4 canonicalToolTcp)=>physicalTool.Transform.GlobalMatrix*canonicalToolTcp;
  public static Matrix4 CurrentContact(Part workpiece,Matrix4 cadContact)=>workpiece.Transform.GlobalMatrix*cadContact;
  public static bool ActuallyAttached(Station station,Part tool,Part workpiece)=>station.Attachments.Any(a=>Object.ReferenceEquals(a.AttachmentParent,tool)&&Object.ReferenceEquals(a.AttachmentChild,workpiece));
  public static bool AnyActualAttachment(Station station,Part workpiece)=>station.Attachments.Any(a=>Object.ReferenceEquals(a.AttachmentChild,workpiece));
  public static bool TryAttachKeepWorld(Station station,Part tool,Part workpiece,Matrix4 canonicalToolTcp,Matrix4 cadContact,double contactPositionToleranceMetres,double contactAngleToleranceRadians,double positionToleranceMetres,double angleToleranceRadians,out Matrix4 before,out Matrix4 after,out string diagnostic){
   before=workpiece.Transform.GlobalMatrix;after=before;diagnostic="";
   if(!Error(CurrentTcp(tool,canonicalToolTcp),CurrentContact(workpiece,cadContact)).Within(contactPositionToleranceMetres,contactAngleToleranceRadians)){diagnostic="Actual TCP/contact pose is outside tolerance immediately before attachment; no attachment attempted.";return false;}
   if(AnyActualAttachment(station,workpiece)){diagnostic="Workpiece already attached; no new attachment attempted.";return false;}
   bool returned=tool.Attach(workpiece,false,new Matrix4(new Vector3(0,0,0)));after=workpiece.Transform.GlobalMatrix;
   if(!returned||!ActuallyAttached(station,tool,workpiece)||!Error(after,before).Within(positionToleranceMetres,angleToleranceRadians)){diagnostic="Attachment return/readback/world-preservation failed. Retain scene for explicit fault handling; do not assert vacuum feedback.";return false;}
   return true;
  }
  public static bool TryDetachKeepWorld(Station station,Part tool,Part workpiece,double positionToleranceMetres,double angleToleranceRadians,out Matrix4 before,out Matrix4 after,out string diagnostic){
   before=workpiece.Transform.GlobalMatrix;after=before;diagnostic="";
   if(!ActuallyAttached(station,tool,workpiece)){diagnostic="Expected actual tool/workpiece attachment is absent; no successful release claimed.";return false;}
   if(!tool.Detach(workpiece)){diagnostic="Official Detach returned false; retain scene for diagnosis.";return false;}
   // Preserve the observed release pose, never a planned fixture/box target.
   workpiece.Transform.GlobalMatrix=before;after=workpiece.Transform.GlobalMatrix;
   if(AnyActualAttachment(station,workpiece)||!Error(after,before).Within(positionToleranceMetres,angleToleranceRadians)){diagnostic="Detach or world-preservation readback failed; no successful release claimed.";return false;}
   return true;
  }
 }
 public sealed class SimulationContactDwellGate {
  string candidate;double since,last;public void Reset(){candidate=null;since=last=double.NaN;}
  // simulationTimeMs must come from the actual OnSimulationStepEndAsync callback.
  // Repeated timestamps/paused simulation do not accrue contact time; backwards time resets.
  public bool Observe(double simulationTimeMs,string uniquePartAndContact,bool vacuumCommand,bool uniqueCandidate,bool measuredPoseValid,int stableMilliseconds,int maximumSampleGapMilliseconds){
   if(stableMilliseconds<=0||maximumSampleGapMilliseconds<=0)throw new ArgumentOutOfRangeException("Positive dwell and sample-gap limits required.");
   if(!double.IsFinite(simulationTimeMs)||simulationTimeMs<0)throw new ArgumentOutOfRangeException("Actual nonnegative simulation time required.");
   if(!vacuumCommand||!uniqueCandidate||!measuredPoseValid||String.IsNullOrEmpty(uniquePartAndContact)){Reset();return false;}
   double now=simulationTimeMs;if(candidate!=uniquePartAndContact||!double.IsFinite(last)||now<last||now-last>maximumSampleGapMilliseconds){candidate=uniquePartAndContact;since=last=now;return false;}last=now;
   return now-since>=stableMilliseconds;
  }
 }
}
