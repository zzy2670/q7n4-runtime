// Reload only the packaged unarmed program on the verified restored virtual controller.
using System;using System.IO;using System.Linq;using System.Security.Cryptography;using System.Threading.Tasks;
using ABB.Robotics.Controllers;using ABB.Robotics.Controllers.Discovery;using ABB.Robotics.Controllers.RapidDomain;
using Task=System.Threading.Tasks.Task;using String=System.String;
namespace Q7n4.Portable014m {
 public static class PortableProgramPrepare014 {
  const string Module="MA6212_OnePiece014",Sha="7F192EF7D2DC666E5B1DDEDC32C503E621DD5B04666E52384C44DB384A45EEA4";
  static string H(string p)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p)));
  static string Text(string s)=>s.TrimStart('\uFEFF').Replace("\r\n","\n").Replace("\r","\n");
  public static async Task Reload(PortableSession014 session,string source) {
   if(!session.IsCurrent||H(source)!=Sha)throw new InvalidDataException("Exact packaged six-piece source and current restored session required.");
   string output=Path.Combine(session.EvidenceDirectory,"program_prepare");if(Directory.Exists(output))throw new IOException("One preparation attempt only.");Directory.CreateDirectory(output);
   await Task.Run(()=>{
    var info=new NetworkScanner().GetControllers(NetworkScannerSearchCriterias.Virtual).Single(i=>i.IsVirtual&&i.SystemId==session.SystemId);
    using var c=Controller.Connect(info,ConnectionType.Standalone);c.Logon(UserInfo.DefaultUser);var t=c.Rapid.GetTask("T_ROB1");
    void Guard(){if(!c.IsVirtual||c.SystemId!=session.SystemId||!String.Equals(Path.GetFullPath(c.GetEnvironmentVariable("HOME")),session.HomePath,StringComparison.OrdinalIgnoreCase)||c.OperatingMode!=ControllerOperatingMode.Auto||c.Rapid.ExecutionStatus!=ExecutionStatus.Stopped||!t.Enabled||c.Rapid.GetTasks().Any(a=>a.Enabled&&a.Name!="T_ROB1"))throw new InvalidOperationException("Exact restored Auto/Stopped controller required.");foreach(string n in new[]{"doVacuum","diVacuumOK","doFeedRequest","diFeedReady","doBoxReset","diBoxReady"})if(c.IOSystem.GetSignal(n).Value!=0||c.IOSystem.GetSignal(n).InputAsPhysical)throw new InvalidOperationException("Six zero virtual IO required.");}
    Guard();var q=t.GetJointTarget().ToString();var names=t.GetModules().Select(m=>m.Name).OrderBy(n=>n).ToArray();if(!names.SequenceEqual(new[]{"BASE","MA6212_Tooling"})&&!names.SequenceEqual(new[]{"BASE",Module,"MA6212_Tooling"}))throw new InvalidOperationException("Only baseline, tooling and the owned production module may be present.");
    string before=Path.Combine(output,"before");Directory.CreateDirectory(before);foreach(var m in t.GetModules())m.SaveToFile(before);
    var retained=new[]{"BASE","MA6212_Tooling"}.ToDictionary(n=>n,n=>H(Directory.GetFiles(before).Single(p=>Path.GetFileNameWithoutExtension(p)==n)));
    if(retained["BASE"]!="95314A5128DB6B5CE43DC246BD5EC0EBF9D504E9FCCB83AB3E319D1A064A8B79"||retained["MA6212_Tooling"]!="AA199E02953D0992D85F1BB95B55D2E7E9074F4207CCB7E66F434E06DCB5B764")throw new InvalidDataException("Canonical baseline/tooling bytes required.");
    string archivedOwnedHash=null;if(names.Contains(Module)){
     string actual=Directory.GetFiles(before).Single(p=>Path.GetFileNameWithoutExtension(p)==Module);archivedOwnedHash=H(actual);if((archivedOwnedHash!="1AE9E539E2F1DE27F1540F09D9ACD7F063426CC039E442EF46AC17260BE2653E"&&Text(File.ReadAllText(actual))!=Text(File.ReadAllText(source)))||t.GetRapidData(new[]{Module,"runAttempt"}).StringValue!="0"||t.GetRapidData(new[]{Module,"completedCycles"}).StringValue!="0"||t.GetRapidData(new[]{Module,"single014ExecutionArmed"}).Value is not Bool b||b.Value)throw new InvalidOperationException("Restore an initial unarmed program; completed sessions are not reset in place.");
    }
    string bare="basic6_"+Guid.NewGuid().ToString("N")+".mod";if(H(source)!=Sha)throw new InvalidDataException("Source changed before upload.");c.FileSystem.PutFile(source,bare,false);string remote=c.GetEnvironmentVariable("HOME").Replace('\\','/').TrimEnd('/')+"/"+bare;
    using(var master=Mastership.Request(c)){Guard();if(names.Contains(Module)){string locked=Path.Combine(output,"mastership_owned_before_delete");Directory.CreateDirectory(locked);t.GetModule(Module).SaveToFile(locked);if(H(Directory.GetFiles(locked).Single(p=>Path.GetFileNameWithoutExtension(p)==Module))!=archivedOwnedHash||t.GetRapidData(new[]{Module,"runAttempt"}).StringValue!="0"||t.GetRapidData(new[]{Module,"completedCycles"}).StringValue!="0"||t.GetRapidData(new[]{Module,"single014ExecutionArmed"}).Value is not Bool currentArmed||currentArmed.Value)throw new InvalidDataException("Archived owned module or unarmed zero counters changed before deletion.");t.GetModule(Module).Delete();}if(H(source)!=Sha||!t.LoadModuleFromFile(remote,RapidLoadMode.Add))throw new InvalidOperationException("One normal Add failed; preserve actual partial state.");var check=t.CheckProgram();File.WriteAllText(Path.Combine(output,"program_check.txt"),"errors="+check.Errors.Count+"\n"+String.Join("\n",check.Errors.Cast<object>().Select(e=>e.ToString())));if(check.Errors.Count!=0)throw new InvalidOperationException("Program check failed; no arm or start.");}
    string after=Path.Combine(output,"after");Directory.CreateDirectory(after);foreach(var m in t.GetModules())m.SaveToFile(after);
    if(retained.Any(e=>H(Directory.GetFiles(after).Single(p=>Path.GetFileNameWithoutExtension(p)==e.Key))!=e.Value)||Text(File.ReadAllText(Directory.GetFiles(after).Single(p=>Path.GetFileNameWithoutExtension(p)==Module)))!=Text(File.ReadAllText(source))||t.GetRapidData(new[]{Module,"runAttempt"}).StringValue!="0"||t.GetRapidData(new[]{Module,"completedCycles"}).StringValue!="0"||t.GetRapidData(new[]{Module,"single014ExecutionArmed"}).Value is not Bool armed||armed.Value||t.GetJointTarget().ToString()!=q)throw new InvalidOperationException("Loaded source, unarmed flags, retained modules or physical joints changed unexpectedly.");Guard();
   });if(!session.IsCurrent)throw new InvalidOperationException("Restored session changed during program preparation.");
  }
 }
}
