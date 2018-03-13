using System;
using System.Collections.Generic;
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
        /// <summary>
        /// Initialize PerfCounter list from a XML file
        /// </summary>
        /// <param name="xmlFilePath">XML file containing performance counters definitions</param>
        /// <param name="verbose">PerfCounter print verbose output to console</param>
        public PerfCounterList(string xmlFilePath, bool verbose = false)
        {
            LoadXml(xmlFilePath, verbose);
        }
        /// <summary>
        /// Initialize PerfCounter list from a performance counter list
        /// </summary>
        /// <param name="perfCounters">PerfCounter list</param>
        public PerfCounterList(List<PerfCounter> perfCounters)
        {
            // Dont allow empty PerfCounter list
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
        private void LoadXml(string xmlFilePath, bool verbose = false)
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
                    //Generate PerfCounter list with all perfcounters in XML file
                    foreach (XmlNode node in nodes)
                    {
                        _perfCounters.Add(
                            new PerfCounter(
                                node.SelectSingleNode("category")?.InnerText,
                                node.SelectSingleNode("name")?.InnerText,
                                node.SelectSingleNode("instance")?.InnerText,
                                node.SelectSingleNode("friendlyname")?.InnerText,
                                node.SelectSingleNode("units")?.InnerText,
                                node.SelectSingleNode("warning")?.InnerText,
                                node.SelectSingleNode("critical")?.InnerText,
                                node.SelectSingleNode("min")?.InnerText,
                                node.SelectSingleNode("max")?.InnerText,
                                verbose
                            )
                        );
                    }
                else
                {
                    throw new NullReferenceException("Error loading xml file. Error selecting perfcounter nodes.");
                }
            }
            else
            {
                throw new NullReferenceException("Error loading xml file.");
            }
        }
        /// <inheritdoc />
        /// <summary>
        /// Dispose al Performance Counters from List
        /// </summary>
        public void Dispose()
        {
            foreach (var perfCounter in _perfCounters)
            {
                perfCounter.Dispose();
            }
        }
        /// <summary>
        /// Calculate status and values of PerfCounter list based on each PerfCounter tresholds 
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
                        //Check if PerfCounter is out of tresholds
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
    }
}