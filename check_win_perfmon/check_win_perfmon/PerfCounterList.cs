using System;
using System.Collections.Generic;
using System.Xml;

namespace check_win_perfmon
{
    /// <summary>
    /// Class to manage performance counter list, generate and calculate output in Icinga/Nagios format.
    /// </summary>
    // Default values of xml based on http://mpwiki.viacode.com/default.aspx?g=posts&t=219816
    public class PerfCounterList
    {
        public string GlobalPerfOutput { get; private set; } = "| ";
        public string GlobalOutput { get; private set; }
        private List<PerfCounter> _perfCounters;
        private NagiosStatus NagiosState { get; } = new NagiosStatus();

        public PerfCounterList(string xmlFilePath, bool verbose = false)
        {
            LoadXml(xmlFilePath, verbose);
        }
        public PerfCounterList(List<PerfCounter> perfCounters)
        {
            if (perfCounters.Count == 0)
            {
                throw new ArgumentException("Performance counters list cannot be empty", nameof(perfCounters));
            }

            if (_perfCounters != null)
            {
                Dispose();
            }
            _perfCounters = perfCounters;
        }

        public PerfCounterList()
        {
        }

        public void AddPerformanceCounter(PerfCounter perfCounter)
        {
            if (_perfCounters == null)
            {
                _perfCounters = new List<PerfCounter>();
            }
            _perfCounters.Add(perfCounter);
        }

        /// <summary>
        /// Generate List of PerfCounter based on XML file.
        /// </summary>
        /// <param name="xmlFilePath">XML file path</param>
        /// <param name="verbose">PerfCounter write debuggin messages on console</param>
        private void LoadXml(string xmlFilePath, bool verbose = false)
        {
            //Dispose PerformanceCounter List if not empty
            if (_perfCounters != null)
            {
                Dispose();
            }

            //Create new PerformanceCounter List
            _perfCounters = new List<PerfCounter>();
            //Load XML file
            var doc = new XmlDocument();
            doc.Load(xmlFilePath);
            var root = doc.DocumentElement;
            if (root != null)
            {
                var nodes = root.SelectNodes("perfcounter");
                if (nodes != null)
                    foreach (XmlNode node in nodes)
                    {
                        //Generate PerfCounter list with all perfcounters in XML file

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
        /// <summary>
        /// Dispose al PerformanceCounter from List
        /// </summary>
        public void Dispose()
        {
            foreach (var perfCounter in _perfCounters)
            {
                perfCounter.Dispose();
            }
        }
        /// <summary>
        /// Calculate all PerformanceCounter status and values based on tresholds given in XML file
        /// </summary>
        /// <param name="samples">Samples to take</param>
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
        /// <param name="timeBetweenSamples"></param>
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