using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static check_win_perfmon.Utils;

namespace check_win_perfmon.Test
{
    [TestClass]
    public class PerfCounterTests
    {
        //MethodName_Condition_Expectation
        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void PerfCounter_MaxIsNull_ThrowsAnException()
        {
            var unused = new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "5","10","0",null);
        }
        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void PerfCounter_MinIsNull_ThrowsAnException()
        {
            var unused = new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "5", "10", null, "100");
        }
        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void PerfCounter_CriticalIsNull_ThrowsAnException()
        {
            var unused = new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "5", null, "0", "100");
        }
        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void PerfCounter_WarningIsNull_ThrowsAnException()
        {
            var unused = new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", null, "10", "0", "100");
        }
        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void PerfCounter_UnitIsNull_ThrowsAnException()
        {
            var unused = new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", null, "5", "10", "0", "100");
        }
        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void PerfCounter_FriendlyNameIsNull_ThrowsAnException()
        {
            var unused = new PerfCounter("Processor", "% Processor Time", "_Total", null, "%", "5", "10", "0", "100");
        }
        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void PerfCounter_InstanceNameIsNull_ThrowsAnException()
        {
            var unused = new PerfCounter("Processor", "% Processor Time", null, "ProcessorTime", "%", "5", "10", "0", "100");
        }
        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void PerfCounter_CounterNameIsNull_ThrowsAnException()
        {
            var unused = new PerfCounter("Processor", null, "_Total", "ProcessorTime", "%", "5", "10", "0", "100");
        }
        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void PerfCounter_CategoryNameIsNull_ThrowsAnException()
        {
            var unused = new PerfCounter(null, "% Processor Time", "_Total", "ProcessorTime", "%", "5", "10", "0", "100");
        }
        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void PerfCounter_MixAndMaxAreEquals_ThrowsAnException()
        {
            var unused = new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "5", "10", "100", "100");
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void PerfCounter_MinIsNotANumber_ThrowsAnException()
        {
            var unused = new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "5", "10", "test", "100");
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void PerfCounter_MaxIsNotANumber_ThrowsAnException()
        {
            var unused = new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "5", "10", "0", "test");
        }
        public void PerfCounter_WarningIsNotANumber_ThrowsAnException()
        {
            var unused = new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "test", "10", "0", "100");
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void PerfCounter_CriticalIsNotANumber_ThrowsAnException()
        {
            var unused = new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "5", "test", "0", "100");
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void PerfCounter_CriticalIsAPercentAndMaxIsNone_ThrowsAnException()
        {
            var unused = new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "5", "10%", "0", "none");
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void PerfCounter_WarningIsAPercentAndMaxIsNone_ThrowsAnException()
        {
            var unused = new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "5%", "10", "0", "none");
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void PerfCounter_NextValueWithoutInitializingCounter_ThrowsAnException()
        {
            var perfCounter = new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "5", "10", "0", "100");
            perfCounter.NextValue();
        }
        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void PerfCounter_InvalidCounter_ThrowsAnException()
        {
            var unused = new PerfCounter("ProcessorTest", "% Processor Time", "_Total", "ProcessorTime", "%", "5", "10", "0", "100");
        }
        [TestMethod]
        public void PerfCounter_SimulateWarning_StatusWarning()
        {
            var perfCounter = new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "1", "99", "0", "100");
            CalculatePerfCounter(perfCounter);
            Assert.AreEqual(perfCounter.CounterStatus.GetNagiosExitCode(), 1);
        }
        [TestMethod]
        public void PerfCounter_SimulateCritical_StatusCritical()
        {
            var perfCounter = new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "1", "2", "0", "100");
            CalculatePerfCounter(perfCounter);
            Assert.AreEqual(perfCounter.CounterStatus.GetNagiosExitCode(), 2);
        }
        [TestMethod]
        public void PerfCounter_SimulateReverseWarning_StatusWarning()
        {
            var perfCounter = new PerfCounter("Memory", "Available MBytes", "none", "AvailableMBytes", "MB", "95%", "10%", "0", "auto");
            CalculatePerfCounter(perfCounter);
            Assert.AreEqual(perfCounter.CounterStatus.GetNagiosExitCode(), 1);
        }
        [TestMethod]
        public void PerfCounter_SimulateReverseCritical_StatusCritical()
        {
            var perfCounter = new PerfCounter("Memory", "Available MBytes", "none", "AvailableMBytes", "MB", "95%", "90%", "0", "auto");
            CalculatePerfCounter(perfCounter);
            Assert.AreEqual(perfCounter.CounterStatus.GetNagiosExitCode(), 2);
        }
        [TestMethod]
        public void PerfCounter_InterfaceSpeedAutodetection_InterfaceSpeedAsMax()
        {
            var perfCounter = new PerfCounter("Network Adapter", "Bytes Total/Sec", "auto", "BytesTotalSec", "B", "80%", "90%", "0", "auto");
            CalculatePerfCounter(perfCounter);
            Assert.AreEqual(perfCounter.GetMax(), GetNetworkInterfaceSpeed(GetNetworkInterface()));
        }
        [TestMethod]
        public void PerfCounter_SystemMemoryAutodetection_SystemMemoryAsMax()
        {
            var perfCounter = new PerfCounter("Memory", "Available MBytes", "none", "AvailableMBytes", "MB", "10%", "5%", "0", "auto");
            CalculatePerfCounter(perfCounter);
            Assert.AreEqual(perfCounter.GetMax(), GetTotalMemory("MB"));
        }
        [TestMethod]
        public void PerfCounter_WarningAndCriticalPercentCalculation_WarningAndCriticalPercent()
        {
            var perfCounter = new PerfCounter("Memory", "Available MBytes", "none", "AvailableMBytes", "MB", "10%", "5%", "0", "auto");
            CalculatePerfCounter(perfCounter);
            float systemMemory = GetTotalMemory("MB");
            Assert.AreEqual(perfCounter.GetWarning(), systemMemory * 10 / 100);
            Assert.AreEqual(perfCounter.GetCritical(), systemMemory * 5 / 100);

            perfCounter = new PerfCounter("Network Adapter", "Bytes Total/Sec", "auto", "BytesTotalSec", "B", "80%", "90%", "0", "auto");
            CalculatePerfCounter(perfCounter);
            var interfaceSpeed = GetNetworkInterfaceSpeed(GetNetworkInterface());
            Assert.AreEqual(perfCounter.GetWarning(), interfaceSpeed * 80 / 100);
            Assert.AreEqual(perfCounter.GetCritical(), interfaceSpeed * 90 / 100);
        }
        [TestMethod]
        public void PerfCounter_CalculateAverageResult_AverageResult()
        {
            var perfCounter = new PerfCounter("Memory", "Available MBytes", "none", "AvailableMBytes", "MB", "10%", "5%", "0", "auto");
            float result = 0;
            perfCounter.Initialize();
            System.Threading.Thread.Sleep(1000);
            result += perfCounter.NextValue();
            System.Threading.Thread.Sleep(1000);
            result += perfCounter.NextValue();
            System.Threading.Thread.Sleep(1000);
            result += perfCounter.NextValue();
            perfCounter.Calculate();
            Assert.AreEqual(perfCounter.GetResult(), result / 3);
        }
        private static void CalculatePerfCounter(PerfCounter perfCounter)
        {
            perfCounter.Initialize();
            System.Threading.Thread.Sleep(1000);
            perfCounter.NextValue();
            System.Threading.Thread.Sleep(1000);
            perfCounter.NextValue();
            System.Threading.Thread.Sleep(1000);
            perfCounter.NextValue();
            perfCounter.Calculate();
        }
    }
}