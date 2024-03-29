﻿using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace check_win_perfmon.Test
{
    [TestClass]
    public class PerfCounterListTests
    {
        [TestMethod]
        public void PerfCounterList_SimulateCheck_StatusOK()
        {
            var perfCounterList = new PerfCounterList();
            perfCounterList.AddPerformanceCounter(new PerfCounter("Memory", "Available MBytes", "none", "AvailableMBytes", "MB", "15%","10%", "0", "automemory"));
            perfCounterList.AddPerformanceCounter(new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "85", "95", "0", "100"));
            perfCounterList.AddPerformanceCounter(new PerfCounter("Network Adapter", "Bytes Total/Sec", "autonetwork", "BytesTotalSec", "B", "80%", "90%", "0", "autonetwork"));
            perfCounterList.AddPerformanceCounter(new PerfCounter("PhysicalDisk", "Current Disk Queue Length", "autodisk", "CurrentDiskQueueLength", "", "gt32", "none", "none", "none"));
            perfCounterList.Calculate(3, 1000);
            Assert.AreEqual(perfCounterList.GetGlobalExitCode(), 0);
            perfCounterList.Dispose();
        }

        [TestMethod]
        public void PerfCounterList_SimulateCheck_StatusWarning()
        {
            var perfCounterList = new PerfCounterList();
            perfCounterList.AddPerformanceCounter(new PerfCounter("Memory", "Available MBytes", "none", "AvailableMBytes", "MB", "15%", "10%", "0", "automemory")); //OK
            perfCounterList.AddPerformanceCounter(new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "1", "95", "0", "100")); //Warning
            perfCounterList.AddPerformanceCounter(new PerfCounter("Network Adapter", "Bytes Total/Sec", "autonetwork", "BytesTotalSec", "B", "80%", "90%", "0", "autonetwork")); //OK
            perfCounterList.AddPerformanceCounter(new PerfCounter("PhysicalDisk", "Current Disk Queue Length", "autodisk", "CurrentDiskQueueLength", "", "none", "gt40", "none", "none")); //OK
            perfCounterList.Calculate(3, 1000);
            Assert.AreEqual(perfCounterList.GetGlobalExitCode(), 1);
            perfCounterList.Dispose();
        }

        [TestMethod]
        public void PerfCounterList_SimulateCheck_StatusCritical()
        {
            var perfCounterList = new PerfCounterList();
            perfCounterList.AddPerformanceCounter(new PerfCounter("Memory", "Available MBytes", "none", "AvailableMBytes", "MB", "99%", "98%", "0", "automemory")); //Critical
            perfCounterList.AddPerformanceCounter(new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "1", "90", "0", "100")); //Warning
            perfCounterList.AddPerformanceCounter(new PerfCounter("Network Adapter", "Bytes Total/Sec", "autonetwork", "BytesTotalSec", "B", "80%", "90%", "0", "autonetwork")); //OK
            perfCounterList.AddPerformanceCounter(new PerfCounter("PhysicalDisk", "% Idle Time", "autodisk", "IdleTime", "%", "lt100", "none", "0", "100")); // Warning
            perfCounterList.Calculate(3, 1000);
            Assert.AreEqual(perfCounterList.GetGlobalExitCode(), 2);
            perfCounterList.Dispose();
        }

        [TestMethod]
        public void PerfCounterList_SimulateXMLLoad_StatusOK()
        {
            var path = System.IO.Path.GetTempPath() + "\\foo.xml";
            new XDocument(
                    new XElement("perfcounters",
                        new XElement("perfcounter",
                            new XElement("category", "Processor"),
                            new XElement("name", "% Processor Time"),
                            new XElement("instance", "_Total"),
                            new XElement("friendlyname", "ProcessorTime"),
                            new XElement("units", "%"),
                            new XElement("warning", "gt90"),
                            new XElement("critical", "none"), 
                            new XElement("min", "0"),
                            new XElement("max", "100")
                        ),
                        new XElement("perfcounter",
                            new XElement("category", "PhysicalDisk"),
                            new XElement("name", "% Idle Time"),
                            new XElement("instance", "autodisk"),
                            new XElement("friendlyname", "IdleTime"),
                            new XElement("units", "%"),
                            new XElement("warning", "20"),
                            new XElement("critical", "15"),
                            new XElement("min", "0"),
                            new XElement("max", "100")
                        ),
                        new XElement("perfcounter",
                            new XElement("category", "Memory"),
                            new XElement("name", "Available MBytes"),
                            new XElement("instance", "none"),
                            new XElement("friendlyname", "AvailableMBytes"),
                            new XElement("units", "MB"),
                            new XElement("warning", "10%"),
                            new XElement("critical", "5%"),
                            new XElement("min", "0"),
                            new XElement("max", "100")
                        )
                    )
                )
                .Save(path);
            var perfCounterList = new PerfCounterList(path);
            perfCounterList.Calculate(3, 1000);
            Assert.AreEqual(perfCounterList.GetGlobalExitCode(), 0);
            perfCounterList.Dispose();
        }

        [TestMethod]
        public void PerfCounterList_SimulateXMLLoad_StatusWarning()
        {
            var path = System.IO.Path.GetTempPath() + "\\foo.xml";
            new XDocument(
                    new XElement("perfcounters",
                        new XElement("perfcounter",
                            new XElement("category", "Processor"),
                            new XElement("name", "% Processor Time"),
                            new XElement("instance", "_Total"),
                            new XElement("friendlyname", "ProcessorTime"),
                            new XElement("units", "%"),
                            new XElement("warning", "gt1"),//Warning
                            new XElement("critical", "none"),
                            new XElement("min", "0"),
                            new XElement("max", "100")
                        ),
                        new XElement("perfcounter",
                            new XElement("category", "PhysicalDisk"),
                            new XElement("name", "% Idle Time"),
                            new XElement("instance", "autodisk"),
                            new XElement("friendlyname", "IdleTime"),
                            new XElement("units", "%"),
                            new XElement("warning", "20"),
                            new XElement("critical", "15"),
                            new XElement("min", "0"),
                            new XElement("max", "100")
                        ),
                        new XElement("perfcounter",
                            new XElement("category", "Memory"),
                            new XElement("name", "Available MBytes"),
                            new XElement("instance", "none"),
                            new XElement("friendlyname", "AvailableMBytes"),
                            new XElement("units", "MB"),
                            new XElement("warning", "10%"),
                            new XElement("critical", "5%"),
                            new XElement("min", "0"),
                            new XElement("max", "100")
                        )
                    )
                )
                .Save(path);
            var perfCounterList = new PerfCounterList(path);
            perfCounterList.Calculate(3, 1000);
            Assert.AreEqual(perfCounterList.GetGlobalExitCode(), 1);
            perfCounterList.Dispose();
        }

        [TestMethod]
        public void PerfCounterList_SimulateXMLLoad_StatusCritical()
        {
            var path = System.IO.Path.GetTempPath() + "\\foo.xml";
            new XDocument(
                    new XElement("perfcounters",
                        new XElement("perfcounter",
                            new XElement("category", "Processor"),
                            new XElement("name", "% Processor Time"),
                            new XElement("instance", "_Total"),
                            new XElement("friendlyname", "ProcessorTime"),
                            new XElement("units", "%"),
                            new XElement("warning", "gt1"),//Warning
                            new XElement("critical", "none"),
                            new XElement("min", "0"),
                            new XElement("max", "100")
                        ),
                        new XElement("perfcounter",
                            new XElement("category", "PhysicalDisk"),
                            new XElement("name", "% Idle Time"),
                            new XElement("instance", "autodisk"),
                            new XElement("friendlyname", "IdleTime"),
                            new XElement("units", "%"),
                            new XElement("warning", "101"),
                            new XElement("critical", "100"),//Critical
                            new XElement("min", "0"),
                            new XElement("max", "100")
                        ),
                        new XElement("perfcounter",
                            new XElement("category", "Memory"),
                            new XElement("name", "Available MBytes"),
                            new XElement("instance", "none"),
                            new XElement("friendlyname", "AvailableMBytes"),
                            new XElement("units", "MB"),
                            new XElement("warning", "10%"),
                            new XElement("critical", "5%"),
                            new XElement("min", "0"),
                            new XElement("max", "100")
                        )
                    )
                )
                .Save(path);
            var perfCounterList = new PerfCounterList(path);
            perfCounterList.Calculate(3, 1000);
            Assert.AreEqual(perfCounterList.GetGlobalExitCode(), 2);
            perfCounterList.Dispose();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void PerfCounterList_SimulateBadXMLLoad_Exception()
        {
            var path = System.IO.Path.GetTempPath() + "\\foo.xml";
            new XDocument(
                    new XElement("perfcounters",
                        new XElement("perfcounter",
                            new XElement("category", "Processor"),
                            new XElement("name", "% Processor Time"),
                            new XElement("instance", "_Total"),
                            new XElement("friendlyname", "ProcessorTime"),
                            new XElement("units", "%"),
                            //new XElement("warning", "gt1"),
                            new XElement("critical", "none"),
                            new XElement("min", "0"),
                            new XElement("max", "100")
                        ),
                        new XElement("perfcounter",
                            new XElement("category", "PhysicalDisk"),
                            new XElement("name", "% Idle Time"),
                            new XElement("instance", "autodisk"),
                            new XElement("friendlyname", "IdleTime"),
                            new XElement("units", "%"),
                            new XElement("warning", "101"),
                            new XElement("critical", "100"),
                            new XElement("min", "0"),
                            new XElement("max", "100")
                        ),
                        new XElement("perfcounter",
                            new XElement("category", "Memory"),
                            new XElement("name", "Available MBytes"),
                            new XElement("instance", "none"),
                            new XElement("friendlyname", "AvailableMBytes"),
                            new XElement("units", "MB"),
                            new XElement("warning", "10%"),
                            new XElement("critical", "5%"),
                            new XElement("min", "0"),
                            new XElement("max", "100")
                        )
                    )
                )
                .Save(path);
            using (var perfCounterList = new PerfCounterList(path))
            {
                perfCounterList.Calculate(3, 1000);
                perfCounterList.Dispose();
            }
        }
        [TestMethod]
        public void PerfCounterList_SimulateXMLLoadWithParams_StatusOK()
        {
            var path = System.IO.Path.GetTempPath() + "\\foo.xml";
            string[] arguments = { "_Total", "gt90", "IdleTime", "15", "Available", "MBytes", "Physical", "Disk", "e", "0" };
            new XDocument(
                    new XElement("perfcounters",
                        new XElement("perfcounter",
                            new XElement("category", "Processor"),
                            new XElement("name", "% Processor Time"),
                            new XElement("instance", "{0}"),
                            new XElement("friendlyname", "ProcessorTime"),
                            new XElement("units", "%"),
                            new XElement("warning", "{1}"),
                            new XElement("critical", "none"),
                            new XElement("min", "{9}"),
                            new XElement("max", "100")
                        ),
                        new XElement("perfcounter",
                            new XElement("category", "{6}{7}"),
                            new XElement("name", "% Idl{8} Tim{8}"),
                            new XElement("instance", "autodisk"),
                            new XElement("friendlyname", "{2}"),
                            new XElement("units", "%"),
                            new XElement("warning", "20"),
                            new XElement("critical", "{3}"),
                            new XElement("min", "0"),
                            new XElement("max", "1{9}0")
                        ),
                        new XElement("perfcounter",
                            new XElement("category", "Memory"),
                            new XElement("name", "{4} {5}"),
                            new XElement("instance", "none"),
                            new XElement("friendlyname", "{4}{5}"),
                            new XElement("units", "MB"),
                            new XElement("warning", "10%"),
                            new XElement("critical", "5%"),
                            new XElement("min", "0"),
                            new XElement("max", "1{9}{9}")
                        )
                    )
                )
                .Save(path);
            var perfCounterList = new PerfCounterList(path, arguments);
            perfCounterList.Calculate(3, 1000);
            Assert.AreEqual(perfCounterList.GetGlobalExitCode(), 0);
            perfCounterList.Dispose();
        }
        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void PerfCounterList_SimulateXMLLoadWithBadParams_Exception()
        {
            var path = System.IO.Path.GetTempPath() + "\\foo.xml";
            string[] arguments = { "_Total", "gt90", "IdleTime", "15", "Available", "MBytes", "Physical", "Disk", "e", "0" };
            new XDocument(
                    new XElement("perfcounters",
                        new XElement("perfcounter",
                            new XElement("category", "Processor"),
                            new XElement("name", "% Processor Time"),
                            new XElement("instance", "{0}"),
                            new XElement("friendlyname", "ProcessorTime"),
                            new XElement("units", "%"),
                            new XElement("warning", "{1}"),
                            new XElement("critical", "none"),
                            new XElement("min", "{9}"),
                            new XElement("max", "100")
                        ),
                        new XElement("perfcounter",
                            new XElement("category", "{6}{7}"),
                            new XElement("name", "% Idl{8} Tim{8}"),
                            new XElement("instance", "autodisk"),
                            new XElement("friendlyname", "{2}"),
                            new XElement("units", "%"),
                            new XElement("warning", "20"),
                            new XElement("critical", "{3}"),
                            new XElement("min", "0"),
                            new XElement("max", "1{9}0")
                        ),
                        new XElement("perfcounter",
                            new XElement("category", "Memory"),
                            new XElement("name", "{4} {5}"),
                            new XElement("instance", "none"),
                            new XElement("friendlyname", "{14}{5}"),
                            new XElement("units", "MB"),
                            new XElement("warning", "10%"),
                            new XElement("critical", "5%"),
                            new XElement("min", "0"),
                            new XElement("max", "1{9}{9}")
                        )
                    )
                )
                .Save(path);
            using (var perfCounterList = new PerfCounterList(path, arguments))
            {
                perfCounterList.Calculate(3, 1000);
                perfCounterList.Dispose();
            }
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void PerfCounterList_PerfCounterListIsEmpty_ThrowsAnException()
        {
            var unused = new PerfCounterList(new List<PerfCounter>());
        }
        [TestMethod]
        [ExpectedException(typeof(System.IO.FileNotFoundException))]
        public void PerfCounterList_PerfCounterXMLPathIsInvalid_ThrowsAnException()
        {
            var unused = new PerfCounterList("C:\\temp\\fake.xml");
        }
    }
}