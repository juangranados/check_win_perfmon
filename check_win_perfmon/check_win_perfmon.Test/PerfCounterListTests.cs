using System;
using System.Collections.Generic;
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
            perfCounterList.AddPerformanceCounter(new PerfCounter("Memory", "Available MBytes", "none", "AvailableMBytes", "MB", "15%","10%", "0", "auto"));
            perfCounterList.AddPerformanceCounter(new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "85", "95", "0", "100"));
            perfCounterList.AddPerformanceCounter(new PerfCounter("Network Adapter", "Bytes Total/Sec", "auto", "BytesTotalSec", "B", "80%", "90%", "0", "auto"));
            perfCounterList.AddPerformanceCounter(new PerfCounter("PhysicalDisk", "Current Disk Queue Length", "auto", "CurrentDiskQueueLength", "", ">=32", "none", "none", "none"));
            perfCounterList.Calculate(3, 1000);
            Assert.AreEqual(perfCounterList.GetGlobalExitCode(), 0);
            perfCounterList.Dispose();
        }

        [TestMethod]
        public void PerfCounterList_SimulateCheck_StatusWarning()
        {
            var perfCounterList = new PerfCounterList();
            perfCounterList.AddPerformanceCounter(new PerfCounter("Memory", "Available MBytes", "none", "AvailableMBytes", "MB", "15%", "10%", "0", "auto")); //Ok
            perfCounterList.AddPerformanceCounter(new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "1", "95", "0", "100")); //Warning
            perfCounterList.AddPerformanceCounter(new PerfCounter("Network Adapter", "Bytes Total/Sec", "auto", "BytesTotalSec", "B", "80%", "90%", "0", "auto")); //Ok
            perfCounterList.AddPerformanceCounter(new PerfCounter("PhysicalDisk", "Current Disk Queue Length", "auto", "CurrentDiskQueueLength", "", "none", ">=40", "none", "none")); //Ok
            perfCounterList.Calculate(3, 1000);
            Assert.AreEqual(perfCounterList.GetGlobalExitCode(), 1);
            perfCounterList.Dispose();
        }

        [TestMethod]
        public void PerfCounterList_SimulateCheck_StatusCritical()
        {
            var perfCounterList = new PerfCounterList();
            perfCounterList.AddPerformanceCounter(new PerfCounter("Memory", "Available MBytes", "none", "AvailableMBytes", "MB", "99%", "98%", "0", "auto")); //Critical
            perfCounterList.AddPerformanceCounter(new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "1", "90", "0", "100")); //Warning
            perfCounterList.AddPerformanceCounter(new PerfCounter("Network Adapter", "Bytes Total/Sec", "auto", "BytesTotalSec", "B", "80%", "90%", "0", "auto")); //Ok
            perfCounterList.AddPerformanceCounter(new PerfCounter("PhysicalDisk", "% Idle Time", "auto", "IdleTime", "%", "<=100", "none", "0", "100")); // Warning
            perfCounterList.Calculate(3, 1000);
            Assert.AreEqual(perfCounterList.GetGlobalExitCode(), 2);
            perfCounterList.Dispose();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void PerfCounterList_PerfCounterListIsEmpty_ThrowsAnException()
        {
            var perfCounterList = new PerfCounterList(new List<PerfCounter>());
        }
        [TestMethod]
        [ExpectedException(typeof(System.IO.FileNotFoundException))]
        public void PerfCounterList_PerfCounterXMLPathIsInvalid_ThrowsAnException()
        {
            var perfCounterList = new PerfCounterList("C:\\temp\\fake.xml");
        }
    }
}