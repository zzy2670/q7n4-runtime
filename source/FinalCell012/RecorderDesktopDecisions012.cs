using System;
namespace MA6212.FinalRecording012 {
 public static class RecorderDesktopDecisions012 {
  public static bool Allowed(bool requireIsolatedDesktop,string boundHost,string actualHost,string actualInput)=>!String.IsNullOrEmpty(boundHost)&&String.Equals(boundHost,actualHost,StringComparison.Ordinal)&&(!requireIsolatedDesktop||(actualHost=="MA6212_Isolated"&&actualInput=="Default"));
 }
}
