using System;
using System.Diagnostics;

namespace check_win_perfmon
{
    /// <summary>
    /// Load xml file with performance counters and their threshholds.
    /// Check performance counters aganist tresholds.
    /// Print performance info in Icinga/Nagios format
    /// Exit with ok, warning, critical, unknown code in Icinga/Nagios format
    /// Using nuget: (Install-Package) Costura.Fody, CommandLineParser
    /// </summary>

    internal static class Program
    {
        private static void Main(string[] args)
        {
            //Set lower priority for the process
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
            //Use only first procesor
            Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)1;
            //Change culture to en-US in order to use . as decimal separator
            System.Threading.Thread.CurrentThread.CurrentCulture =
            System.Globalization.CultureInfo.CreateSpecificCulture("en-US");
            //Initializing arguments
            var options = new Options();
            //Parsing arguments
            CommandLine.Parser.Default.ParseArgumentsStrict(args, options);
            //List of performance monitors class
            PerfCounterList perfCounterList = null;
            //Load XML with performance counters and calculate result
            try
            {
                //Load XML file
                perfCounterList = new PerfCounterList(options.XmlFile, options.Verbose);
                //Taking samples and calculate result of performance counters
                perfCounterList.Calculate(options.MaxSamples, options.TimeSamples);
                //Dispose performance counters
                perfCounterList.Dispose();
            }
            catch (Exception e)
            {
                //Return status and code unknown for monitoring software
                Console.WriteLine("Unknown: " + e.Message);
                Environment.Exit(3);
            }
            
            //No errors in PerfCounter, all counters are between ranges
            if (perfCounterList.GetGlobalExitCode() == 0)
            {
                Console.WriteLine($"{perfCounterList.GetGlobalStatus()} - All performance counters between range {perfCounterList.GlobalPerfOutput}");
            }
            //Some counters has values out of tresholds
            else
            {
                //Generate output with errors and performance data
                Console.WriteLine($"{perfCounterList.GetGlobalStatus()} - {perfCounterList.GlobalOutput} {perfCounterList.GlobalPerfOutput}");   
            }
            //Exit code for monitoring software
            Environment.Exit(perfCounterList.GetGlobalExitCode());
        }
    }
}