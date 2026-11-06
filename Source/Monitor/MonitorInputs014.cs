using System;using System.IO;using System.Linq;using System.Text;using System.Text.Json;using System.Security.Cryptography;using System.Collections.Generic;
using ABB.Robotics.Math;
namespace MA6212.Model014.Monitor {
 public sealed record EquipmentDefinition014(string Name,Matrix4 Pose,IReadOnlyList<double> LocalMinMm,IReadOnlyList<double> LocalMaxMm);
 public sealed class MonitorInputs014 {
  MonitorInputs014(){}
  public string PolicyHash{get;private set;}public string SpecHash{get;private set;}public string LayoutHash{get;private set;}public string ContactsHash{get;private set;}public string SceneHash{get;private set;}
  public Matrix4 Source{get;private set;}public Matrix4 Fixture{get;private set;}public Matrix4 Ready{get;private set;}public Matrix4 Tcp{get;private set;}public Matrix4 C1{get;private set;}public Matrix4 C2{get;private set;}
  public IReadOnlyList<Matrix4> Slots{get;private set;}public IReadOnlyList<EquipmentDefinition014> Equipment{get;private set;}public SourceLimits014 SourceLimits{get;private set;}
  public double ContactPositionM{get;private set;}public double ContactAngleRad{get;private set;}public double PlacePositionM{get;private set;}public double PlaceAngleRad{get;private set;}public double ReadyPositionM{get;private set;}public double ReadyAngleRad{get;private set;}public double FixtureXYMm{get;private set;}public double FixtureZMm{get;private set;}public double FixtureAngleRad{get;private set;}
  public int MaximumParts{get;private set;}public bool AllowFeed{get;private set;}public bool AllowClear{get;private set;}public IReadOnlyList<double> MaterialMinMm{get;private set;}public IReadOnlyList<double> MaterialMaxMm{get;private set;}
  public static string HashText(string text)=>Convert.ToHexString(SHA256.HashData(new UTF8Encoding(false).GetBytes(text))).ToLowerInvariant();
  public static string HashFile(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
  static double[] N(JsonElement e)=>e.EnumerateArray().Select(x=>x.GetDouble()).ToArray();
  static double Number(JsonElement r,string name,double maximum){double v=r.GetProperty(name).GetDouble();if(!double.IsFinite(v)||v<=0||v>maximum)throw new InvalidDataException("Invalid or loosened frozen threshold: "+name);return v;}
  static Matrix4 Pose(double[] p,double[] q){if(p.Length!=3||q.Length!=4||p.Concat(q).Any(v=>!double.IsFinite(v))||Math.Abs(q.Sum(v=>v*v)-1)>1e-7)throw new InvalidDataException("Finite mm/wxyz pose required");return new Matrix4(new Vector3(p[0]/1000,p[1]/1000,p[2]/1000),new Quaternion(q));}
  static Matrix4 Pose(JsonElement e)=>Pose(N(e.GetProperty("position_mm")),N(e.GetProperty("quaternion_wxyz")));
  public static MonitorInputs014 Load(string policy,string expectedPolicySha,string spec,string layout,string contacts,string scene){
   if(new[]{policy,spec,layout,contacts,scene}.Any(p=>!Path.IsPathRooted(p??"")))throw new InvalidDataException("Absolute root-owned input paths required");
   var x=new MonitorInputs014{PolicyHash=HashFile(policy),SpecHash=HashFile(spec),LayoutHash=HashFile(layout),ContactsHash=HashFile(contacts),SceneHash=HashFile(scene)};
   if(!String.Equals(x.PolicyHash,expectedPolicySha,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Root supplied policy hash mismatch");
   using var p=JsonDocument.Parse(File.ReadAllText(policy));var r=p.RootElement;
   if(r.GetProperty("status").GetString()!="FROZEN_FOR_MODEL014_EXECUTION"||r.GetProperty("policy_version").GetInt32()!=14||r.GetProperty("runtime_namespace").GetString()!="MA6212.Model014.Actor"||r.GetProperty("monitor_runtime_namespace").GetString()!="MA6212.Model014.Monitor")throw new InvalidDataException("New root-frozen Model014 policy required; old approval cannot be reused");
   foreach(var v in new[]{("model014_source_spec_sha256",x.SpecHash),("canonical_layout_sha256",x.LayoutHash),("model014_contacts_sha256",x.ContactsHash),("model014_scene_sha256",x.SceneHash)})if(!String.Equals(r.GetProperty(v.Item1).GetString(),v.Item2,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Frozen policy/input hash mismatch: "+v.Item1);
   x.ContactPositionM=Number(r,"contact_position_tolerance_mm",1)/1000;x.ContactAngleRad=Number(r,"contact_orientation_tolerance_deg",2)*Math.PI/180;
   x.PlacePositionM=Number(r,"placement_position_tolerance_mm",1)/1000;x.PlaceAngleRad=Number(r,"placement_orientation_tolerance_deg",2)*Math.PI/180;
   x.ReadyPositionM=Number(r,"ready_position_tolerance_mm",5)/1000;x.ReadyAngleRad=Number(r,"ready_orientation_tolerance_deg",3)*Math.PI/180;
   x.FixtureXYMm=Number(r,"fixture_insert_xy_norm_tolerance_mm",.6);x.FixtureZMm=Number(r,"fixture_insert_z_tolerance_mm",.1);x.FixtureAngleRad=Number(r,"fixture_insert_angle_tolerance_rad",1e-5);
   x.MaximumParts=r.GetProperty("maximum_parts").GetInt32();x.AllowFeed=r.GetProperty("allow_material_feeding").GetBoolean();x.AllowClear=r.GetProperty("allow_nonempty_box_clear").GetBoolean();if(x.MaximumParts<1||x.MaximumParts>20)throw new InvalidDataException("Frozen batch bound1..20 required");
   using var l=JsonDocument.Parse(File.ReadAllText(layout));var a=l.RootElement;x.Source=Pose(a.GetProperty("part_poses").GetProperty("source"));x.Fixture=Pose(a.GetProperty("part_poses").GetProperty("fixture"));x.Ready=Pose(a.GetProperty("motion").GetProperty("ready_tcp_world"));var t=a.GetProperty("tool");x.Tcp=Pose(N(t.GetProperty("tcp_translation_flange_mm")),N(t.GetProperty("tcp_quaternion_flange_wxyz")));
   using var c=JsonDocument.Parse(File.ReadAllText(contacts));var cs=c.RootElement.GetProperty("models")[0].GetProperty("contacts").EnumerateArray().ToArray();Matrix4 C(string name){var cc=cs.Single(z=>z.GetProperty("name").GetString()==name);return Pose(N(cc.GetProperty("position_part_mm")),N(cc.GetProperty("tcp_quaternion_wxyz_part")));}x.C1=C("C_pick1_side");x.C2=C("C_pick2_outer_bend");
   using var s=JsonDocument.Parse(File.ReadAllText(spec));var root=s.RootElement;var all=root.GetProperty("parts").EnumerateArray().ToArray();var names=new HashSet<string>(StringComparer.Ordinal);var eq=new List<EquipmentDefinition014>();
   foreach(var item in all){string n=item.GetProperty("name").GetString();if(!names.Add(n))throw new InvalidDataException("Duplicate spec Part name");if(n=="SourcePart"){x.MaterialMinMm=N(item.GetProperty("bbox_min_mm"));x.MaterialMaxMm=N(item.GetProperty("bbox_max_mm"));}if(n=="SourcePart"||n=="FixturePart"||n=="VacuumTool"||n.StartsWith("BoxPart_",StringComparison.Ordinal))continue;eq.Add(new EquipmentDefinition014(n,Pose(N(item.GetProperty("world_position_mm")),N(item.GetProperty("quaternion_wxyz"))),N(item.GetProperty("bbox_min_mm")),N(item.GetProperty("bbox_max_mm"))));}
   foreach(var item in root.GetProperty("boxes").EnumerateArray()){string n=item.GetProperty("name").GetString();if(!names.Add(n))throw new InvalidDataException("Duplicate spec box name");eq.Add(new EquipmentDefinition014(n,Pose(N(item.GetProperty("position_mm")),new double[]{1,0,0,0}),new double[]{0,0,0},N(item.GetProperty("size_mm"))));}
   if(eq.Count!=28||eq.Count(e=>e.Name=="Floor")!=1||x.MaterialMinMm==null)throw new InvalidDataException("Exact reviewed28-equipment Model014 inventory required");x.Equipment=Array.AsReadOnly(eq.Select(e=>new EquipmentDefinition014(e.Name,e.Pose,Array.AsReadOnly(e.LocalMinMm.ToArray()),Array.AsReadOnly(e.LocalMaxMm.ToArray()))).ToArray());x.MaterialMinMm=Array.AsReadOnly(x.MaterialMinMm.ToArray());x.MaterialMaxMm=Array.AsReadOnly(x.MaterialMaxMm.ToArray());
   x.Slots=Array.AsReadOnly(Enumerable.Range(1,6).Select(i=>{var z=all.Single(q=>q.GetProperty("name").GetString()=="BoxPart_"+i);return Pose(N(z.GetProperty("world_position_mm")),N(z.GetProperty("quaternion_wxyz")));}).ToArray());
   foreach(int i in Enumerable.Range(1,6))foreach(string side in new[]{"PositiveY","NegativeY"})if(eq.Count(z=>z.Name=="PackingCradle_"+side+"_BoxPart_"+i)!=1)throw new InvalidDataException("Missing assigned-slot packing support");
   var belt=eq.Single(e=>e.Name=="ConveyorSurface");var bp=belt.Pose.Translation;var sp=x.Source.Translation;
   if(belt.Pose.Elements.Take(12).Zip(Matrix4.Identity.Elements.Take(12),(v,w)=>Math.Abs(v-w)).Max()>1e-10||x.Source.Elements.Take(12).Zip(Matrix4.Identity.Elements.Take(12),(v,w)=>Math.Abs(v-w)).Max()>1e-10)throw new InvalidDataException("This conveyor supports identity axes only");
   double gap=Number(r,"maximum_virtual_sample_gap_ms",100),speed=r.GetProperty("conveyor_speed_mm_s").GetDouble(),settled=r.GetProperty("conveyor_settled_duration_ms").GetDouble();
   x.SourceLimits=new SourceLimits014(sp.x*1000,sp.y*1000,bp.z*1000+belt.LocalMaxMm[2],bp.x*1000+belt.LocalMinMm[0],bp.x*1000+belt.LocalMaxMm[0],bp.y*1000+belt.LocalMinMm[1],bp.y*1000+belt.LocalMaxMm[1],1380,speed,gap,settled);x.SourceLimits.Validate();
   return x;
  }
 }
}
