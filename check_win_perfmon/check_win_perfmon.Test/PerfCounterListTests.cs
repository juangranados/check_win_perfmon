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
            var perfCounter1 = new PerfCounter("Memory", "Available MBytes", "none", "AvailableMBytes", "MB", "15%",
                "10%", "0", "auto");
            var perfCounter2 = new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "85",
                "95", "0", "100");
            var perfCounter3 = new PerfCounter("Network Adapter", "Bytes Total/Sec", "auto", "BytesTotalSec", "B",
                "80%", "90%", "0", "auto");
            var perfCounterList = new PerfCounterList();
            perfCounterList.AddPerformanceCounter(perfCounter1);
            perfCounterList.AddPerformanceCounter(perfCounter2);
            perfCounterList.AddPerformanceCounter(perfCounter3);
            perfCounterList.Calculate(3, 1000);
            Assert.AreEqual(perfCounterList.GetGlobalExitCode(), 0);
            perfCounterList.Dispose();
        }

        [TestMethod]
        public void PerfCounterList_SimulateCheck_StatusWarning()
        {
            var perfCounter1 = new PerfCounter("Memory", "Available MBytes", "none", "AvailableMBytes", "MB", "15%",
                "10%", "0", "auto"); //Ok
            var perfCounter2 = new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "1",
                "95", "0", "100"); //Warning
            var perfCounter3 = new PerfCounter("Network Adapter", "Bytes Total/Sec", "auto", "BytesTotalSec", "B",
                "80%", "90%", "0", "auto"); //Ok
            var perfCounterList = new PerfCounterList();
            perfCounterList.AddPerformanceCounter(perfCounter1);
            perfCounterList.AddPerformanceCounter(perfCounter2);
            perfCounterList.AddPerformanceCounter(perfCounter3);
            perfCounterList.Calculate(3, 1000);
            Assert.AreEqual(perfCounterList.GetGlobalExitCode(), 1);
            perfCounterList.Dispose();
        }

        [TestMethod]
        public void PerfCounterList_SimulateCheck_StatusCritical()
        {
            var perfCounter1 = new PerfCounter("Memory", "Available MBytes", "none", "AvailableMBytes", "MB", "99%",
                "98%", "0", "auto"); //Critical
            var perfCounter2 = new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "1",
                "90", "0", "100"); //Warning
            var perfCounter3 = new PerfCounter("Network Adapter", "Bytes Total/Sec", "auto", "BytesTotalSec", "B",
                "80%", "90%", "0", "auto"); //Ok
            var perfCounterList = new PerfCounterList();
            perfCounterList.AddPerformanceCounter(perfCounter1);
            perfCounterList.AddPerformanceCounter(perfCounter2);
            perfCounterList.AddPerformanceCounter(perfCounter3);
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