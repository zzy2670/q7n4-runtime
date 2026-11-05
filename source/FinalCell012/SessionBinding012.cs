using System;using System.IO;using System.Threading;
namespace MA6212.Commissioning {
 // Native restoration can assign a new controller GUID. Bind once from the validated current station.
 // Keep that identity immutable for the remainder of the run.
 public static class SessionBinding012 {
  public const string Mode="SOLE_RESTORED_STARTED_MAPPED_VIRTUAL_WITH_EXACT_RELATIVE_PATHS";
  sealed class Value {public readonly Guid Id,ConfigurationId;public readonly string StationPath,SystemPath;public Value(Guid id,string station,string system,Guid config){Id=id;StationPath=station;SystemPath=system;ConfigurationId=config;}}
  static Value value;
  static Value Bound=>Volatile.Read(ref value)??throw new InvalidOperationException("Restored virtual session has not been explicitly verified/bound");
  public static Guid SystemId=>Bound.Id;
  public static string SystemIdText=>Bound.Id.ToString();
  public static string StationPath=>Bound.StationPath;
  public static string SystemPath=>Bound.SystemPath;
  public static Guid ConfigurationId=>Bound.ConfigurationId;
  internal static void BindVerifiedOnce(Guid id,string station,string system,Guid configuration){
   if(id==Guid.Empty||configuration==Guid.Empty||!Path.IsPathRooted(station)||!Path.IsPathRooted(system))throw new InvalidDataException("Actual verified virtual identity/paths/configuration required");
   if(Interlocked.CompareExchange(ref value,new Value(id,Path.GetFullPath(station),Path.GetFullPath(system),configuration),null)!=null)throw new InvalidOperationException("One restored session binding only; no implicit reset/rebind");
  }
 }
}
