using System;using System.IO;
namespace MA6212.Model014.Monitor {
 // Accumulates native local vertices in metres; empty or nonfinite meshes fail closed.
 internal sealed class MeshLocalBounds014 {
  public double[] Min{get;}={double.PositiveInfinity,double.PositiveInfinity,double.PositiveInfinity};
  public double[] Max{get;}={double.NegativeInfinity,double.NegativeInfinity,double.NegativeInfinity};
  public int Count{get;private set;}
  public void Add(double x,double y,double z){var v=new[]{x,y,z};for(int i=0;i<3;i++){if(!double.IsFinite(v[i]))throw new InvalidDataException("Nonfinite actual mesh vertex");Min[i]=Math.Min(Min[i],v[i]);Max[i]=Math.Max(Max[i],v[i]);}Count++;}
 }
}
