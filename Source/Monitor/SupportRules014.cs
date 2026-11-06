using System;
namespace MA6212.Model014.Monitor {
 public static class SupportRules014 {
  public static bool FixtureWithin(double dxMm,double dyMm,double dzMm,double angleRad,double xyLimitMm=.6,double zLimitMm=.1,double angleLimitRad=1e-5){
   if(!double.IsFinite(dxMm)||!double.IsFinite(dyMm)||!double.IsFinite(dzMm)||!double.IsFinite(angleRad)||angleRad<0||!double.IsFinite(xyLimitMm)||xyLimitMm<=0||xyLimitMm>.6||!double.IsFinite(zLimitMm)||zLimitMm<=0||zLimitMm>.1||!double.IsFinite(angleLimitRad)||angleLimitRad<=0||angleLimitRad>1e-5)return false;
   return Math.Sqrt(dxMm*dxMm+dyMm*dyMm)<=xyLimitMm&&Math.Abs(dzMm)<=zLimitMm&&angleRad<=angleLimitRad;
  }
  public static bool AssignedBoxObstacle(string obstacle,int zeroBasedSlot)=>zeroBasedSlot>=0&&zeroBasedSlot<6&&(obstacle=="PackingBoxFloor"||obstacle=="PackingCradle_PositiveY_BoxPart_"+(zeroBasedSlot+1)||obstacle=="PackingCradle_NegativeY_BoxPart_"+(zeroBasedSlot+1));
 }
}
