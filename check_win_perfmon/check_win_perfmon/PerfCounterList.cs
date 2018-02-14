using System;
using System.Collections.Generic;
using System.Xml;

namespace check_win_perfmon
{
    /// <summary>
    /// Class to manage performance counter list, generate and calculate output in Icinga/Nagios format.
    /// </summary>
    internal class PerfCounterList
    {
        public string PerfOutput { get; private set; } = " | ";
        public string Output { get; private set; }
        private readonly List<PerfCounter> _perfCounters = new List<PerfCounter>();
        public Status State { get; } = new Status();
        private readonly int _samples;
        private readonly int _timeBetweenSamples;

        public PerfCounterList(string xmlFilePath,bool verbose,int samples,int timeBetweenSamples)
        {
            _samples = samples;
            _timeBetweenSamples = timeBetweenSamples;
            try
            { 
                var doc = new XmlDocument();
                doc.Load(xmlFilePath);
                var root = doc.DocumentElement;
                if (root != null)
                {
                    var nodes = root.SelectNodes("perfcounter");
                    if (nodes != null)
                    {
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
                                    State, verbose
                                )
                            );
                        }
                    }
                    else
                    {
                        Console.WriteLine("Error loading xml file.");
                    }
                }
                else
                {
                    Console.WriteLine("Error loading xml file.");
                }
            }   
            catch (Exception)
            {
                Console.WriteLine("Error loading xml file.");
                throw;
            }
        }

        public void Calculate()
        {
            //Generate options.maxSamples +1 (for initializing counters) values for each PerfCounter
            for (var i = 0; i <= _samples; i++)
            {
                foreach (var perfCounter in _perfCounters)
                {
                    perfCounter.NextValue();
                    if (i == _samples)
                    {
                        //Calculate performance counter status and values
                        perfCounter.Calculate();
                        //Get performance output
                        PerfOutput = PerfOutput + perfCounter.PerfString;
                        //Check if PerfCounter is out of range
                        if (perfCounter.ResultString != null)
                        {
                            //Get the error
                            Output = Output + perfCounter.ResultString + " ";
                        }
                        //Dispose object   
                        perfCounter.Dispose();
                    }
                }
                //Only sleep before calculate
                if (i < _samples)
                {
                    System.Threading.Thread.Sleep(_timeBetweenSamples);
                }
            }

            Output = Output?.TrimEnd();
            PerfOutput = PerfOutput.TrimEnd();
        }

        public string GetStatus()
        {
            return State.GetStatus();
        }

        public int GetExitCode()
        {
            return State.GetExitCode();
        }
    }
}