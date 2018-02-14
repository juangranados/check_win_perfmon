// Using nuget: (Install-Package) Costura.Fody, CommandLineParser
// Default values of xml based on http://mpwiki.viacode.com/default.aspx?g=posts&t=219816
using System;
using System.Diagnostics;
namespace check_win_perfmon
{
    /// <summary>
    /// Load xml file with performance counters and their threshholds.
    /// Check performance counters aganist tresholds.
    /// Print performance info in Icinga/Nagios format
    /// Exit with ok, warning, critical, unknown code in Icinga/Nagios format
    /// </summary>
    internal class Program
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
            CommandLine.Parser.Default.ParseArgumentsStrict(args, options);
            //Output message
            string outputMessage;
            //List of performance monitors class
            PerfCounterList perfCounterList = null;
            try
            {
                perfCounterList = new PerfCounterList(options.XmlFile, options.Verbose, options.MaxSamples, options.TimeSamples);
                perfCounterList.Calculate();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Environment.Exit(3);
            }
            
            //No errors in PerfCounter, all counters are between ranges
            if (perfCounterList.GetStatus() == "Ok")
            {
                outputMessage = $"Ok - All performance counters between range{perfCounterList.PerfOutput}";
            }
            //Some counters has values out of tresholds
            else
            {
                //Generate output with errors and performance data
                outputMessage = $"{perfCounterList.GetStatus()} - {perfCounterList.Output + perfCounterList.PerfOutput}";   
            }
            //Print result
            Console.WriteLine(outputMessage);
            //Exit code for monitoring software
            Environment.Exit(perfCounterList.GetExitCode());
        }
    }
}