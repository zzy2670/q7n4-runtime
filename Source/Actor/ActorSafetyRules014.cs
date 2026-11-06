// Pure pre-mutation guards. No SDK, IO, attachment changes or success feedback.
using System;
using System.IO;
namespace MA6212.Model014.Actor {
 public static class ActorSafetyRules014 {
  static void Positive(string name,double value,double maximum){if(!double.IsFinite(value)||value<=0||value>maximum)throw new InvalidDataException("Invalid or loosened Actor014 threshold: "+name);}
  public static void RequireThresholds(double contactM,double contactRad,double placementM,double placementRad,double worldM,double worldRad,double readyM,double readyRad,int stableMs,int gapMs,double fixtureXYM,double fixtureZM,double fixtureRad){
   Positive("contactM",contactM,.001);Positive("contactRad",contactRad,2*Math.PI/180);
   Positive("placementM",placementM,.001);Positive("placementRad",placementRad,2*Math.PI/180);
   Positive("worldM",worldM,1e-6);Positive("worldRad",worldRad,1e-5);
   Positive("readyM",readyM,.005);Positive("readyRad",readyRad,3*Math.PI/180);
   Positive("fixtureXYM",fixtureXYM,.0006);Positive("fixtureZM",fixtureZM,.0001);Positive("fixtureRad",fixtureRad,1e-5);
   if(stableMs<100||gapMs<=0||gapMs>60)throw new InvalidDataException("Original contact dwell/sample-gap caps must not be weakened.");
  }
  public static int RequireC2Release(string actualId,bool uniqueHeldRecord,bool actualToolAttachment,bool registeredMaterial,bool alreadyAccepted,int previousSlot,int[] actualUnoccupiedSlotMatches){
   if(!Guid.TryParse(actualId,out var id)||id==Guid.Empty||!uniqueHeldRecord||!actualToolAttachment||!registeredMaterial||alreadyAccepted||previousSlot!=-1)throw new InvalidOperationException("C2 release identity/held/attachment state is not an unaccepted actual registered workpiece; retain attachment.");
   if(actualUnoccupiedSlotMatches==null||actualUnoccupiedSlotMatches.Length!=1||actualUnoccupiedSlotMatches[0]<0||actualUnoccupiedSlotMatches[0]>=6)throw new InvalidOperationException("C2 release requires exactly one actual geometrically matched unoccupied box slot before detach; retain attachment.");
   return actualUnoccupiedSlotMatches[0];
  }
 }
}
