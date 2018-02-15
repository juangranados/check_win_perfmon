﻿using System;
using System.Collections.Generic;
using System.Xml;

namespace check_win_perfmon
{
    /// <summary>
    /// Class to manage performance counter list, generate and calculate output in Icinga/Nagios format.
    /// </summary>
    // Default values of xml based on http://mpwiki.viacode.com/default.aspx?g=posts&t=219816
    internal class PerfCounterList
    {
        public string GlobalPerfOutput { get; private set; } = "| ";
        public string GlobalOutput { get; private set; }
        private List<PerfCounter> _perfCounters;
        private NagiosStatus NagiosState { get; } = new NagiosStatus();

        public PerfCounterList(string xmlFilePath, bool verbose = false)
        {
            LoadXml(xmlFilePath, verbose);
        }

        private void LoadXml(string xmlFilePath, bool verbose = false)
        {
            if (_perfCounters != null)
            {
                Dispose();
            }

            _perfCounters = new List<PerfCounter>();
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
        public void Dispose()
        {
            foreach (var perfCounter in _perfCounters)
            {
                perfCounter.Dispose();
            }
        }

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
                        //Get performance output
                        GlobalPerfOutput = GlobalPerfOutput + perfCounter.PerfString + " ";
                        //Check if PerfCounter is out of tresholds
                        if (perfCounter.ResultString != null)
                        {
                            //Get the error
                            GlobalOutput = GlobalOutput + perfCounter.ResultString + " ";
                        }
                        //Set global status of counters
                        SetGlobalStatus(perfCounter.CounterStatus);
                    }
                }
                //Only sleep before samples not after calculate
                if (i < samples -1)
                {
                    System.Threading.Thread.Sleep(timeBetweenSamples);
                }
            }
            //Trim spaces at the end
            GlobalOutput = GlobalOutput?.TrimEnd();
            GlobalPerfOutput = GlobalPerfOutput.TrimEnd();
        }

        private void Initialize(int timeBetweenSamples)
        {
            foreach (var perfCounter in _perfCounters)
            {
                perfCounter.Initialize();
            }
            System.Threading.Thread.Sleep(timeBetweenSamples);
        }

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

        public string GetGlobalStatus()
        {
            return NagiosState.GetNagiosStatus();
        }

        public int GetGlobalExitCode()
        {
            return NagiosState.GetNagiosExitCode();
        }
    }
}