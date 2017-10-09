// Using nuget: (Install-Package) Costura.Fody, CommandLineParser
// Default values of xml based on http://mpwiki.viacode.com/default.aspx?g=posts&t=219816
using System;
using CommandLine;
using System.Xml;
using System.Collections.Generic;
namespace check_win_perfmon
{
    /// <summary>
    /// Program arguments
    /// </summary>
    class Options
    {
        [Option('f', "xmlFile", DefaultValue = "PerfMonCPU.xml", HelpText = "XML file with performance counters to check.")]
        public string XmlFile { get; set; }

        [Option('s', "maxSamples", DefaultValue = 3, HelpText = "Amount of samples to take from perfmon.")]
        public int MaxSamples { get; set; }

        [Option('t', "timeSamples", DefaultValue = 1000, HelpText = "Time between samples in ms")]
        public int TimeSamples { get; set; }

        [Option('v', "verbose", HelpText = "Verbose output to debug.")]
        public bool Verbose { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            var help = new CommandLine.Text.HelpText
            {
                Heading = new CommandLine.Text.HeadingInfo("Check Win Perfmon", "1.0\n"),
                Copyright = new CommandLine.Text.CopyrightInfo("Juan Granados\n", 2017),
                AdditionalNewLineAfterOption = true,
                AddDashesToOption = true
            };
            help.AddPreOptionsLine("GNU General Public License 3.0\n");
            help.AddPreOptionsLine("Usage: check_win_perfmon.exe params:\n");
            help.AddOptions(this);
            return help;
        }
    }
    /// <summary>
    /// Load xml file with performance counters and their threshholds.
    /// Check performance counters aganist tresholds.
    /// Print performance info in Icinga/Nagios format
    /// Exit with ok, warning, critical, unknown code in Icinga/Nagios format
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            //Change culture to en-US in order to use . as decimal separator
            System.Threading.Thread.CurrentThread.CurrentCulture =
            System.Globalization.CultureInfo.CreateSpecificCulture("en-US");
            //Initializing arguments
            var options = new Options();
            CommandLine.Parser.Default.ParseArgumentsStrict(args, options);
            //Exit code
            int exitCode = 0;
            //Performance output of all counters
            string perfOutput = " | ";
            //Program output
            string output = null;

            //List of PerfCounter
            List<PerfCounter> perfCounters = new List<PerfCounter>();
            //Status class to store final status
            Status status = new Status();
            //Read XML
            try
            {
                //Load XML document
                XmlDocument doc = new XmlDocument();
                doc.Load(options.XmlFile);
                XmlElement root = doc.DocumentElement;
                XmlNodeList nodes = root.SelectNodes("perfcounter");
                foreach (XmlNode node in nodes)
                {
                    //Generate PerfCounter list with all perfcounters in XML file

                    perfCounters.Add(
                        new PerfCounter(
                            node.SelectSingleNode("category").InnerText,
                            node.SelectSingleNode("name").InnerText,
                            node.SelectSingleNode("instance").InnerText,
                            node.SelectSingleNode("friendlyname").InnerText,
                            node.SelectSingleNode("units").InnerText,
                            node.SelectSingleNode("warning").InnerText,
                            node.SelectSingleNode("critical").InnerText,
                            node.SelectSingleNode("min").InnerText,
                            node.SelectSingleNode("max").InnerText,
                            status, options.MaxSamples, options.Verbose
                        )
                    );
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Environment.Exit(3);
            }
            //Generate options.maxSamples +1 (for initializing counter) values for each PerfCounter
            for (int i = 0; i <= options.MaxSamples; i++)
            {
                foreach (PerfCounter perfCounter in perfCounters)
                {
                    try
                    {
                        perfCounter.NextValue();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        Environment.Exit(3);
                    }
                }
                //Sleep options.TimeSamples
                System.Threading.Thread.Sleep(options.TimeSamples);
            }
            //Get output of PerfCounters
            foreach (PerfCounter perfCounter in perfCounters)
            {
                //Get performance output
                perfOutput = perfOutput + perfCounter.PerfString;
                //Check if PerfCounter is out of range
                if (perfCounter.ResultString != null)
                {
                    //Get the error
                    output = output + perfCounter.ResultString + " ";
                    //Dispose object   
                    perfCounter.Dispose();
                }
            }
            //No errors in PerfCounter, all counters are between ranges
            if (output == null)
            {
                output = $"OK - All performance counters between range{perfOutput.TrimEnd()}";
            }
            //Some counters has values out of tresholds
            else
            {
                //Generate output with errors and performance data
                output = output.TrimEnd() + perfOutput.TrimEnd();
                //Status critical
                if (status.Critical)
                {
                    output = $"Critical - {output}";
                    exitCode = 2;
                }
                //Status warning
                else if (status.Warning)
                {
                    output = $"Warning - {output}";
                    exitCode = 1;
                }
            }
            //Print result
            Console.WriteLine(output);
            //Exit code for monitoring software
            Environment.Exit(exitCode);
        }
    }
}