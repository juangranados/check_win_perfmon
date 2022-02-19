using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static check_win_perfmon.Utils;

namespace check_win_perfmon.Test
{
    [TestClass]
    public class PerfCounterTests
    {
        //MethodName_Condition_Expectation

        //Incorrect data input tests
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
        [ExpectedException(typeof(ArgumentException))]
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
        [ExpectedException(typeof(ArgumentException))]
        public void PerfCounter_IncorrectOnlyWarningFormat1_ThrowsAnException()
        {
            var unused = new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "g5", "none", "0", "100");
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void PerfCounter_IncorrectOnlyWarningFormat2_ThrowsAnException()
        {
            var unused = new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "5", "none", "0", "100");
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void PerfCounter_IncorrectOnlyWarningEmpty1_ThrowsAnException()
        {
            var unused = new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "g", "none", "0", "100");
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void PerfCounter_IncorrectOnlyWarningEmpty2_ThrowsAnException()
        {
            var unused = new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "gt", "none", "0", "100");
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void PerfCounter_IncorrectOnlyCriticalFormat1_ThrowsAnException()
        {
            var unused = new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "none", "g5", "0", "100");
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void PerfCounter_IncorrectOnlyCriticalFormat2_ThrowsAnException()
        {
            var unused = new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "none", "5", "0", "100");
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void PerfCounter_IncorrectOnlyCriticalEmpty1_ThrowsAnException()
        {
            var unused = new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "none", "l", "0", "100");
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void PerfCounter_IncorrectOnlyCriticalEmpty2_ThrowsAnException()
        {
            var unused = new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "none", "lt", "0", "100");
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
        [ExpectedException(typeof(Exception))]
        public void PerfCounter_InvalidCounter_ThrowsAnException()
        {
            var unused = new PerfCounter("ProcessorTest", "% Processor Time", "_Total", "ProcessorTime", "%", "5", "10", "0", "100");
        }
        /// <summary>
        /// Test if exception is thrown if performance counter is not initialized
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void PerfCounter_NextValueWithoutInitializingCounter_ThrowsAnException()
        {
            var perfCounter = new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "5", "10", "0", "100");
            perfCounter.NextValue();
        }
        /// <summary>
        /// Test if exception is thrown if instance is deprecated auto
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void PerfCounter_DeprecatedAutoInstance_ThrowsAnException()
        {
            var unused = new PerfCounter("Processor", "% Processor Time", "auto", "ProcessorTime", "%", "5", "10", "0", "100");
        }
        /// <summary>
        /// Test if exception is thrown if instance is deprecated auto
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void PerfCounter_DeprecatedAutoMax_ThrowsAnException()
        {
            var unused = new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "5", "10", "0", "auto");
        }
        /// <summary>
        /// Simulate OK status.
        /// Warning and critical are 99 an 99 for % Processor Time
        /// </summary>
        [TestMethod]
        public void PerfCounter_SimulateOk_StatusOk()
        {
            var perfCounter = new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "98", "99", "0", "100");
            CalculatePerfCounter(perfCounter);
            Assert.AreEqual(perfCounter.CounterStatus.GetNagiosExitCode(), 0);
        }
        /// <summary>
        /// Simulate OK status with only warning check
        /// </summary>
        [TestMethod]
        public void PerfCounter_SimulateOkWithOnlyWarningCheck_StatusOk()
        {
            var perfCounter = new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "gt90", "none", "0", "100");
            CalculatePerfCounter(perfCounter);
            Assert.AreEqual(perfCounter.CounterStatus.GetNagiosExitCode(), 0);
            var perfCounter2 = new PerfCounter("Memory", "Available MBytes", "none", "AvailableMBytes", "MB", "lt10%", "none", "0", "automemory");
            CalculatePerfCounter(perfCounter2);
            Assert.AreEqual(perfCounter.CounterStatus.GetNagiosExitCode(), 0);
        }
        /// <summary>
        /// Simulate OK status with only critical check
        /// </summary>
        [TestMethod]
        public void PerfCounter_SimulateOkWithOnlyCriticalCheck_StatusOk()
        {
            var perfCounter = new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "none", "gt90", "0", "100");
            CalculatePerfCounter(perfCounter);
            Assert.AreEqual(perfCounter.CounterStatus.GetNagiosExitCode(), 0);
            var perfCounter2 = new PerfCounter("Memory", "Available MBytes", "none", "AvailableMBytes", "MB", "none", "lt10%", "0", "automemory");
            CalculatePerfCounter(perfCounter2);
            Assert.AreEqual(perfCounter.CounterStatus.GetNagiosExitCode(), 0);
        }
        /// <summary>
        /// Simulate OK status if there aren't warning and critical
        /// </summary>
        [TestMethod]
        public void PerfCounter_NotCheckWarningAndCritical_StatusOk()
        {
            var perfCounter = new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "none", "none", "0", "100");
            CalculatePerfCounter(perfCounter);
            Assert.AreEqual(perfCounter.CounterStatus.GetNagiosExitCode(), 0);
        }
        /// <summary>
        /// Simulate warning status. performance counter value has to be lower than warning.
        /// Warning is 1 for % Processor Time
        /// </summary>
        [TestMethod]
        public void PerfCounter_SimulateWarning_StatusWarning()
        {
            var perfCounter = new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "1", "99", "0", "100");
            CalculatePerfCounter(perfCounter);
            Assert.AreEqual(perfCounter.CounterStatus.GetNagiosExitCode(), 1);
        }
        /// <summary>
        /// Simulate critical status.
        /// Warning is 2 for % Processor Time
        /// </summary>
        [TestMethod]
        public void PerfCounter_SimulateCritical_StatusCritical()
        {
            var perfCounter = new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "1", "2", "0", "100");
            CalculatePerfCounter(perfCounter);
            Assert.AreEqual(perfCounter.CounterStatus.GetNagiosExitCode(), 2);
        }
        /// <summary>
        /// Simulate reverse warning status, performance counter value has to be greater than warning.
        /// Warning is 95% for Available MBytes.
        /// Check also % interpretation is correct because max is specified and warning is a percent with MB value.
        /// Check also memory auto detection in max parameter.
        /// </summary>
        [TestMethod]
        public void PerfCounter_SimulateReverseWarning_StatusWarning()
        {
            var perfCounter = new PerfCounter("Memory", "Available MBytes", "none", "AvailableMBytes", "MB", "95%", "10%", "0", "automemory");
            CalculatePerfCounter(perfCounter);
            Assert.AreEqual(perfCounter.CounterStatus.GetNagiosExitCode(), 1);
        }
        /// <summary>
        /// Simulate reverse critical status, performance counter value has to be greater than critical.
        /// Critical is 90% for Available MBytes.
        /// Check also % interpretation is correct because max is specified and critical is a percent with MB value.
        /// Check also memory auto detection in max parameter.
        /// </summary>
        [TestMethod]
        public void PerfCounter_SimulateReverseCritical_StatusCritical()
        {
            var perfCounter = new PerfCounter("Memory", "Available MBytes", "none", "AvailableMBytes", "MB", "95%", "90%", "0", "automemory");
            CalculatePerfCounter(perfCounter);
            Assert.AreEqual(perfCounter.CounterStatus.GetNagiosExitCode(), 2);
        }
        /// <summary>
        /// Simulate only greater warning check.
        /// Warning is gt5 for % Processor Time, status has to be warning
        /// </summary>
        [TestMethod]
        public void PerfCounter_SimulateOnlyGreaterWarning_StatusWarning()
        {
            var perfCounter = new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "gt5", "none", "0", "100");
            CalculatePerfCounter(perfCounter);
            Assert.AreEqual(perfCounter.CounterStatus.GetNagiosExitCode(), 1);
        }
        /// <summary>
        /// Simulate only greater critical check.
        /// Critical is gt5 for % Processor Time, status has to be critical
        /// </summary>
        [TestMethod]
        public void PerfCounter_SimulateOnlyGreaterCritical_StatusCritical()
        {
            var perfCounter = new PerfCounter("Processor", "% Processor Time", "_Total", "ProcessorTime", "%", "none", "gt5", "0", "100");
            CalculatePerfCounter(perfCounter);
            Assert.AreEqual(perfCounter.CounterStatus.GetNagiosExitCode(), 2);
        }
        /// <summary>
        /// Simulate only less warning check.
        /// Warning is lt90 for Available MBytes, status has to be warning
        /// </summary>
        [TestMethod]
        public void PerfCounter_SimulateOnlyLessWarning_StatusWarning()
        {
            var perfCounter = new PerfCounter("Memory", "Available MBytes", "none", "AvailableMBytes", "MB", "lt90%", "none", "0", "automemory");
            CalculatePerfCounter(perfCounter);
            Assert.AreEqual(perfCounter.CounterStatus.GetNagiosExitCode(), 1);
        }
        /// <summary>
        /// Simulate only less critical check.
        /// Critical is lt90 for Available MBytes, status has to be critical
        /// </summary>
        [TestMethod]
        public void PerfCounter_SimulateOnlyLessCritical_StatusCritical()
        {
            var perfCounter = new PerfCounter("Memory", "Available MBytes", "none", "AvailableMBytes", "MB", "none", "lt90%", "0", "automemory");
            CalculatePerfCounter(perfCounter);
            Assert.AreEqual(perfCounter.CounterStatus.GetNagiosExitCode(), 2);
        }
        /// <summary>
        /// Test network interface speed auto detection
        /// </summary>
        [TestMethod]
        public void PerfCounter_InterfaceSpeedAutodetection_InterfaceSpeedAsMax()
        {
            var perfCounter = new PerfCounter("Network Adapter", "Bytes Total/Sec", "autonetwork", "BytesTotalSec", "B", "80%", "90%", "0", "autonetwork");
            CalculatePerfCounter(perfCounter);
            Assert.AreEqual(perfCounter.GetMax(), GetNetworkInterfaceSpeed(GetNetworkInterface()));
        }
        /// <summary>
        /// Test system memory auto detection
        /// </summary>
        [TestMethod]
        public void PerfCounter_SystemMemoryAutodetection_SystemMemoryAsMax()
        {
            var perfCounter = new PerfCounter("Memory", "Available MBytes", "none", "AvailableMBytes", "MB", "10%", "5%", "0", "automemory");
            CalculatePerfCounter(perfCounter);
            Assert.AreEqual(perfCounter.GetMax(), GetTotalMemory("MB"));
        }
        /// <summary>
        /// Check if warning and critical are correctly calculated when both has percent value
        /// </summary>
        [TestMethod]
        public void PerfCounter_WarningAndCriticalPercentCalculation_WarningAndCriticalPercent()
        {
            var perfCounter = new PerfCounter("Memory", "Available MBytes", "none", "AvailableMBytes", "MB", "10%", "5%", "0", "automemory");
            CalculatePerfCounter(perfCounter);
            float systemMemory = GetTotalMemory("MB");
            Assert.AreEqual(perfCounter.GetWarning(), systemMemory * 10 / 100);
            Assert.AreEqual(perfCounter.GetCritical(), systemMemory * 5 / 100);

            perfCounter = new PerfCounter("Network Adapter", "Bytes Total/Sec", "autonetwork", "BytesTotalSec", "B", "80%", "90%", "0", "autonetwork");
            CalculatePerfCounter(perfCounter);
            var interfaceSpeed = GetNetworkInterfaceSpeed(GetNetworkInterface());
            Assert.AreEqual(perfCounter.GetWarning(), interfaceSpeed * 80 / 100);
            Assert.AreEqual(perfCounter.GetCritical(), interfaceSpeed * 90 / 100);
        }
        /// <summary>
        /// Check if average result calculation if correct
        /// </summary>
        [TestMethod]
        public void PerfCounter_CalculateAverageResult_AverageResult()
        {
            var perfCounter = new PerfCounter("Memory", "Available MBytes", "none", "AvailableMBytes", "MB", "10%", "5%", "0", "automemory");
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
        /// <summary>
        /// Simulate performance counter calculation using 3 samples
        /// </summary>
        /// <param name="perfCounter"></param>
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