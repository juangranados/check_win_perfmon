using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
    }
}