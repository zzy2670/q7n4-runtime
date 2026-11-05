using System;using System.IO;using System.Linq;using System.Text.Json;using System.Security.Cryptography;using System.Collections.Generic;using System.Text.RegularExpressions;
namespace MA6212.Commissioning {
 public static class PortableEvidence012 {
  public static string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes));
  public static bool IsSha(string value)=>Regex.IsMatch(value??"",@"^[A-F0-9]{64}$");
  public static void Require(bool value,string message){if(!value)throw new InvalidDataException(message);}
  public static string[] RequireNoDevelopmentBridge(string[] names){
   var filtered=names.Where(n=>!String.IsNullOrEmpty(n)&&(n.StartsWith("MA6212_Bridge",StringComparison.OrdinalIgnoreCase)||n.StartsWith("MA6212_FinalCell",StringComparison.OrdinalIgnoreCase)||n.StartsWith("MA6212_Actor",StringComparison.OrdinalIgnoreCase))).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n=>n,StringComparer.Ordinal).ToArray();
   Require(!filtered.Any(n=>n.StartsWith("MA6212_Bridge",StringComparison.OrdinalIgnoreCase)),"Final demonstration refuses a loaded development Bridge; use the native installed add-in host");return filtered;
  }
  public static bool RunOwnedCleanup(string token,string observedToken,bool sameActor,bool sameRuntime,string runtimeType,string runtimeAssembly,string actorRun,bool installed,bool ready,Action cleanup){
   if(String.IsNullOrEmpty(token)||token!=observedToken||!sameActor||!sameRuntime||runtimeType!="MA6212.VirtualCellDemo012.CellRuntime"||runtimeAssembly!="MA6212_ActorDemo012"||actorRun!="demo10005_v001"||!installed||!ready)return false;
   cleanup();return true;
  }
  public static HashSet<string> ReadCompactPrior(byte[] bytes,string sha,string actor,int minimum){
   Require(IsSha(sha)&&Hash(bytes)==sha,"Exact approved compact prior bytes required");using var document=JsonDocument.Parse(bytes);var r=document.RootElement;
   Require(r.GetProperty("schema").GetString()=="MA6212_PORTABLE_ACCEPTED_IDS_V1"&&r.GetProperty("status").GetString()=="EXPORT_VERIFIED_FROM_SEALED_ACTUAL_C2_LOGS"&&r.GetProperty("targetActorRunId").GetString()==actor,"Explicit export-verified portable prior contract required");
   Require(r.GetProperty("rawEvidenceVerifiedAtExport").GetBoolean()&&!r.GetProperty("allRawEvidenceRevalidatedAtRestore").GetBoolean(),"Restore must not claim raw-history revalidation");
   Require(IsSha(r.GetProperty("exportVerificationReceiptSHA256").GetString()),"Actual export verification receipt hash required");
   var ids=r.GetProperty("acceptedPartIds").EnumerateArray().Select(e=>Demo012Ledger.Id(e.GetString())).ToArray();
   Require(minimum==45&&ids.Length==45&&ids.Distinct().Count()==45,"Exactly45 real prior accepted IDs required; duplicates forbidden");
   var sources=r.GetProperty("sourceEvidence").EnumerateArray().ToArray();Require(sources.Length==4&&sources.All(s=>IsSha(s.GetProperty("sha256").GetString()))&&sources.Select(s=>s.GetProperty("sha256").GetString()).Distinct().Count()==4,"Four distinct sealed raw sources/hashes retained for repository audit");
   Require(sources.Select(s=>s.GetProperty("acceptedCount").GetInt32()).SequenceEqual(new[]{1,4,20,20}),"Single1 + interrupted4 + Base20 + Fast20 required; partial is not20");return ids.ToHashSet();
  }
  public static void ValidateRelease(byte[] bytes,string expectedHash){
   Require(IsSha(expectedHash)&&Hash(bytes)==expectedHash,"Final release is not frozen; Fast comparison/prior45/restore binding still required");using var d=JsonDocument.Parse(bytes);var r=d.RootElement;
   Require(r.GetProperty("schema").GetString()=="MA6212_FINAL_DEMO_RELEASE_012"&&r.GetProperty("executionApproved").GetBoolean()&&r.GetProperty("selectedVariant").GetString()=="FAST_EMPTY_150_AFTER_TRUE20_AND_FASTER_COMPARISON","Approved measured selection required; provisional Fast is blocked");
   foreach(string key in new[]{"baseline20ResultSHA256","fast20ResultSHA256","comparisonReceiptSHA256","compactPriorSHA256","actorConfigSHA256","moduleSHA256","casePlanSHA256"})Require(IsSha(r.GetProperty(key).GetString()),"Actual final proof hash missing: "+key);
   Require(r.GetProperty("rapidRunId").GetInt32()==10005&&r.GetProperty("actorRunId").GetString()=="demo10005_v001"&&r.GetProperty("csvBasename").GetString()=="MA6212_demo10005_v001.csv","Fresh one-shot Demo10005 identity required");
  }
  public static string Inside(string root,string relative){
   Require(!String.IsNullOrWhiteSpace(relative)&&!Path.IsPathRooted(relative),"Explicit portable relative path required");string prefix=Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar,path=Path.GetFullPath(Path.Combine(root,relative));Require(path.StartsWith(prefix,StringComparison.OrdinalIgnoreCase),"Path leaves the approved restored root");return path;
  }
 }
}
