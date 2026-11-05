// Six pure production-contract tests. Synthetic in-memory IDs are never accepted runtime evidence.
using System;using System.Linq;using System.Text.Json;using System.Text.Json.Nodes;using MA6212.Commissioning;
public static class PortableEvidence012Tests {
 public static void Main(){int passed=0;
  var ids=Enumerable.Range(1,45).Select(i=>new Guid(i,0,0,new byte[8]).ToString()).ToArray();
  var fixture=new{schema="MA6212_PORTABLE_ACCEPTED_IDS_V1",status="EXPORT_VERIFIED_FROM_SEALED_ACTUAL_C2_LOGS",targetActorRunId="demo10005_v001",rawEvidenceVerifiedAtExport=true,allRawEvidenceRevalidatedAtRestore=false,exportVerificationReceiptSHA256=new string('A',64),acceptedPartIds=ids,sourceEvidence=new[]{1,4,20,20}.Select((n,i)=>new{sha256=new string((char)('A'+i),64),acceptedCount=n}).ToArray()};
  byte[] good=JsonSerializer.SerializeToUtf8Bytes(fixture);void Test(string name,Action f,bool reject){bool ok;try{f();ok=!reject;}catch{ok=reject;}if(!ok)throw new Exception("Failed: "+name);passed++;Console.WriteLine(name+": PASS");}
  Test("exact45_export_verified_compact_contract",()=>{if(PortableEvidence012.ReadCompactPrior(good,PortableEvidence012.Hash(good),"demo10005_v001",45).Count!=45)throw new Exception();},false);
  var dup=JsonNode.Parse(good);dup["acceptedPartIds"][44]=ids[0];byte[] dupBytes=JsonSerializer.SerializeToUtf8Bytes(dup);Test("duplicate45_rejected",()=>PortableEvidence012.ReadCompactPrior(dupBytes,PortableEvidence012.Hash(dupBytes),"demo10005_v001",45),true);
  var raw=JsonNode.Parse(good);raw["allRawEvidenceRevalidatedAtRestore"]=true;byte[] rawBytes=JsonSerializer.SerializeToUtf8Bytes(raw);Test("false_restore_raw_revalidation_claim_rejected",()=>PortableEvidence012.ReadCompactPrior(rawBytes,PortableEvidence012.Hash(rawBytes),"demo10005_v001",45),true);
  Test("early_reject_cannot_cleanup_existing_actor011",()=>{int writes=0;bool cleaned=PortableEvidence012.RunOwnedCleanup(null,"existing_other_owner",true,true,"MA6212.VirtualCell011.CellRuntime","MA6212_VirtualCellActor_011","batch10004_fast_v001",true,true,()=>writes++);if(cleaned||writes!=0)throw new Exception("Early rejection modified another actor");},false);
  byte[] notApproved=JsonSerializer.SerializeToUtf8Bytes(new{schema="MA6212_FINAL_DEMO_RELEASE_012",executionApproved=false});Test("unapproved_release_rejected_even_with_matching_hash",()=>PortableEvidence012.ValidateRelease(notApproved,PortableEvidence012.Hash(notApproved)),true);
  Test("devBridge_rejected_native_names_filtered",()=>{bool rejected=false;try{PortableEvidence012.RequireNoDevelopmentBridge(new[]{"System.Private.CoreLib","MA6212_Bridge.net10_002","MA6212_FinalCell012"});}catch(System.IO.InvalidDataException){rejected=true;}if(!rejected)throw new Exception("Development Bridge accepted");var names=PortableEvidence012.RequireNoDevelopmentBridge(new[]{"System.Private.CoreLib","MA6212_FinalCell012","MA6212_ActorDemo012"});if(names.Length!=2||names.Contains("System.Private.CoreLib"))throw new Exception("Unexpected assembly metadata retained");},false);
  if(passed!=6)throw new Exception();Console.WriteLine("6 PURE PRODUCTION CONTRACT TESTS PASS; NO ABB REFERENCE/HTTP/DISCOVERY/RUNTIME");
 }
}
