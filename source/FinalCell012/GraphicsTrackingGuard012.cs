// Pure production decision logic. No ABB/runtime calls.
using System;
namespace MA6212.Commissioning {
 public sealed class GraphicsTrackingGuard012 {
  double badSince=double.NaN;
  public readonly record struct Decision(bool Stop,double BadAgeMs,double ModelAgeMs,string Reason);
  public Decision Observe(double nowMs,double mismatchRad,bool modelProgress,double lastModelMs){
   if(!double.IsFinite(nowMs)||!double.IsFinite(mismatchRad)||!double.IsFinite(lastModelMs)||mismatchRad<0||lastModelMs>nowMs)throw new ArgumentException("Finite monotonic tracking measurements required");
   bool bad=mismatchRad>Math.PI/1800;
   if(!bad||modelProgress)badSince=double.NaN;
   if(bad&&double.IsNaN(badSince))badSince=nowMs;
   double badAge=double.IsNaN(badSince)?0:nowMs-badSince,modelAge=nowMs-lastModelMs;
   bool immediate=mismatchRad>Math.PI/36;
   // A stationary in-position robot is valid. Small mismatches require stale motion; a mismatch over five degrees stops immediately.
   bool sustained=bad&&badAge>300&&modelAge>300;
   return new Decision(immediate||sustained,badAge,modelAge,immediate?"IMMEDIATE_OVER_5DEG":sustained?"SUSTAINED_OVER_0_1DEG_AND_NO_MODEL_PROGRESS_300MS":"TRACKING_WITHIN_GUARD");
  }
 }
}
