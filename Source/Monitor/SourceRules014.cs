using System;
using System.IO;

namespace MA6212.Model014.Monitor {
 public readonly record struct SourceSample014(double TimeMs,double X,double Y,double Z,double AngleRad,double MinX,double MaxX,double MinY,double MaxY,double BottomZ) {
  public bool Finite=>double.IsFinite(TimeMs)&&double.IsFinite(X)&&double.IsFinite(Y)&&double.IsFinite(Z)&&double.IsFinite(AngleRad)&&double.IsFinite(MinX)&&double.IsFinite(MaxX)&&double.IsFinite(MinY)&&double.IsFinite(MaxY)&&double.IsFinite(BottomZ);
 }
 public sealed record SourceLimits014(double TargetX,double TargetY,double SurfaceZ,double BeltMinX,double BeltMaxX,double BeltMinY,double BeltMaxY,double TravelMm=1380,double MaximumSpeedMmS=460,double MaximumSampleGapMs=50,double StableMs=200) {
  public void Validate(){
   foreach(double v in new[]{TargetX,TargetY,SurfaceZ,BeltMinX,BeltMaxX,BeltMinY,BeltMaxY,TravelMm,MaximumSpeedMmS,MaximumSampleGapMs,StableMs})if(!double.IsFinite(v))throw new InvalidDataException("Finite source limits required");
   if(BeltMaxX<=BeltMinX||BeltMaxY<=BeltMinY||TravelMm!=1380||MaximumSpeedMmS!=460||MaximumSampleGapMs<=0||MaximumSampleGapMs>100||StableMs<200)throw new InvalidDataException("Explicit bounded Model014 source limits required");
  }
 }
 // Input samples must come from actual CAD and the native virtual clock, never a planned target.
 public sealed class SourceTrajectory014 {
  readonly SourceLimits014 limits;readonly double offsetX,offsetY;SourceSample014 last;double stableSince=double.NaN;
  const double MotionEpsilonMm=.001;
  public string Phase{get;private set;}="Conveying";
  public SourceSample014 Latest=>last;
  public SourceTrajectory014(SourceLimits014 values,SourceSample014 entry){
   limits=values;limits.Validate();Geometry(entry);offsetX=entry.X-limits.TargetX;offsetY=entry.Y-(limits.TargetY-limits.TravelMm);
   if(Math.Abs(offsetX)>5+1e-9||Math.Abs(offsetY)>5+1e-9)throw new InvalidDataException("Actual source entry must be within each declared +/-5 mm input range");last=entry;
  }
  void Geometry(SourceSample014 s){
   if(!s.Finite||s.TimeMs<0||s.AngleRad<0||s.AngleRad>1e-5||Math.Abs(s.Z-limits.SurfaceZ)>.1||Math.Abs(s.BottomZ-limits.SurfaceZ)>.1||s.MinX<limits.BeltMinX-1e-6||s.MaxX>limits.BeltMaxX+1e-6||s.MinY<limits.BeltMinY-1e-6||s.MaxY>limits.BeltMaxY+1e-6||s.MaxX<=s.MinX||s.MaxY<=s.MinY)throw new InvalidDataException("Actual source CAD is not upright and fully supported by the declared flat belt");
  }
  public bool AtTarget(SourceSample014 s){Geometry(s);return Math.Abs(s.X-(limits.TargetX+offsetX))<=.1&&Math.Abs(s.Y-(limits.TargetY+offsetY))<=.1;}
  public bool Supports(SourceSample014 s){
   Geometry(s);if(Math.Abs(s.X-(limits.TargetX+offsetX))>.1||s.Y<limits.TargetY-limits.TravelMm+offsetY-MotionEpsilonMm||s.Y>limits.TargetY+offsetY+MotionEpsilonMm)throw new InvalidDataException("Actual source left its independently locked lane");return true;
  }
  public void Observe(string phase,SourceSample014 s){
   if(phase!="Conveying"&&phase!="Stopped")throw new InvalidDataException("Only Conveying and Stopped source phases are valid");Supports(s);
   double dt=s.TimeMs-last.TimeMs,dx=s.X-last.X,dy=s.Y-last.Y,dz=s.Z-last.Z,dist=Math.Sqrt(dx*dx+dy*dy+dz*dz);
   if(dt<0||dt>limits.MaximumSampleGapMs)throw new InvalidDataException("Source CAD samples rewound or exceeded the bounded virtual sample gap");
   if(dt==0&&dist>1e-6)throw new InvalidDataException("Actual CAD moved without virtual time advancing");
   if(dt>0&&(dist/(dt/1000)>limits.MaximumSpeedMmS+.01||dy< -MotionEpsilonMm))throw new InvalidDataException("Actual source speed or direction exceeds the frozen conveyor rule");
   if(Phase=="Stopped"&&phase!="Stopped")throw new InvalidDataException("A stopped source cannot be silently restarted/reset");
   if(phase=="Stopped"){
    if(!AtTarget(s)||dist>MotionEpsilonMm)throw new InvalidDataException("Stopped phase requires actual target and no measured displacement");
    if(Phase!="Stopped"){if(dt<=0)throw new InvalidDataException("Stopped needs a later distinct stationary sample after the last moving sample");stableSince=s.TimeMs;}
   }
   Phase=phase;last=s;
  }
  // Between simulation observations, retain an already measured stop only while
  // the current actual CAD remains unchanged and within the existing sample gap.
  // This read never advances the measured stop duration or the latest observation.
  public bool ReadyFresh(SourceSample014 current){
   if(!Supports(current)||!AtTarget(current)||!Ready(last))return false;
   double age=current.TimeMs-last.TimeMs;
   if(age<0||age>limits.MaximumSampleGapMs)return false;
   double dx=current.X-last.X,dy=current.Y-last.Y,dz=current.Z-last.Z;
   return Math.Sqrt(dx*dx+dy*dy+dz*dz)<=1e-6;
  }
  // Read-only: time argument cannot manufacture unobserved stable time.
  public bool Ready(SourceSample014 current){
   if(!Supports(current)||Phase!="Stopped"||!AtTarget(current)||!double.IsFinite(stableSince)||Math.Abs(current.TimeMs-last.TimeMs)>1e-6)return false;
   double dx=current.X-last.X,dy=current.Y-last.Y,dz=current.Z-last.Z;
   return Math.Sqrt(dx*dx+dy*dy+dz*dz)<=1e-6&&last.TimeMs-stableSince>=limits.StableMs;
  }
 }
}
