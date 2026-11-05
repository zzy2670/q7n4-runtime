using System;using MA6212.FinalRecording012;
public static class Tests {public static void Main(){
 if(!RecorderDesktopDecisions012.Allowed(false,"Default","Default",null))throw new Exception("Ordinary manual desktop rejected");Console.WriteLine("manual_Default_bound_owned_desktop_allowed: PASS");
 if(RecorderDesktopDecisions012.Allowed(false,"Default","Other",null)||RecorderDesktopDecisions012.Allowed(true,"Default","Default","Default"))throw new Exception("Host change/isolation bypass allowed");Console.WriteLine("host_changed_or_isolated_mode_on_Default_rejected: PASS");
 if(!RecorderDesktopDecisions012.Allowed(true,"MA6212_Isolated","MA6212_Isolated","Default")||RecorderDesktopDecisions012.Allowed(true,"MA6212_Isolated","MA6212_Isolated","Winlogon"))throw new Exception("Probe isolated/input constraint changed");Console.WriteLine("isolated_probe_requires_bound_host_and_Default_input: PASS");
 Console.WriteLine("3 PURE PRODUCTION DESKTOP DECISION TESTS PASS; NO WINDOWS/ABB CALLS");}}
