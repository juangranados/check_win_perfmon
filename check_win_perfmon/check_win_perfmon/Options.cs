using CommandLine;

namespace check_win_perfmon
{
    /// <summary>
    /// Program arguments
    /// </summary>
    internal class Options
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
                Heading = new CommandLine.Text.HeadingInfo(programName: "Check Win Perfmon", version: "1.0\n"),
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
}