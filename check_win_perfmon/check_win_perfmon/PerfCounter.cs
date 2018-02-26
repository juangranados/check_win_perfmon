using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace check_win_perfmon
{
    /// <summary>
    /// Class to manage performance counter, generate and calculate output in Icinga/Nagios format.
    /// </summary>
    /// https://msdn.microsoft.com/en-us/library/system.diagnostics.performancecounter.instancename.aspx
    public class PerfCounter
    {
        private PerformanceCounter _performanceCounter;
        public NagiosStatus CounterStatus { get; } = new NagiosStatus();
        private readonly string _friendlyName;
        private readonly string _units;
        private readonly float _warning;
        private readonly float _critical;
        private float _min;
        private float _max;
        private readonly bool _check = true;
        private int _samplesCount;
        private float _result;
        private bool _initialized;
        private bool _disposed;
        private static readonly string FormatFloat = "0." + new string('#', 324);
        private readonly bool _verbose;
        private readonly string _interfacename;
        private readonly char _checkOnlyCritical;
        private readonly char _checkOnlyWarning;

        /// <summary>
        /// Get and set for resultString, string that contains result of counter (Ok, warning or critical)
        /// </summary>
        public string ResultString { get; private set; }

        /// <summary>
        /// Get and set for perfstring, string that contains performance data in Icinga/Nagios format
        /// </summary>
        public string PerfString { get; private set; }

        public float GetWarning(){ return _warning; }
        public float GetCritical(){ return _critical; }
        public float GetMax() { return _max; }
        public float GetMin() { return _min; }
        public float GetResult() { return _result; }

        /// <summary>
        /// Constructor od class PerfCounter
        /// </summary>
        /// <param name="categoryName">Category of performance counter</param>
        /// <param name="counterName">Name of performance counter</param>
        /// <param name="instanceName">Instance of performance counter. "none" if performance counter does not have an instance</param>
        /// <param name="friendlyName">Friendly name to return check result in Icinga/Nagios format</param>
        /// <param name="units">Units of performance counter in Icinga/Nagios format</param>
        /// <param name="warning">Warning threshold of performance counter</param>
        /// <param name="critical">Critical threshold of performance counter</param>
        /// <param name="min">Minumal value of performance counter</param>
        /// <param name="max">Max value of performance counter.</param>
        /// <param name="verbose">Enable verbose output</param>
        public PerfCounter(string categoryName, string counterName, string instanceName, string friendlyName, string units, string warning, string critical, string min, string max, bool verbose = false)
        {
            _verbose = verbose;
            //Show performance counter creation
            WriteVerbose($"Creating performance counter class for: \\{categoryName}\\{counterName}\\{instanceName}");

            //All paramenters must have value
            if (categoryName == null || counterName == null || instanceName == null || friendlyName == null || units == null || warning == null || critical == null || min == null || max == null)
            {
                throw new Exception($"Incorrect format/values in xml for counter {counterName}. Please, check xml file.");
            }

            //Min and max can not be the same
            if (min == max && min != "none")
            {
                throw new Exception($"Min and max can not be the same on counter {counterName}");
            }

            try
            {
                //Performance counter with instance
                if (instanceName != "none")
                {
                    //Try to detect instance
                    if (instanceName == "auto")
                    {
                        switch (categoryName)
                        {
                            case "Network Interface":
                            case "Network Adapter":
                                _interfacename = Utils.GetNetworkInterface();
                                instanceName = Utils.NormalizeNetworkInterface(_interfacename);
                                if (instanceName == "unknown")
                                {
                                    throw new ArgumentException($"Error detecting network interface on \\{categoryName}\\{counterName}.", instanceName);
                                }
                                break;
                            case "PhysicalDisk":
                                instanceName = Utils.GetDiskName();
                                if (instanceName == "_total")
                                {
                                    throw new ArgumentException($"Error detecting physical disk 0 on \\{categoryName}\\{counterName}.", instanceName);
                                }
                                break;
                            default:
                                throw new ArgumentException(
                                    $"Parameter auto not supported for \\{categoryName}\\{counterName} counter instance.", instanceName);
                        }
                        WriteVerbose($"Detected instance {instanceName} for counter \\{categoryName}\\{counterName}");
                    }

                    //Create performance counter
                    _performanceCounter = new PerformanceCounter(categoryName, counterName, instanceName, true);
                }
                //Performance counter without instance
                else
                {
                    _performanceCounter = new PerformanceCounter(categoryName, counterName, true);
                }

                WriteVerbose($"Created read only performance counter \\{_performanceCounter.CategoryName}\\{_performanceCounter.CounterName}\\{_performanceCounter.InstanceName}");
            }
            catch (Exception e)
            {
                throw new Exception($"Error creating performance counter \\{categoryName}\\{counterName}: " + e.Message);
            }
            //Assign friendly name without spaces or non ascii characters
            _friendlyName = Regex.Replace(friendlyName != "none" ? friendlyName : counterName, @"[^A-Za-z0-9]+", "");
            //Check if counter has measure units
            _units = units != "none" ? units : "";
            //Parse min into float
            if (min != "none")
            {
                try
                {
                    _min = float.Parse(min, CultureInfo.InvariantCulture.NumberFormat);
                }
                catch (Exception)
                {
                    throw new ArgumentException($"Error parsing min in counter \\{categoryName}\\{counterName}. Please, check it is a number.", min);
                }
            }
            //No min specified
            else
            {
                _min = -1;
            }
            //Parse max into float
            if (max != "none" && max != "0")
            {
                if (max == "auto")
                {
                    WriteVerbose($"Detecting max for: \\{categoryName}\\{counterName}");
                    switch (categoryName)
                    {
                        case "Memory":
                        case "SQLServer:Memory Manager":
                        case "MSSQL$MICROSOFT##WID:Memory Manager":
                            WriteVerbose($"Getting system memory");
                            _max = Utils.GetTotalMemory(units);
                            if (_max <= 0)
                            {
                                throw new ArgumentException($"Error detecting system memory in counter \\{categoryName}\\{counterName}.", max);
                            }
                            break;
                        case "Network Interface":
                        case "Network Adapter":
                            WriteVerbose($"Getting interface {_interfacename} speed");
                            _max = Utils.GetNetworkInterfaceSpeed(_interfacename);
                            if (_max <= 0)
                            {
                                throw new ArgumentException($"Error detecting interface {_interfacename} speed in counter \\{categoryName}\\{counterName}.", max);
                            }
                            break;
                        default:
                            throw new ArgumentException($"Parameter auto not supported for max in counter {counterName}.",max);
                    }

                    WriteVerbose($"Detected max of: {_max.ToString(FormatFloat)} for: \\{categoryName}\\{counterName}");
                }
                else
                {
                    try
                    {
                        _max = float.Parse(max, CultureInfo.InvariantCulture.NumberFormat);
                    }
                    catch (Exception)
                    {
                        throw new ArgumentException($"Error parsing max in counter {counterName}. Please, check it is a number.",max);
                    }
                }
            }
            //No max specified
            else
            {
                _max = -1;
            }
            //Parse warning and critical into float
            if (warning != "none" && critical != "none")
            {
                ParseIntoFloat(warning,out _warning);
                ParseIntoFloat(critical, out _critical);
            }
            else if (warning != "none")
            {
                _critical = 0;
                if ((warning[0] != '>' && warning[0] != '<') || warning.Length < 3)
                {
                    throw new ArgumentException("If critical is none, only warning will check, you need to specify <= or >= before its value in order to calculate it.",warning);
                }
                _checkOnlyWarning = warning[0];
                ParseIntoFloat(warning.Substring(2), out _warning);
            }
            else if (critical != "none")
            {
                _warning = 0;
                if ((critical[0] != '>' && critical[0] != '<') || critical.Length < 2)
                {
                    throw new ArgumentException("If warning is none, only critical will check, you need to specify <= or >= before its value in order to calculate it.", critical);
                }
                _checkOnlyCritical = critical[0];
                ParseIntoFloat(critical.Substring(2), out _critical);
            }
            //if warning and critical are "none" counter will not check 
            else
            {
                _warning = 0;
                _critical = 0;
                _check = false;
            }
        }
        /// <summary>
        /// Parse warning and critical in to float and calculate them if are percent.
        /// </summary>
        /// <param name="value">Value of warning or critical</param>
        /// <param name="field">Value parsed</param>
        private void ParseIntoFloat(string value, out float field)
        {
            //Min or max are percents
            if (value.Contains('%'))
            {
                //Calculate percent
                if (_max > 0)
                {
                    var percent = float.Parse(value.Substring(0, value.IndexOf("%", StringComparison.Ordinal)),
                        CultureInfo.InvariantCulture.NumberFormat);
                    field = _max * percent / 100;
                }
                else
                {
                    throw new InvalidOperationException($"Can not calculate % of max because is none or zero in counter {_friendlyName}.");
                }
            }
            else
            {
                try
                {
                    field = float.Parse(value, CultureInfo.InvariantCulture.NumberFormat);
                }
                catch (Exception)
                {
                    throw new ArgumentException($"Error parsing warning or critical in counter {_friendlyName}. Please, check it is a number.", nameof(field));
                }
            }
        }

        /// <summary>
            /// Calculate value of a counter and generate result if last sample is reached
            /// </summary>
        public float NextValue()
        {
            //Some counters returns zero on first value because they need two values in order to be calculated.
            if (!_initialized)
            {
                throw new InvalidOperationException($"Counter {_friendlyName} has not been inicialized.");
            }

            var nextValue = _performanceCounter.NextValue();
            _result = _result + nextValue;
            _samplesCount = _samplesCount + 1;

            WriteVerbose($"Next value of performance counter \\{_performanceCounter.CategoryName}\\{_performanceCounter.CounterName}\\{_performanceCounter.InstanceName}: {nextValue}");

            return nextValue;
        }
        /// <summary>
        /// Initialize performance counter for new calculations
        /// </summary>
        public void Initialize()
        {
            _performanceCounter.NextValue();
            _initialized = true;
            _samplesCount = 0;
            _result = 0;
            CounterStatus.Initialize();

            WriteVerbose($"Initialized performance counter \\{_performanceCounter.CategoryName}\\{_performanceCounter.CounterName}\\{_performanceCounter.InstanceName}");
        }
        /// <summary>
        /// Calculate performance and status
        /// </summary>
        public void Calculate()
        {
            WriteVerbose($"{_samplesCount} samples taken for performance counter \\{_performanceCounter.CategoryName}\\{_performanceCounter.CounterName}\\{_performanceCounter.InstanceName}. Calculating performance and status.");
            //Calculate average of samples
            _result = _result / _samplesCount;
            CalculatePerformance();
            //Check status of a counter
            if (_check)
            {
                CalculateStatus();
            }
            _initialized = false;
        }

        /// <summary>
        /// Calculate performance output and saves it in perfString
        /// </summary>
        private void CalculatePerformance()
        {
            //Units are percent
            if (_units == "%")
            {
                //Max and min are 0,100
                _max = 100;
                _min = 0;

                //Store performance output
                PerfString = $"'{_friendlyName}'={Math.Floor(_result)}{_units};{_warning};{_critical};0;100";
            }
            //Units are not a percent
            else
            {
                //Store performance output
                PerfString = $"'{_friendlyName}'={Math.Round(_result, 4, MidpointRounding.AwayFromZero).ToString(FormatFloat)}{_units};{_warning.ToString(FormatFloat)};{_critical.ToString(FormatFloat)}";
                //check if min and max have values to add them in performance output
                if (_min > -1)
                {
                    PerfString = PerfString + $";{_min.ToString(FormatFloat)}";
                }
                else
                {
                    PerfString = PerfString + ";";
                }
                if (_max > -1)
                {
                    PerfString = PerfString + $";{_max.ToString(FormatFloat)}";
                }
                else
                {
                    PerfString = PerfString + ";";
                }
                //If max has a value, calculate percent
                if (_max > 0)
                {
                    //Calculate percent of result and round to zero floats
                    //Calculate percent of warning and critical and round to zero floats
                    //Add new percent to performance output
                    PerfString = PerfString + $" '{_friendlyName}Percent'={Math.Floor(_result * 100 / _max)}%;{Math.Floor(_warning * 100 / _max)};{Math.Floor(_critical * 100 / _max)};0;100";
                }
            }
        }
        /// <summary>
        /// Calculate result of performance counter check and saves it in resultString
        /// </summary>
        private void CalculateStatus()
        {
            switch (_checkOnlyWarning)
            {
                case '<':
                    WriteVerbose($"Average result for performance counter \\{_performanceCounter.CategoryName}\\{_performanceCounter.CounterName}\\{_performanceCounter.InstanceName} = {_result} -> Must be greater than {_warning} to be ok");
                    if (_result <= _warning)
                    {
                        RegisterStatusOnCounter("warning");
                    }
                    else
                    {
                        WriteVerbose($"Performance counter \\{_performanceCounter.CategoryName}\\{_performanceCounter.CounterName}\\{_performanceCounter.InstanceName} -> status ok");
                    }
                    return;
                case '>':
                    WriteVerbose($"Average result for performance counter \\{_performanceCounter.CategoryName}\\{_performanceCounter.CounterName}\\{_performanceCounter.InstanceName} = {_result} -> Must be less than {_warning} to be ok");
                    if (_result >= _warning)
                    {
                        RegisterStatusOnCounter("warning");
                    }
                    else
                    {
                        WriteVerbose($"Performance counter \\{_performanceCounter.CategoryName}\\{_performanceCounter.CounterName}\\{_performanceCounter.InstanceName} -> status ok");
                    }
                    return;
            }
            switch (_checkOnlyCritical)
            {
                case '<':
                    WriteVerbose($"Average result for performance counter \\{_performanceCounter.CategoryName}\\{_performanceCounter.CounterName}\\{_performanceCounter.InstanceName} = {_result} -> Must be greater than {_critical} to be ok");
                    if (_result <= _critical)
                    {
                        RegisterStatusOnCounter("critical");
                    }
                    else
                    {
                        WriteVerbose($"Performance counter \\{_performanceCounter.CategoryName}\\{_performanceCounter.CounterName}\\{_performanceCounter.InstanceName} -> status ok");
                    }
                    return;
                case '>':
                    WriteVerbose($"Average result for performance counter \\{_performanceCounter.CategoryName}\\{_performanceCounter.CounterName}\\{_performanceCounter.InstanceName} = {_result} -> Must be less than {_critical} to be ok");
                    if (_result >= _critical)
                    {
                        RegisterStatusOnCounter("critical");
                    }
                    else
                    {
                        WriteVerbose($"Performance counter \\{_performanceCounter.CategoryName}\\{_performanceCounter.CounterName}\\{_performanceCounter.InstanceName} -> status ok");
                    }
                    return;
            }

            //Warning greater than critical->Counter has to be less than warning and critical to be ok.
            if (_warning < _critical)
            {
                WriteVerbose($"Average result for performance counter \\{_performanceCounter.CategoryName}\\{_performanceCounter.CounterName}\\{_performanceCounter.InstanceName} = {_result} -> Must be less than {_critical} and {_warning} to be ok");
                //Status critical
                if (_result >= _critical)
                {
                    RegisterStatusOnCounter("critical");
                }
                //Status warning
                else if (_result >= _warning)
                {
                    RegisterStatusOnCounter("warning");
                }
                else
                {
                    WriteVerbose($"Performance counter \\{_performanceCounter.CategoryName}\\{_performanceCounter.CounterName}\\{_performanceCounter.InstanceName} -> status ok");
                }
            }
            //Warning less than critical->Counter has to be greater than warning and critical to be ok.
            else
            {
                WriteVerbose($"Average result for performance counter \\{_performanceCounter.CategoryName}\\{_performanceCounter.CounterName}\\{_performanceCounter.InstanceName} = {_result} -> Must be greater than {_critical} and {_warning} to be ok");
                //Status critical
                if (_result <= _critical)
                {
                    RegisterStatusOnCounter("critical");
                }
                //Status warning
                else if (_result <= _warning)
                {
                    RegisterStatusOnCounter("warning");
                }
                else
                {
                    WriteVerbose($"Performance counter \\{_performanceCounter.CategoryName}\\{_performanceCounter.CounterName}\\{_performanceCounter.InstanceName} -> status ok");
                }
            }
        }

        private void RegisterStatusOnCounter(string status)
        {
            if (status == "warning")
            {
                CounterStatus.SetWarning();
                WriteVerbose($"Performance counter \\{_performanceCounter.CategoryName}\\{_performanceCounter.CounterName}\\{_performanceCounter.InstanceName} = {_result} >= {_warning} -> status warning");
            }
            else
            {
                CounterStatus.SetCritical();
                WriteVerbose($"Performance counter \\{_performanceCounter.CategoryName}\\{_performanceCounter.CounterName}\\{_performanceCounter.InstanceName} = {_result} >= {_critical} -> status critical");
            }
            ResultString = $"{_friendlyName} = {Math.Round(_result, 4, MidpointRounding.AwayFromZero).ToString(FormatFloat)} {status}.";
        }

        private void WriteVerbose(string output)
        {
            if (_verbose)
            {
                Console.WriteLine($"[{DateTime.Now.ToString("MM/dd/yyyy-HH:mm:ss.fff", DateTimeFormatInfo.InvariantInfo)}] {output}");
            }
        }
        /// <summary>
        /// Dispose method
        /// </summary>
        /// <param name="disposing"> Is disposing</param>
        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    WriteVerbose($"Disposing Performance counter \\{_performanceCounter.CategoryName}\\{_performanceCounter.CounterName}\\{_performanceCounter.InstanceName}");
                    _performanceCounter.Close();
                    _performanceCounter.Dispose();
                    _performanceCounter = null;
                }
                // Disposed of any unmanaged objects. Not any
                _disposed = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Destructor
        /// </summary>
        ~PerfCounter()
        {
            Dispose(false);
        }
    }
}