using System.Collections.Generic;using System;using System.IO;using System.Linq;using System.Text;using System.Text.Json;using System.Globalization;
namespace MA6212.Model014.Portable6Supervisor {
 public sealed class TrackingGate014 {double since=double.NaN,last=double.NaN;public void Observe(double ms,double mismatch)=>Observe(ms,mismatch,true);public void Observe(double ms,double mismatch,bool stationary){if(!double.IsFinite(ms)||!double.IsFinite(mismatch)||ms<0||mismatch<0||double.IsFinite(last)&&ms<last)throw new InvalidDataException("Invalid tracking sample");last=ms;if(mismatch>Math.PI/36)throw new InvalidDataException("Actual joint mismatch>5deg");if(!stationary){since=double.NaN;return;}if(mismatch>Math.PI/1800){if(!double.IsFinite(since))since=ms;if(ms-since>=300)throw new InvalidDataException("Stationary joint mismatch>0.1deg persisted300ms");}else since=double.NaN;}}
 public static class RpcSettlementRules014 {public static bool MayReturn(bool startPending,bool stopPending,bool readPending,bool actualStopped,bool permittedState)=>!startPending&&!stopPending&&!readPending&&actualStopped&&permittedState;}
 public static class WorkObjectRules014 {
  static bool Identity(double[] p,double[] q)=>p!=null&&q!=null&&p.Length==3&&q.Length==4&&p.Concat(q).All(double.IsFinite)&&p.All(v=>Math.Abs(v)<=1e-9)&&Math.Abs(Math.Abs(q[0])-1)<=1e-9&&q.Skip(1).All(v=>Math.Abs(v)<=1e-9);
  public static bool WorldEquivalent(bool robhold,bool ufprog,string ufmec,double[] up,double[] uq,double[] op,double[] oq)=>!robhold&&ufprog&&string.IsNullOrEmpty(ufmec)&&Identity(up,uq)&&Identity(op,oq);
 }
 public sealed class OnePieceLedgerContext014 {public string Id,PriorHash;public string[] PriorIds;public double[] Fixture,Source;public double[][] Slots;public int ExpectedPieces,NumericRun;public double ContactM,ContactRad,PlaceM,PlaceRad,WorldM,WorldRad;public double FixtureXYM,FixtureZM,FixtureRad;}
 public sealed record OnePieceLedgerResult014(string[] Ids,int ChainEvents,int CompleteRows,int TrailingUnsealedBytes,double[][] AcceptedWorlds,double CycleSeconds);
 public static class OnePieceLedger014 {
  static double[] Quaternion(double[] a){double m00=a[0],m11=a[5],m22=a[10],w,x,y,z,t=m00+m11+m22;if(t>0){double s=Math.Sqrt(t+1)*2;w=.25*s;x=(a[6]-a[9])/s;y=(a[8]-a[2])/s;z=(a[1]-a[4])/s;}else if(m00>m11&&m00>m22){double s=Math.Sqrt(1+m00-m11-m22)*2;w=(a[6]-a[9])/s;x=.25*s;y=(a[4]+a[1])/s;z=(a[8]+a[2])/s;}else if(m11>m22){double s=Math.Sqrt(1+m11-m00-m22)*2;w=(a[8]-a[2])/s;x=(a[4]+a[1])/s;y=.25*s;z=(a[9]+a[6])/s;}else{double s=Math.Sqrt(1+m22-m00-m11)*2;w=(a[1]-a[4])/s;x=(a[8]+a[2])/s;y=(a[9]+a[6])/s;z=.25*s;}var q=new[]{w,x,y,z};double norm=Math.Sqrt(q.Sum(v=>v*v));if(!double.IsFinite(norm)||norm<1e-12)throw new InvalidDataException("Valid measured rotation required");return q.Select(v=>v/norm).ToArray();}
  public static (double M,double Rad) Error(double[] a,double[] b){if(a.Length!=16||b.Length!=16||a.Concat(b).Any(v=>!double.IsFinite(v)))throw new InvalidDataException("Finite16element measured matrices required");var q=Quaternion(a);var r=Quaternion(b);double dot=q.Zip(r,(x,y)=>x*y).Sum();return(Math.Sqrt(new[]{12,13,14}.Sum(i=>(a[i]-b[i])*(a[i]-b[i]))),2*Math.Acos(Math.Clamp(Math.Abs(dot),0,1)));}
  static void Within(double[] a,double[] b,double m,double rad){var e=Error(a,b);if(e.M>m||e.Rad>rad)throw new InvalidDataException("Measured matrix outside threshold");}
  static double[] Matrix(JsonElement e){var a=e.EnumerateArray().Select(x=>x.GetDouble()).ToArray();if(a.Length!=16||a.Any(x=>!double.IsFinite(x)))throw new InvalidDataException("Finite actual matrix required");return a;}
  static Guid Id(string s)=>Guid.TryParse(s,out var id)&&id!=Guid.Empty?id:throw new InvalidDataException("Nonzero actual GUID required");
  sealed class Piece {public Guid Id;public int Sequence,Chain;public bool SourceReady;public double LastDistance=-1;public double[] Accepted;public Vision Camera;public bool Committed;}
  sealed record Vision(double Dx,double Dy,double Time,double[] World);
  public static OnePieceLedgerResult014 Verify(byte[] capture,string csv,OnePieceLedgerContext014 ctx){
   if(ctx.ExpectedPieces!=6||ctx.Slots?.Length!=6||ctx.Source?.Length!=16)throw new InvalidDataException("Six actual slot/source definitions required");
   var first=Id(ctx.Id);var prior=ctx.PriorIds.Select(Id).ToHashSet();if(prior.Contains(first))throw new InvalidDataException("Prior material ID reused");
   int end=Array.LastIndexOf(capture,(byte)'\n')+1;if(end==0)throw new InvalidDataException("No complete Actor records");var lines=new UTF8Encoding(false,true).GetString(capture,0,end).Split('\n',StringSplitOptions.RemoveEmptyEntries);
   var pieces=new Dictionary<Guid,Piece>();int starts=0,acceptedCount=0,totalEvents=0;double lastTime=-1;
   foreach(string line in lines){using var doc=JsonDocument.Parse(line);var r=doc.RootElement;string kind=r.GetProperty("kind").GetString();double time=r.GetProperty("virtualTimeMs").GetDouble();if(!double.IsFinite(time)||time<lastTime)throw new InvalidDataException("Actor clock invalid/rewound");lastTime=time;
    var detail=r.GetProperty("detail");var parts=r.GetProperty("parts").EnumerateArray().ToArray();
    if(kind=="fault")throw new InvalidDataException("Actor fault recorded");
    if(kind=="simulation_start"){
     if(++starts!=1||pieces.Count!=0||parts.Length!=1||Id(parts[0].GetProperty("id").GetString())!=first||detail.GetProperty("priorAcceptedEvidenceSha256").GetString()!=ctx.PriorHash||!detail.GetProperty("priorAcceptedPartIds").EnumerateArray().Select(v=>Id(v.GetString())).ToHashSet().SetEquals(prior))throw new InvalidDataException("Fresh real lifecycle/prior identities required");
     pieces.Add(first,new Piece{Id=first,Sequence=1});
    }
    if(kind=="actual_cad_feed_created"){
     var id=Id(detail.GetProperty("id").GetString());int seq=detail.GetProperty("sequence").GetInt32();if(starts!=1||seq!=pieces.Count+1||seq>ctx.ExpectedPieces||pieces.ContainsKey(id)||prior.Contains(id))throw new InvalidDataException("Each feed must create a new actual material identity");pieces.Add(id,new Piece{Id=id,Sequence=seq});
    }
    if(starts!=1||parts.Length!=pieces.Count||parts.Select(v=>Id(v.GetProperty("id").GetString())).Distinct().Count()!=parts.Length||parts.Any(v=>!pieces.ContainsKey(Id(v.GetProperty("id").GetString()))))throw new InvalidDataException("Actual active material IDs disappeared, duplicated or appeared without feed");
    if(kind=="actual_box_items_removed_from_active_station"&&detail.GetProperty("items").GetArrayLength()!=0)throw new InvalidDataException("Six accepted materials must remain in the box");
    if(kind=="source_motion_actual_sample"){
     var p=pieces[Id(detail.GetProperty("id").GetString())];double dist=detail.GetProperty("distanceMm").GetDouble(),speed=detail.GetProperty("measuredSpeedMmS").GetDouble();if(!double.IsFinite(dist)||!double.IsFinite(speed)||dist<p.LastDistance||dist>1380.000001||speed<0||speed>460.001)throw new InvalidDataException("Actual conveyor sample invalid");p.LastDistance=dist;
     if(detail.GetProperty("actorStopped200Ms").GetBoolean()){if(dist<1380||speed>.01)throw new InvalidDataException("False settled source");p.SourceReady=true;}
    }
    if(kind=="simulated_vision_actual_cad_capture"||kind=="simulated_vision_rapid_packet_committed"){
     var id=Id(detail.GetProperty("PartId").GetString());var p=pieces[id];var m=Matrix(detail.GetProperty("MeasuredWorldM"));double dx=detail.GetProperty("DxMm").GetDouble(),dy=detail.GetProperty("DyMm").GetDouble(),stamp=detail.GetProperty("VirtualTimeMs").GetDouble();
     if(!p.SourceReady||detail.GetProperty("Sequence").GetInt32()!=p.Sequence||detail.GetProperty("Frame").GetString()!="station_world"||!double.IsFinite(dx)||!double.IsFinite(dy)||Math.Abs(dx)>5.000001||Math.Abs(dy)>5.000001||stamp>time||time-stamp>2000||Math.Abs(1000*(m[12]-ctx.Source[12])-dx)>1e-6||Math.Abs(1000*(m[13]-ctx.Source[13])-dy)>1e-6)throw new InvalidDataException("Vision must describe this actual stationary CAD identity/frame/time");
     var actual=parts.Single(v=>Id(v.GetProperty("id").GetString())==id);Within(Matrix(actual.GetProperty("actualWorld")),m,ctx.WorldM,ctx.WorldRad);
     if(kind=="simulated_vision_actual_cad_capture"){if(p.Camera!=null)throw new InvalidDataException("Duplicate camera capture");p.Camera=new Vision(dx,dy,stamp,m);}else{if(p.Camera==null||p.Committed||p.Camera.Dx!=dx||p.Camera.Dy!=dy||p.Camera.Time!=stamp)throw new InvalidDataException("Committed packet differs from real camera capture");Within(p.Camera.World,m,ctx.WorldM,ctx.WorldRad);p.Committed=true;}
    }
    if(kind=="actual_contact_and_attachment_verified"||kind=="release_fixture_geometry_verified"||kind=="release_box_geometry_verified"){
     var id=Id(detail.GetProperty("id").GetString());var p=pieces[id];var part=parts.Single(v=>Id(v.GetProperty("id").GetString())==id);string wanted=p.Chain switch{0=>"actual_contact_and_attachment_verified",1=>"release_fixture_geometry_verified",2=>"actual_contact_and_attachment_verified",3=>"release_box_geometry_verified",_=>null};
     if(!p.SourceReady||!p.Committed||kind!=wanted||p.Sequence!=acceptedCount+1)throw new InvalidDataException("Real C1/fixture/C2/box chain or material sequence missing");bool attach=p.Chain==0||p.Chain==2;string contact=p.Chain<2?"C1":"C2";
     if(attach&&detail.GetProperty("contact").GetString()!=contact||part.GetProperty("attachedToTool").GetBoolean()!=attach||part.GetProperty("Phase").GetString()!=(attach?"Held":p.Chain==1?"AtFixture":"Packed"))throw new InvalidDataException("Actual attachment/release phase mismatch");
     var before=Matrix(detail.GetProperty("actualBefore"));var after=Matrix(detail.GetProperty("actualAfter"));Within(before,after,ctx.WorldM,ctx.WorldRad);Within(Matrix(part.GetProperty("actualWorld")),after,ctx.WorldM,ctx.WorldRad);
     if(attach){var e=part.GetProperty(p.Chain==0?"c1Error":"c2Error");double m=e.GetProperty("PositionMetres").GetDouble(),rad=e.GetProperty("AngleRadians").GetDouble();if(!double.IsFinite(m)||!double.IsFinite(rad)||m<0||rad<0||m>ctx.ContactM||rad>ctx.ContactRad)throw new InvalidDataException("Actual contact error exceeds original policy");}
     if(p.Chain==1){Within(after,ctx.Fixture,ctx.PlaceM,ctx.PlaceRad);var e=Error(after,ctx.Fixture);if(Math.Sqrt(Math.Pow(after[12]-ctx.Fixture[12],2)+Math.Pow(after[13]-ctx.Fixture[13],2))>ctx.FixtureXYM||Math.Abs(after[14]-ctx.Fixture[14])>ctx.FixtureZM||e.Rad>ctx.FixtureRad)throw new InvalidDataException("Actual fixture locator tolerance exceeded");}
     if(p.Chain==3){int slot=p.Sequence-1;if(detail.GetProperty("slot").GetInt32()!=slot||part.GetProperty("Slot").GetInt32()!=slot||r.GetProperty("cumulativeGeometryPackedCount").GetInt32()!=acceptedCount+1)throw new InvalidDataException("Six unique slot acceptance counts mismatch");Within(after,ctx.Slots[slot],ctx.PlaceM,ctx.PlaceRad);p.Accepted=after;acceptedCount++;}
     p.Chain++;totalEvents++;
    }
    foreach(var part in parts){var p=pieces[Id(part.GetProperty("id").GetString())];if(part.GetProperty("Sequence").GetInt32()!=p.Sequence)throw new InvalidDataException("Material identity changed sequence");if(p.Chain==4){if(part.GetProperty("Phase").GetString()!="Packed"||part.GetProperty("Slot").GetInt32()!=p.Sequence-1||part.GetProperty("attachedToTool").GetBoolean())throw new InvalidDataException("Accepted material left its actual packed slot");Within(Matrix(part.GetProperty("actualWorld")),p.Accepted,ctx.WorldM,ctx.WorldRad);}}
   }
   if(starts!=1||pieces.Count!=ctx.ExpectedPieces||acceptedCount!=ctx.ExpectedPieces||totalEvents!=4*ctx.ExpectedPieces||pieces.Values.Any(p=>p.Chain!=4||p.Accepted==null))throw new InvalidDataException("Six complete actual material chains required");
   // Compare original camera JSON to RAPID CSV with .001mm/20ms transport readback budgets plus original CSV rounding (.0005mm/.05ms); physical tolerances are unchanged.
   var rows=csv.Replace("\r","").Split('\n',StringSplitOptions.RemoveEmptyEntries);if(rows.Length!=ctx.ExpectedPieces+1||rows[0]!="run,cycle,dx_mm,dy_mm,slot,cycle_s,state,stage,err,vision_part_id,vision_seq,vision_virtual_ms")throw new InvalidDataException("Exactly six measured controller CSV cycle rows required");double seconds=0;
   var ordered=pieces.Values.OrderBy(p=>p.Sequence).ToArray();for(int j=0;j<ordered.Length;j++){var p=ordered[j];var c=rows[j+1].Split(',');double Num(int k)=>double.Parse(c[k],CultureInfo.InvariantCulture);
    if(c.Length!=12||new[]{0,1,2,3,4,5,7,8,10,11}.Any(k=>!double.IsFinite(Num(k)))||Num(0)!=ctx.NumericRun||Num(1)!=p.Sequence||Num(4)!=j||c[6]!="MOTION_SEQUENCE_RETURNED"||Num(7)!=50||Num(8)!=0||Id(c[9])!=p.Id||Num(10)!=p.Sequence||Math.Abs(Num(2)-p.Camera.Dx)>.00150001||Math.Abs(Num(3)-p.Camera.Dy)>.00150001||Math.Abs(Num(11)-p.Camera.Time)>20.050001||!double.IsFinite(Num(5))||Num(5)<=0)throw new InvalidDataException("Actual CSV does not match the six measured material packets");seconds+=Num(5);
   }
   return new OnePieceLedgerResult014(ordered.Select(p=>p.Id.ToString()).ToArray(),totalEvents,lines.Length,capture.Length-end,ordered.Select(p=>p.Accepted).ToArray(),seconds);
  }
 }
}
