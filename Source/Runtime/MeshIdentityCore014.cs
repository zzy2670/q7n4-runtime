// CPU-only complete triangle comparison. Millimetres; no SDK or model mutation.
using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Globalization;using System.Security.Cryptography;using System.Text;
namespace MA6212.Model014.MeshPreflight {
 public readonly record struct Vertex014(double X,double Y,double Z){
  public double[] Values=>new[]{X,Y,Z};public bool Finite=>double.IsFinite(X)&&double.IsFinite(Y)&&double.IsFinite(Z);
  public static Vertex014 operator +(Vertex014 a,Vertex014 b)=>new(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
  public static Vertex014 operator -(Vertex014 a,Vertex014 b)=>new(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
  public static Vertex014 operator *(Vertex014 a,double b)=>new(a.X*b,a.Y*b,a.Z*b);
  public double Norm=>Math.Sqrt(X*X+Y*Y+Z*Z);
 }
 public sealed record Triangle014(Vertex014 A,Vertex014 B,Vertex014 C){public Vertex014[] Vertices=>new[]{A,B,C};public Vertex014 Centre=>(A+B+C)*(1.0/3);}
 public sealed record Match014(int SourceCount,int ActualCount,int MatchedCount,double MaximumVertexErrorMm,int ReversedWindingMatches,string SourceTriangleFingerprint,string ActualTriangleFingerprint);
 public sealed record BoxMatch014(int TriangleCount,double MaximumComparisonCorrectionMm,double[] CoveredFaceAreasMm2,double[] ExpectedFaceAreasMm2,double MaximumPositiveOverlapMm2);
 public static class MeshIdentityCore014 {
  static readonly int[][] Permutations={new[]{0,1,2},new[]{1,2,0},new[]{2,0,1},new[]{0,2,1},new[]{2,1,0},new[]{1,0,2}};
  static double CrossNorm(Triangle014 t){var u=t.B-t.A;var v=t.C-t.A;return new Vertex014(u.Y*v.Z-u.Z*v.Y,u.Z*v.X-u.X*v.Z,u.X*v.Y-u.Y*v.X).Norm;}
  public static void Validate(IReadOnlyList<Triangle014> mesh){if(mesh==null||mesh.Count==0)throw new InvalidDataException("Complete nonempty triangle mesh required");foreach(var t in mesh)if(t.Vertices.Any(v=>!v.Finite||v.Values.Any(c=>Math.Abs(c)>1e9))||CrossNorm(t)<=1e-10)throw new InvalidDataException("Nonfinite/out-of-range/degenerate actual triangle");}
  public static List<Triangle014> ReadBinaryStl(byte[] data){
   if(data==null||data.Length<84)throw new InvalidDataException("Binary STL required");uint n=BitConverter.ToUInt32(data,80);if(n==0||data.Length!=84L+50L*n)throw new InvalidDataException("Binary STL byte length/triangle count mismatch");
   var result=new List<Triangle014>((int)n);for(int i=0;i<n;i++){int p=84+50*i+12;Vertex014 V(int j)=>new(BitConverter.ToSingle(data,p+12*j),BitConverter.ToSingle(data,p+12*j+4),BitConverter.ToSingle(data,p+12*j+8));result.Add(new Triangle014(V(0),V(1),V(2)));}Validate(result);return result;
  }
  public static string Hash(byte[] data)=>Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
  public static string TriangleFingerprint(IReadOnlyList<Triangle014> mesh){
   string F(Vertex014 v)=>string.Join(",",v.Values.Select(x=>(x==0?0:x).ToString("R",CultureInfo.InvariantCulture)));
   var lines=mesh.Select(t=>string.Join(";",t.Vertices.OrderBy(v=>v.X).ThenBy(v=>v.Y).ThenBy(v=>v.Z).Select(F))).OrderBy(x=>x,StringComparer.Ordinal);
   return Hash(Encoding.UTF8.GetBytes(string.Join("\n",lines)));
  }
  public static Match014 MatchStl(IReadOnlyList<Triangle014> source,IReadOnlyList<Triangle014> actual,double toleranceMm=.001){
   if(!double.IsFinite(toleranceMm)||toleranceMm<=0||toleranceMm>.001)throw new InvalidDataException("Positive triangle tolerance<=0.001mm required");Validate(source);Validate(actual);
   if(source.Count!=actual.Count)throw new InvalidDataException("Triangle multiplicity differs; source/native retessellation equivalence is not established.");
   (long,long,long) Key(Vertex014 c)=>(checked((long)Math.Floor(c.X/toleranceMm)),checked((long)Math.Floor(c.Y/toleranceMm)),checked((long)Math.Floor(c.Z/toleranceMm)));
   var buckets=new Dictionary<(long,long,long),List<int>>();for(int i=0;i<source.Count;i++){var key=Key(source[i].Centre);if(!buckets.TryGetValue(key,out var list))buckets[key]=list=new();list.Add(i);}
   var used=new bool[source.Count];double maximum=0;int matched=0,reversed=0;
   foreach(var t in actual){var key=Key(t.Centre);var av=t.Vertices;int best=-1,bestPermutation=-1;double bestError=double.PositiveInfinity;
    for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)for(int z=-1;z<=1;z++)if(buckets.TryGetValue((key.Item1+x,key.Item2+y,key.Item3+z),out var candidates))foreach(int index in candidates){if(used[index])continue;var sv=source[index].Vertices;for(int p=0;p<6;p++){double error=Enumerable.Range(0,3).Max(k=>(av[k]-sv[Permutations[p][k]]).Norm);if(error<bestError){bestError=error;best=index;bestPermutation=p;}}}
    if(best<0||bestError>toleranceMm)throw new InvalidDataException("No unused source triangle matches complete native triangle#"+matched+" within tolerance; bbox/hash existence is insufficient.");
    used[best]=true;matched++;maximum=Math.Max(maximum,bestError);if(bestPermutation>=3)reversed++;
   }
   if(used.Any(v=>!v))throw new InvalidDataException("Unmatched source triangles");return new Match014(source.Count,actual.Count,matched,maximum,reversed,TriangleFingerprint(source),TriangleFingerprint(actual));
  }
  readonly record struct P2(double X,double Y);
  static double Cross(P2 a,P2 b,P2 p)=>(b.X-a.X)*(p.Y-a.Y)-(b.Y-a.Y)*(p.X-a.X);
  static double SignedArea(IReadOnlyList<P2> p){double sum=0;for(int i=0;i<p.Count;i++){var a=p[i];var b=p[(i+1)%p.Count];sum+=a.X*b.Y-a.Y*b.X;}return sum/2;}
  static double Overlap(P2[] first,P2[] second){
   var output=first.ToList();var clip=SignedArea(second)>=0?second:second.Reverse().ToArray();
   for(int edge=0;edge<3&&output.Count>0;edge++){var a=clip[edge];var b=clip[(edge+1)%3];var input=output.ToArray();output=new();for(int i=0;i<input.Length;i++){var p=input[i];var q=input[(i+1)%input.Length];double cp=Cross(a,b,p),cq=Cross(a,b,q);bool insideP=cp>=-1e-10,insideQ=cq>=-1e-10;if(insideP)output.Add(p);if(insideP!=insideQ){double f=cp/(cp-cq);output.Add(new P2(p.X+f*(q.X-p.X),p.Y+f*(q.Y-p.Y)));}}}
   return output.Count<3?0:Math.Abs(SignedArea(output));
  }
  // Primitive boxes have dimensions, not source STL files. Prove all complete
  // native triangles cover exactly the six analytic faces without positive overlap.
  public static BoxMatch014 MatchBox(IReadOnlyList<Triangle014> actual,double[] sizeMm,double toleranceMm=.001){
   Validate(actual);if(sizeMm==null||sizeMm.Length!=3||sizeMm.Any(x=>!double.IsFinite(x)||x<=0)||!double.IsFinite(toleranceMm)||toleranceMm<=0||toleranceMm>.001)throw new InvalidDataException("Finite box dimensions and<=1um comparison tolerance required");
   var faces=Enumerable.Range(0,6).Select(_=>new List<P2[]>()).ToArray();double maxCorrection=0,axisTolerance=toleranceMm/Math.Sqrt(3);
   foreach(var tri in actual){var vertices=tri.Vertices;int face=-1;
    for(int axis=0;axis<3;axis++)for(int side=0;side<2;side++){double plane=side*sizeMm[axis];if(vertices.All(v=>Math.Abs(v.Values[axis]-plane)<=axisTolerance)){if(face!=-1)throw new InvalidDataException("Ambiguous thin/degenerate box facet");face=2*axis+side;}}
    if(face<0)throw new InvalidDataException("Actual primitive triangle is not on a declared box face");int normalAxis=face/2;int[] uv=Enumerable.Range(0,3).Where(i=>i!=normalAxis).ToArray();var projected=new P2[3];
    for(int i=0;i<3;i++){double[] old=vertices[i].Values,v=(double[])old.Clone();v[normalAxis]=(face%2)*sizeMm[normalAxis];foreach(int a in uv){if(Math.Abs(v[a])<=axisTolerance)v[a]=0;else if(Math.Abs(v[a]-sizeMm[a])<=axisTolerance)v[a]=sizeMm[a];if(v[a]<0||v[a]>sizeMm[a])throw new InvalidDataException("Box facet extends beyond analytic face rectangle");}double correction=Math.Sqrt(v.Zip(old,(a,b)=>(a-b)*(a-b)).Sum());maxCorrection=Math.Max(maxCorrection,correction);if(correction>toleranceMm+1e-12)throw new InvalidDataException("Comparison normalization exceeds declared tolerance");projected[i]=new P2(v[uv[0]],v[uv[1]]);}
    if(Math.Abs(SignedArea(projected))<=1e-10)throw new InvalidDataException("Degenerate projected box triangle");faces[face].Add(projected);
   }
   double[] areas=new double[6],expected=new double[6];double maximumOverlap=0;
   for(int face=0;face<6;face++){int[] uv=Enumerable.Range(0,3).Where(i=>i!=face/2).ToArray();expected[face]=sizeMm[uv[0]]*sizeMm[uv[1]];areas[face]=faces[face].Sum(p=>Math.Abs(SignedArea(p)));double areaEpsilon=Math.Max(1e-6,expected[face]*1e-10);
    if(Math.Abs(areas[face]-expected[face])>areaEpsilon)throw new InvalidDataException("Incomplete or excessive box face coverage#"+face);
    for(int i=0;i<faces[face].Count;i++)for(int j=i+1;j<faces[face].Count;j++){double overlap=Overlap(faces[face][i],faces[face][j]);maximumOverlap=Math.Max(maximumOverlap,overlap);if(overlap>Math.Max(1e-6,expected[face]*1e-12))throw new InvalidDataException("Overlapping primitive facets can hide a missing box region");}
   }
   return new BoxMatch014(actual.Count,maxCorrection,areas,expected,maximumOverlap);
  }
 }
}
