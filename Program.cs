/*
 * This is a simple driver to spawn multiple instances of GCPerfSim tool to determine the optimal values for certain
 * default parameters like min Gen0Size. 
 * It uses complus_* variables to initialie the ProcessInfo before starting GCPerfSim. 
 * Note that it currently depends on GCPerfSim to return the time taken as the exit code. Going forward it can parse the 
 * tool output to collect various paramaters.
 * The results are sent to Application Insights as custom Events for analysis. 

*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace FindGen0Size
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = TelemetryConfiguration.CreateDefault();
            var client = new TelemetryClient(config);
            client.InstrumentationKey = "yourIKey";
            // Initialize ProcessStartInfo -- Parameters are hardcoded for now
            // TODO: These could be taken as parameters going forward
            ProcessStartInfo gcPerf =
                new ProcessStartInfo("GcPerfSim.exe", "-tc 4 -tagb 30 -tlgb 1 -lohar 0 -sohsi 0 -lohsi 0 -pohsi 0 -sohpi 0 -lohpi 0 -pohpi 0 -sohfi 0 -lohfi 0 -pohfi 0 -allocType simple -testKind time")
                {
                    RedirectStandardOutput = true
                };


            for (int i = 0; i < 64; i++)
            {
                string gen0Size = (i * 0x100000).ToString("X");
                // Set the gen0Size before starting the process
                gcPerf.Environment.Add("COMPLUS_gcGen0Size", gen0Size);
                for (int j = 0; j < 2; j++)
                {
                    // toggle gcServer setting for current iteration
                    gcPerf.Environment.Add("complus_gcServer", j.ToString());
                    Process p = Process.Start(gcPerf);
                    p.WaitForExit();
                    Console.WriteLine("With {0}, gcServer={2}: {1} ms", gen0Size, p.ExitCode, j);
                    // Send results to AppInsights. 
                    client.TrackEvent("GenSizeThroughput", new Dictionary<string, string>() {
                                                         {"machineName", Environment.MachineName },
                                                         {"gen0size", gen0Size },
                                                         {"timeTaken", p.ExitCode.ToString()},
                                                         {"gcServer", j.ToString()}});
                }
            }
            // Flush the events before exiting. 
            client.Flush();
        }
    }
}
