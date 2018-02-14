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
    public class PerfCounter
    {
        private PerformanceCounter _performanceCounter;
        private readonly string _friendlyName;
        private readonly string _units;
        private readonly float _warning;
        private readonly float _critical;
        private float _min;
        private float _max;
        private readonly Status _status;
        private readonly bool _check;
        private int _samplesCount;
        private float _result;
        private bool _initialized;
        private bool _disposed;
        private static readonly string Format = "0." + new string('#', 324);
        private readonly bool _verbose;
        private float _nextValue;
        /// <summary>
        /// Get and set for resultString, string that contains result of counter (Ok, warning or critical)
        /// </summary>
        public string ResultString { get; private set; }

        /// <summary>
        /// Get and set for perfstring, string that contains performance data in Icinga/Nagios format
        /// </summary>
        public string PerfString { get; private set; }

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
        /// <param name="status">Status class to store overall status of checks</param>
        /// <param name="verbose">Enable verbose output</param>
        public PerfCounter(string categoryName, string counterName, string instanceName, string friendlyName, string units, string warning, string critical, string min, string max, Status status,bool verbose = false)
        {
            //Initializing variables
            _status = status;
            _verbose = verbose;

            //Show performance counter creation
            WriteVerbose($"Creating performance counter class for: \\{categoryName}\\{counterName}\\{instanceName}");

            //All paramenters must have value
            if (categoryName == null || counterName == null || instanceName == null || friendlyName == null || units == null || warning == null || critical == null || min == null || max == null)
            {
                var e = new Exception($"Incorrect format in xml or empty values for counter {counterName}. Please, check xml file.");
                throw e;
            }
            //Min and max can not be the same
            if (min == max && min != "none" && max != "none")
            {
                var e = new Exception($"Min and max can not be the same on counter {counterName}");
                throw e;
            }
            try
            {
                //Performance counter with instance
                if (instanceName != "none")
                {
                    //Try to detect instance
                    if (instanceName == "auto")
                    {
                        //Detect best network interface
                        if (categoryName == "Network Interface" || categoryName == "Network Adapter")
                        {
                            var tempInstanceName = Utils.GetNetworkInterface().Description;
                            tempInstanceName = tempInstanceName.Replace('#', '_');
                            tempInstanceName = tempInstanceName.Replace('(', '[');
                            tempInstanceName = tempInstanceName.Replace(')', ']');
                            instanceName = tempInstanceName;
                        }
                        //Detect disk 0
                        else if (categoryName == "PhysicalDisk")
                        {
                            instanceName = Utils.GetDiskName();
                        }
                        else
                        {
                            throw new Exception($"Parameter auto not supported for {categoryName} counter instance.");
                        }
                    }
                    _performanceCounter = new PerformanceCounter(categoryName, counterName, instanceName, true);
                }
                //Performance counter without instance
                else
                {
                    _performanceCounter = new PerformanceCounter(categoryName, counterName, true);
                }
                WriteVerbose($"Created read only performance counter \\{_performanceCounter.CategoryName}\\{_performanceCounter.CounterName}\\{_performanceCounter.InstanceName}");
            }
            catch (Exception)
            {
                Console.WriteLine($"{counterName} is invalid");
                throw;
            }
            //Assign friendly name without spaces or non ascii characters
            _friendlyName = Regex.Replace(friendlyName != "none" ? friendlyName : counterName, @"[^A-Za-z0-9]+", "");
            //Counter does have measure units
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
                    Console.WriteLine("Error parsing min. Please, check it is a number.");
                    throw;
                }
            }
            //No min specified
            else
            {
                _min = -1;
            }
            //Parse max into float
            if (max != "none")
            {
                if (max == "auto")
                {
                    if (categoryName == "Memory")
                    {
                        _max = Utils.GetTotalMemory(units);
                    }
                    else if (categoryName == "Network Interface" || categoryName == "Network Adapter")
                    {
                        _max = Utils.GetNetworkInterfaceSpeed(instanceName);
                    }
                }
                else
                {
                    try
                    {
                        _max = float.Parse(max, CultureInfo.InvariantCulture.NumberFormat);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Error parsing max. Please, check it is a number.");
                        throw;
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
                try
                {
                    ParseIntoFloat(warning,out _warning);
                    ParseIntoFloat(critical, out _critical);

                    //Counter will be checked.
                    _check = true;
                }
                catch (Exception)
                {
                    Console.WriteLine("Error parsing warning and critical.");
                    throw;
                }
            }
            //if warning and critical are "none" counter will not check 
            else
            {
                _warning = 0;
                _critical = 0;
                _check = false;
            }
        }

        private void ParseIntoFloat(string value, out float field)
        {
            if (value.Contains('%'))
            {
                if (_max != -1)
                {
                    var percent = float.Parse(value.Substring(0, value.IndexOf("%", StringComparison.Ordinal)),
                        CultureInfo.InvariantCulture.NumberFormat);
                    field = (_max * percent) / 100;
                }
                else
                {
                    throw new Exception("Can not calculate % of max because is none.");
                }
            }
            else
            {
                field = float.Parse(value, CultureInfo.InvariantCulture.NumberFormat);
            }
        }

        /// <summary>
            /// Calculate value of a counter and generate result if last sample is reached
            /// </summary>
            public void NextValue()
        {
            //Some counters returns zero on first value because they need two values in order to be calculated.
            if (!_initialized)
            {
               Initialize(); 
            }
            //Counter already initialized
            else
            {
                //Add value to total result
                try
                {
                    _nextValue = _performanceCounter.NextValue();
                    WriteVerbose($"Next value of performance counter \\{_performanceCounter.CategoryName}\\{_performanceCounter.CounterName}\\{_performanceCounter.InstanceName}: {_nextValue}");
                    _result = _result + _nextValue;
                    _samplesCount = _samplesCount + 1;
                }
                catch (Exception)
                {
                    Console.WriteLine($"Error on counter {_performanceCounter.CounterName}");
                    throw;
                }
            }
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
                PerfString = $"'{_friendlyName}'={Math.Floor(_result)}{_units};{_warning};{_critical};0;100 ";
            }
            //Units are not a percent
            else
            {
                //Store performance output
                PerfString = $"'{_friendlyName}'={Math.Round(_result, 4, MidpointRounding.AwayFromZero).ToString(Format)}{_units};{_warning.ToString(Format)};{_critical.ToString(Format)}";
                //check if min and max have values to add them in performance output
                if (_min > -1)
                {
                    PerfString = PerfString + $";{_min.ToString(Format)}";
                }
                else
                {
                    PerfString = PerfString + ";";
                }
                if (_max > -1)
                {
                    PerfString = PerfString + $";{_max.ToString(Format)} ";
                }
                else
                {
                    PerfString = PerfString + "; ";
                }
                //If max have a value, calculate percent
                if (_max > 0)
                {
                    //Calculate percent of result and round to zero floats
                    //Calculate percent of warning and critical and round to zero floats
                    //Add new percent to performance output
                    PerfString = PerfString + $"'{_friendlyName}Percent'={Math.Floor(_result * 100 / _max)}%;{Math.Floor(_warning * 100 / _max)};{Math.Floor(_critical * 100 / _max)};0;100 ";
                }
            }
        }
        /// <summary>
        /// Calculate result of performance counter check and saves it in resultString
        /// </summary>
        private void CalculateStatus()
        {
            //Warning greater than critical->Counter has to be less than warning and critical to be ok.
            if (_warning < _critical)
            {
                WriteVerbose($"Average result for performance counter \\{_performanceCounter.CategoryName}\\{_performanceCounter.CounterName}\\{_performanceCounter.InstanceName} = {_result} -> Must be less than {_critical} and {_warning} to be ok");
                //Status critical
                if (_result >= _critical)
                {
                    //Change global status to critical
                    _status.Critical = true;
                    //Generate error message
                    ResultString = $"{_friendlyName} = {Math.Round(_result, 4, MidpointRounding.AwayFromZero).ToString(Format)} critical.";
                    WriteVerbose($"Performance counter \\{_performanceCounter.CategoryName}\\{_performanceCounter.CounterName}\\{_performanceCounter.InstanceName} = {_result} >= {_critical} -> status critical");
                }
                //Status warning
                else if (_result >= _warning)
                {
                    _status.Warning = true;
                    ResultString = $"{_friendlyName} = {Math.Round(_result, 4, MidpointRounding.AwayFromZero).ToString(Format)} warning.";
                    WriteVerbose($"Performance counter \\{_performanceCounter.CategoryName}\\{_performanceCounter.CounterName}\\{_performanceCounter.InstanceName} = {_result} >= {_warning} -> status warning");
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
                    _status.Critical = true;
                    ResultString = $"{_friendlyName} = {Math.Round(_result, 4, MidpointRounding.AwayFromZero).ToString(Format)} critical.";
                    WriteVerbose($"Performance counter \\{_performanceCounter.CategoryName}\\{_performanceCounter.CounterName}\\{_performanceCounter.InstanceName} = {_result} <= {_critical} -> status critical");
                }
                //Status warning
                else if (_result <= _warning)
                {
                    _status.Warning = true;
                    ResultString = $"{_friendlyName} = {Math.Round(_result, 4, MidpointRounding.AwayFromZero).ToString(Format)} warning.";
                    WriteVerbose($"Performance counter \\{_performanceCounter.CategoryName}\\{_performanceCounter.CounterName}\\{_performanceCounter.InstanceName} = {_result} <= {_warning} -> status warning");
                }
                else
                {
                    WriteVerbose($"Performance counter \\{_performanceCounter.CategoryName}\\{_performanceCounter.CounterName}\\{_performanceCounter.InstanceName} -> status ok");
                }
            }
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
        protected virtual void Dispose(bool disposing)
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