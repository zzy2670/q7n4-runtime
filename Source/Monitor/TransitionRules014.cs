using System;using System.IO;using System.Collections.Generic;using System.Linq;
namespace MA6212.Model014.Monitor {
 public enum AttachmentOperation014 {Attach,Detach}
 public sealed record CapturedTransition014(long Sequence,Guid MaterialId,object ActualInstance,string Contact,AttachmentOperation014 Operation,double TimeMs);
 // A native event can be deferred only across an already authorized, observed real attachment gap.
 public sealed class AttachmentTransitionGate014 {
  sealed record Authorization(Guid Id,object Instance,string Contact,AttachmentOperation014 Operation,double TimeMs);
  readonly Dictionary<Guid,Authorization> authorization=new();readonly List<CapturedTransition014> pending=new();long sequence;
  public int PendingCount=>pending.Count;
  public void ForgetAuthorization(Guid id)=>authorization.Remove(id);
  public void Authorize(Guid id,object instance,string contact,AttachmentOperation014 operation,double timeMs){
   if(id==Guid.Empty||instance==null||(contact!="C1"&&contact!="C2")||!double.IsFinite(timeMs)||timeMs<0||pending.Any(p=>p.MaterialId==id))throw new InvalidDataException("Fresh exact material/contact authorization required");
   authorization[id]=new Authorization(id,instance,contact,operation,timeMs);
  }
  public CapturedTransition014 TryCapture(Guid id,object actualInstance,string contact,AttachmentOperation014 operation,double nowMs,bool actualAttachmentGapAndGeometryVerified){
   if(!actualAttachmentGapAndGeometryVerified||!authorization.TryGetValue(id,out var a)||!Object.ReferenceEquals(a.Instance,actualInstance)||a.Contact!=contact||a.Operation!=operation||!double.IsFinite(nowMs)||Math.Abs(nowMs-a.TimeMs)>1e-6)return null;
   var item=new CapturedTransition014(++sequence,id,actualInstance,contact,operation,nowMs);pending.Add(item);return item;
  }
  public CapturedTransition014[] Resolve(Guid id,object actualInstance,string contact,AttachmentOperation014 operation,double nowMs,bool committedActualTransitionVerified){
   var items=pending.Where(p=>p.MaterialId==id).ToArray();
   if(items.Length==0){authorization.Remove(id);return items;}
   if(!committedActualTransitionVerified||items.Any(p=>!Object.ReferenceEquals(p.ActualInstance,actualInstance)||p.Contact!=contact||p.Operation!=operation||!double.IsFinite(nowMs)||Math.Abs(p.TimeMs-nowMs)>1e-6))throw new InvalidDataException("Captured collision was not resolved by its exact committed native transition");
   foreach(var p in items)pending.Remove(p);authorization.Remove(id);return items;
  }
  public void RequireNoPending(){if(pending.Count!=0)throw new InvalidDataException("Captured unexpected tool/material event remains unresolved; disappearance at next sample is not acceptance");}
 }
 public static class MountRules014 {
  public static bool Valid(int toolAttachmentCount,object actualParent,object ownedLink6)=>toolAttachmentCount==1&&ownedLink6!=null&&Object.ReferenceEquals(actualParent,ownedLink6);
 }
 public static class BindingRules014 {
  public static bool MayBindActor(bool serializedArmed,bool liveArmed,bool liveFaulted)=>!serializedArmed&&!liveArmed&&!liveFaulted;
 }
 public static class GeometryBindingRules014 {
  public static bool LocalBoundsWithin(IReadOnlyList<double> actualMinM,IReadOnlyList<double> actualMaxM,IReadOnlyList<double> expectedMinMm,IReadOnlyList<double> expectedMaxMm){
   if(actualMinM==null||actualMaxM==null||expectedMinMm==null||expectedMaxMm==null||actualMinM.Count!=3||actualMaxM.Count!=3||expectedMinMm.Count!=3||expectedMaxMm.Count!=3)return false;
   for(int i=0;i<3;i++)if(!double.IsFinite(actualMinM[i])||!double.IsFinite(actualMaxM[i])||!double.IsFinite(expectedMinMm[i])||!double.IsFinite(expectedMaxMm[i])||actualMaxM[i]<=actualMinM[i]||expectedMaxMm[i]<=expectedMinMm[i]||Math.Abs(actualMinM[i]*1000-expectedMinMm[i])>.02||Math.Abs(actualMaxM[i]*1000-expectedMaxMm[i])>.02)return false;
   return true;
  }
 }
 public static class SupportReportRules014 {
  public static bool Accept(string supportKind,bool actualGeometryWithin,double actualReportPositionM,double actualReportAngleRad,double canonicalReportPositionM,double canonicalReportAngleRad){
   if(!actualGeometryWithin)return false;
   double p,a;if(supportKind=="Source"){p=actualReportPositionM;a=actualReportAngleRad;}else if(supportKind=="Fixture"||supportKind=="Box"){p=canonicalReportPositionM;a=canonicalReportAngleRad;}else return false;
   return double.IsFinite(p)&&double.IsFinite(a)&&p>=0&&a>=0&&p<=1e-6&&a<=1e-5;
  }
 }
}
