// Pure virtual-time supply clock. No SDK, wall clock, signals or model mutation.
using System;
namespace MA6212.Model014.Actor {
 public sealed class ConveyorTiming014 {
  public const double LengthMm=300,SpeedMmPerSecond=100,MaximumStepMs=20,SettledMs=200;
  public double DistanceMm{get;private set;}public bool Started{get;private set;}
  double last=double.NaN,stoppedSince=double.NaN,stoppedLast=double.NaN;
  public double Advance(double time,bool requested,bool ready){
   if(!double.IsFinite(time)||time<0)throw new ArgumentOutOfRangeException(nameof(time));
   double dt=double.IsFinite(last)?time-last:0;
   if(dt<0||dt>MaximumStepMs+1e-6)throw new InvalidOperationException("Supply virtual timestamp reversed or exceeded20ms; no catch-up jump.");
   last=time;
   if(!Started){if(requested&&ready)Started=true;return DistanceMm;}
   if(DistanceMm<LengthMm){if(!requested||!ready)throw new InvalidOperationException("Supply request/Ready lost during transport; retain actual pose.");DistanceMm=System.Math.Min(LengthMm,DistanceMm+SpeedMmPerSecond*dt/1000);}
   return DistanceMm;
  }
  public bool ObserveStopped(double time,bool actualAtTargetAndStationary){
   if(!actualAtTargetAndStationary){stoppedSince=stoppedLast=double.NaN;return false;}
   if(!double.IsFinite(stoppedLast)||time<stoppedLast||time-stoppedLast>MaximumStepMs+1e-6)stoppedSince=time;
   stoppedLast=time;return time-stoppedSince>=SettledMs;
  }
 }
}
