using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;

namespace check_win_perfmon
{
    /// <inheritdoc />
    /// <summary>
    /// Class to manage a performance counter list, generate and calculate output in Icinga/Nagios format.
    /// </summary>
    public class PerfCounterList : IDisposable
    {
        // String to store performance output from all counters
        public string GlobalPerfOutput { get; private set; } = "| ";
        // String to store output from all counters
        public string GlobalOutput { get; private set; }
        // PerfCounter list
        private List<PerfCounter> _perfCounters;
        private NagiosStatus NagiosState { get; } = new NagiosStatus();
        // Set verbose output to console
        private readonly bool _verbose;
        // Set verbose output to console
        private readonly string[] _xmlParams;

        /// <summary>
        /// Initialize PerfCounter list from a XML file
        /// </summary>
        /// <param name="xmlFilePath">XML file containing performance counters definitions</param>
        /// <param name="verbose">PerfCounter print verbose output to console</param>
        public PerfCounterList(string xmlFilePath, string[] xmlParams = null, bool verbose = false)
        {
            _verbose = verbose;
            _xmlParams = xmlParams;
            LoadXml(xmlFilePath, xmlParams, verbose);
            WriteVerbose("XML loaded");
        }
        /// <summary>
        /// Initialize PerfCounter list from a performance counter list
        /// </summary>
        /// <param name="perfCounters">PerfCounter list</param>
        public PerfCounterList(List<PerfCounter> perfCounters)
        {
            // Don't allow empty PerfCounter list
            if (perfCounters.Count == 0)
            {
                throw new ArgumentException("Performance counters list cannot be empty", nameof(perfCounters));
            }
            // Dispose PerfCounter list if not empty
            if (_perfCounters != null)
            {
                Dispose();
            }
            // Initialize PerfCounter list
            _perfCounters = perfCounters;
        }
        /// <summary>
        /// Empty constructor
        /// </summary>
        public PerfCounterList()
        {
        }
        /// <summary>
        /// Add PerfCounter to PerfCounter list
        /// </summary>
        /// <param name="perfCounter">PerfCounter to add</param>
        public void AddPerformanceCounter(PerfCounter perfCounter)
        {
            // Initialize PerfCounter list if null
            if (_perfCounters == null)
            {
                _perfCounters = new List<PerfCounter>();
            }
            // Add PerfCounter to list
            _perfCounters.Add(perfCounter);
        }

        /// <summary>
        /// Initialize PerfCounter list from XML file.
        /// </summary>
        /// <param name="xmlFilePath">XML file path</param>
        /// <param name="verbose">PerfCounter write debugging messages on console</param>
        private void LoadXml(string xmlFilePath, string[] xmlParams, bool verbose = false)
        {
            //Dispose PerfCounter list if not empty
            if (_perfCounters != null)
            {
                Dispose();
            }

            //Create new PerfCounter List
            _perfCounters = new List<PerfCounter>();
            //Load XML file
            var doc = new XmlDocument();
            doc.Load(xmlFilePath);
            var root = doc.DocumentElement;
            if (root != null)
            {
                var nodes = root.SelectNodes("perfcounter");
                if (nodes != null)
                    //Generate PerfCounter list with all performance counters in XML file
                    foreach (XmlNode node in nodes)
                    {
                        _perfCounters.Add(
                            new PerfCounter(
                                CheckParams(node.SelectSingleNode("category")?.InnerText),
                                CheckParams(node.SelectSingleNode("name")?.InnerText),
                                CheckParams(node.SelectSingleNode("instance")?.InnerText),
                                CheckParams(node.SelectSingleNode("friendlyname")?.InnerText),
                                CheckParams(node.SelectSingleNode("units")?.InnerText),
                                CheckParams(node.SelectSingleNode("warning")?.InnerText),
                                CheckParams(node.SelectSingleNode("critical")?.InnerText),
                                CheckParams(node.SelectSingleNode("min")?.InnerText),
                                CheckParams(node.SelectSingleNode("max")?.InnerText),
                                verbose
                            )
                        );
                    }
                else
                {
                    throw new NullReferenceException("Error loading XML file. Error selecting perfcounter nodes.");
                }
            }
            else
            {
                throw new NullReferenceException("Error loading XML file.");
            }
        }
        /// <inheritdoc />
        /// <summary>
        /// Dispose all Performance Counters from List
        /// </summary>
        public void Dispose()
        {
            foreach (var perfCounter in _perfCounters)
            {
                perfCounter.Dispose();
            }
        }
        /// <summary>
        /// Calculate status and values of PerfCounter list based on each PerfCounter thresholds 
        /// </summary>
        /// <param name="samples">Samples to take from each PerfCounter</param>
        /// <param name="timeBetweenSamples">Pause between samples</param>
        public void Calculate(int samples,int timeBetweenSamples)
        {
            //Initialize counters
            Initialize(timeBetweenSamples);
            //Taking samples
            for (var i = 0; i < samples; i++)
            {
                foreach (var perfCounter in _perfCounters)
                {
                    //Take new sample
                    perfCounter.NextValue();

                    //Last sample taken
                    if (i == samples -1)
                    {
                        //Calculate performance counter status and values
                        perfCounter.Calculate();
                        //Add performance output to global performance.
                        GlobalPerfOutput = GlobalPerfOutput + perfCounter.PerfString + " ";
                        //Check if PerfCounter is out of thresholds
                        if (perfCounter.ResultString != null)
                        {
                            //Get the error to global errors.
                            GlobalOutput = GlobalOutput + perfCounter.ResultString + " ";
                        }
                        //Set global status of counter List.
                        SetGlobalStatus(perfCounter.CounterStatus);
                    }
                }
                //Only sleep before samples not after calculate
                if (i < samples -1)
                {
                    System.Threading.Thread.Sleep(timeBetweenSamples);
                }
            }
            //Trim spaces at the end of globals.
            GlobalOutput = GlobalOutput?.TrimEnd();
            GlobalPerfOutput = GlobalPerfOutput.TrimEnd();
        }
        /// <summary>
        /// Initialize all counters.
        /// </summary>
        /// <param name="timeBetweenSamples">Pause after initialization</param>
        private void Initialize(int timeBetweenSamples)
        {
            foreach (var perfCounter in _perfCounters)
            {
                perfCounter.Initialize();
            }
            System.Threading.Thread.Sleep(timeBetweenSamples);
        }
        /// <summary>
        /// Change global status of counter List
        /// </summary>
        /// <param name="nagiosStatus">Status to change</param>
        private void SetGlobalStatus(NagiosStatus nagiosStatus)
        {
            if (nagiosStatus.GetNagiosExitCode() == 0)
            {
                return;
            }

            if (nagiosStatus.GetNagiosExitCode() == 1)
            {
                NagiosState.SetWarning();
            }
            else if (nagiosStatus.GetNagiosExitCode() == 2)
            {
                NagiosState.SetCritical();
            }
        }
        /// <summary>
        /// Return global status
        /// </summary>
        /// <returns></returns>
        public string GetGlobalStatus()
        {
            return NagiosState.GetNagiosStatus();
        }
        /// <summary>
        /// Return global exit code
        /// </summary>
        /// <returns></returns>
        public int GetGlobalExitCode()
        {
            return NagiosState.GetNagiosExitCode();
        }
        /// <summary>
        /// Check for params keyword in xml field and change it for its real value.
        /// </summary>
        /// <param name="xmlField">Field to check</param>
        private string CheckParams(string xmlField)
        {
            if (xmlField == null) {
                throw new ArgumentException("XML field cannot be null.");
            }
            // Finds any parameter like {0} or {2} or {20}
            MatchCollection matches = Regex.Matches(xmlField, "{[0-9^}]+}");
            
            // Regex regex = new Regex("[ -~]+{([0-9]+)}[ -~]+");
            if (matches.Count > 0) 
            {
                foreach (Match match in matches)
                {
                    WriteVerbose($"Param {match.Value} found");
                    // Get param number inside {}
                    int paramNumber = Int32.Parse(match.ToString().Substring(1, match.ToString().Length - 2));
                    WriteVerbose($"Param number: {paramNumber}");
                    if (_xmlParams.Length > paramNumber && _xmlParams[paramNumber] != null)
                    {
                        string paramValue = _xmlParams[paramNumber];
                        WriteVerbose($"Param number {paramNumber} value is {paramValue}");
                        WriteVerbose($"Replacing {match.Value} by {paramValue}");
                        xmlField = xmlField.ReplaceFirst(match.Value, paramValue);
                        WriteVerbose($"XML field is now {xmlField}");
                    }
                    else 
                    {
                        throw new Exception($"Param number {paramNumber} does not exist in array of parameters received.");
                    }
                }
                return xmlField;
            }
            else 
            { 
                return xmlField;
            }
        }
        /// <summary>
        /// Write to console if _verbose is true
        /// </summary>
        /// <param name="output">Message to output</param>
        private void WriteVerbose(string output)
        {
            if (_verbose)
            {
                Console.WriteLine($"[{DateTime.Now.ToString("MM/dd/yyyy-HH:mm:ss.fff", DateTimeFormatInfo.InvariantInfo)}] {output}");
            }
        }
    }
}