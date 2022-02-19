using CommandLine;
using System.Collections.Generic;

namespace check_win_perfmon
{
    /// <summary>
    /// Program arguments
    /// </summary>
    internal class Options
    {
        [Option('f', "xmlFile", Default = "PerfMonCPU.xml", HelpText = "XML file with performance counters to check.")]
        public string XmlFile { get; set; }

        [Option('s', "maxSamples", Default = 3, HelpText = "Amount of samples to take from performance counter.")]
        public int MaxSamples { get; set; }

        [Option('t', "timeSamples", Default = 1000, HelpText = "Time between samples in ms.")]
        public int TimeSamples { get; set; }

        [Option('p', "xmlParameters", HelpText = "List of xml space-separated parameters.")]
        public IEnumerable<string> XmlParameters { get; set; }

        [Option('n', "noAlerts", HelpText = "Allways return 0 -> Ok status. Useful for getting only performance data.")]
        public bool NoAlerts { get; set; }

        [Option('v', "verbose", HelpText = "Verbose output to debug.")]
        public bool Verbose { get; set; }
    }
}