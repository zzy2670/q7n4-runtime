using System;
using System.Linq;
namespace MA6212.Model014.Portable6Supervisor {
 // Independent stationary samples: each source must remain at its own first observed pose.
 public sealed class StopStability014 {
  double[] controller,native;double lastWall=Double.NegativeInfinity;int count;
  static double Delta(double[] a,double[] b)=>a.Length==6&&b.Length==6&&a.Concat(b).All(Double.IsFinite)?a.Zip(b,(x,y)=>Math.Abs(x-y)).Max():Double.PositiveInfinity;
  public bool Observe(double wallMs,bool nativeStopped,bool rapidStopped,bool corresponding,double[] controllerQ,double[] nativeQ){
   if(!Double.IsFinite(wallMs)||wallMs<=lastWall||controllerQ.Length!=6||nativeQ.Length!=6||controllerQ.Concat(nativeQ).Any(v=>!Double.IsFinite(v)))throw new ArgumentException("Finite monotonic actual stationary sample required");
   if(!nativeStopped||!rapidStopped||!corresponding){count=0;controller=native=null;lastWall=wallMs;return false;}
   if(controller!=null&&wallMs-lastWall<250)throw new ArgumentException("Independent stop samples require250ms actual wall gap");
   if(controller==null||Delta(controllerQ,controller)>1e-7||Delta(nativeQ,native)>1e-9){controller=controllerQ.ToArray();native=nativeQ.ToArray();count=0;}
   lastWall=wallMs;return ++count>=3;
  }
 }
}
