﻿using System;
using System.Diagnostics;

namespace check_win_perfmon
{
    /// <summary>
    /// Load XML file with performance counters and their thresholds.
    /// Check performance counters against thresholds.
    /// Print performance info in Icinga/Nagios format
    /// Exit with OK, warning, critical, unknown code in Icinga/Nagios format
    /// Using NuGet: (Install-Package) Costura.Fody, CommandLineParser
    /// </summary>

    internal static class Program
    {
        private static void Main(string[] args)
        {   
            try
            {
                //Set lower priority for the process
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
                //Use only first processor
                Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)1;
                //Change culture to en-US in order to use . as decimal separator
                System.Threading.Thread.CurrentThread.CurrentCulture =
                    System.Globalization.CultureInfo.CreateSpecificCulture("en-US");
                //Initializing arguments
                var options = new Options();
                //Parsing arguments
                CommandLine.Parser.Default.ParseArgumentsStrict(args, options);
                //Load XML with performance counters and calculate result
                //Load XML file
                using (var perfCounterList = new PerfCounterList(options.XmlFile, options.Verbose))
                {
                    //Taking samples and calculate result of performance counters
                    perfCounterList.Calculate(options.MaxSamples, options.TimeSamples);
                    //No errors in PerfCounter, all counters are between ranges
                    if (perfCounterList.GetGlobalExitCode() == 0)
                    {
                        Console.WriteLine(
                            $"{perfCounterList.GetGlobalStatus()} - All performance counters between range {perfCounterList.GlobalPerfOutput}");
                    }
                    //Some counters has values out of thresholds
                    else
                    {
                        //Generate output with errors and performance data
                        Console.WriteLine(
                            $"{perfCounterList.GetGlobalStatus()} - {perfCounterList.GlobalOutput} {perfCounterList.GlobalPerfOutput}");
                    }

                    //Exit code for monitoring software
                    Environment.Exit(perfCounterList.GetGlobalExitCode());
                }
            }
            catch (Exception e)
            {
                //Return status and code unknown for monitoring software
                Console.WriteLine("Unknown: " + e.Message);
                Environment.Exit(3);
            }
            
        }
    }
}