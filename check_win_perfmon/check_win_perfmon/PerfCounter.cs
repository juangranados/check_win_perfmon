﻿using System;
using System.Collections.Generic;
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
        public NagiosStatus CounterStatus { get; } = new NagiosStatus();
        private readonly string _friendlyName;
        private readonly string _units;
        private readonly float _warning;
        private readonly float _critical;
        private float _min;
        private float _max;
        private readonly bool _check;
        private int _samplesCount;
        private float _result;
        private bool _initialized;
        private bool _disposed;
        private static readonly string Format = "0." + new string('#', 324);
        private readonly bool _verbose;
        private readonly Dictionary<char,char> _networkInterfaceReplacements = new Dictionary<char, char> { { '#', '_' }, { '(', '[' }, { ')', ']' } };


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
        /// <param name="verbose">Enable verbose output</param>
        public PerfCounter(string categoryName, string counterName, string instanceName, string friendlyName, string units, string warning, string critical, string min, string max, bool verbose = false)
        {
            _verbose = verbose;
            //Show performance counter creation
            WriteVerbose($"Creating performance counter class for: \\{categoryName}\\{counterName}\\{instanceName}");

            //All paramenters must have value
            if (categoryName == null || counterName == null || instanceName == null || friendlyName == null || units == null || warning == null || critical == null || min == null || max == null)
            {
                throw new ArgumentNullException($"Incorrect format/values in xml for counter {counterName}. Please, check xml file.");
            }

            //Min and max can not be the same
            if (min == max && min != "none")
            {
                throw new ArgumentException($"Min and max can not be the same on counter {counterName}");
            }

            try
            {
                //Performance counter with instance
                if (instanceName != "none")
                {
                    //Try to detect instance
                    if (instanceName == "auto")
                    {
                        WriteVerbose($"Detecting instance for: \\{categoryName}\\{counterName}");
                        switch (categoryName)
                        {
                            case "Network Interface":
                            case "Network Adapter":
                                instanceName = NormalizeNetworkInterface(Utils.GetNetworkInterface().Description);
                                break;
                            case "PhysicalDisk":
                                instanceName = Utils.GetDiskName();
                                break;
                            default:
                                throw new ArgumentException(
                                    $"Parameter auto not supported for {categoryName} counter instance.", instanceName);
                        }
                    }
                    WriteVerbose($"{instanceName} detected");
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
                throw new ApplicationException($"\\{_performanceCounter.CategoryName}\\{_performanceCounter.CounterName}\\{_performanceCounter.InstanceName} is invalid: " + e.Message);
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
                    throw new ArgumentException($"Error parsing min in counter {counterName}. Please, check it is a number.", min);
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
                    WriteVerbose($"Detecting max for: {categoryName}");
                    switch (categoryName)
                    {
                        case "Memory":
                        case "SQLServer:Memory Manager":
                            WriteVerbose($"Getting system memory");
                            _max = Utils.GetTotalMemory(units);
                            break;
                        case "Network Interface":
                        case "Network Adapter":
                            WriteVerbose($"Getting interface {instanceName} speed");
                            _max = Utils.GetNetworkInterfaceSpeed(DeNormalizeNetworkInterface(instanceName));
                            break;
                        default:
                            throw new ArgumentException($"Parameter auto not supported for max in counter {counterName}.",max);
                    }
                    WriteVerbose($"Detected max of: {_max} for: \\{categoryName}\\{counterName}\\{instanceName}");
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

                //Counter will be checked.
                _check = true;
            }
            //if warning or critical are "none" counter will not check 
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
                if (_max > 0)
                {
                    var percent = float.Parse(value.Substring(0, value.IndexOf("%", StringComparison.Ordinal)),
                        CultureInfo.InvariantCulture.NumberFormat);
                    field = _max * percent / 100;
                }
                else
                {
                    throw new ArgumentException($"Can not calculate % of max because is none or zero in counter {_friendlyName}.");
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
                throw new ApplicationException($"Counter {_friendlyName} has not been inicialized");
            }

            var nextValue = _performanceCounter.NextValue();
            _result = _result + nextValue;
            _samplesCount = _samplesCount + 1;
            WriteVerbose($"Next value of performance counter \\{_performanceCounter.CategoryName}\\{_performanceCounter.CounterName}\\{_performanceCounter.InstanceName}: {nextValue}");
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
                    PerfString = PerfString + $";{_max.ToString(Format)}";
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
            //Warning greater than critical->Counter has to be less than warning and critical to be ok.
            if (_warning < _critical)
            {
                WriteVerbose($"Average result for performance counter \\{_performanceCounter.CategoryName}\\{_performanceCounter.CounterName}\\{_performanceCounter.InstanceName} = {_result} -> Must be less than {_critical} and {_warning} to be ok");
                //Status critical
                if (_result >= _critical)
                {
                    //Change global status to critical
                    CounterStatus.SetCritical();
                    //Generate error message
                    ResultString = $"{_friendlyName} = {Math.Round(_result, 4, MidpointRounding.AwayFromZero).ToString(Format)} critical.";
                    WriteVerbose($"Performance counter \\{_performanceCounter.CategoryName}\\{_performanceCounter.CounterName}\\{_performanceCounter.InstanceName} = {_result} >= {_critical} -> status critical");
                }
                //Status warning
                else if (_result >= _warning)
                {
                    CounterStatus.SetWarning();
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
                    CounterStatus.SetCritical();
                    ResultString = $"{_friendlyName} = {Math.Round(_result, 4, MidpointRounding.AwayFromZero).ToString(Format)} critical.";
                    WriteVerbose($"Performance counter \\{_performanceCounter.CategoryName}\\{_performanceCounter.CounterName}\\{_performanceCounter.InstanceName} = {_result} <= {_critical} -> status critical");
                }
                //Status warning
                else if (_result <= _warning)
                {
                    CounterStatus.SetWarning();
                    ResultString = $"{_friendlyName} = {Math.Round(_result, 4, MidpointRounding.AwayFromZero).ToString(Format)} warning.";
                    WriteVerbose($"Performance counter \\{_performanceCounter.CategoryName}\\{_performanceCounter.CounterName}\\{_performanceCounter.InstanceName} = {_result} <= {_warning} -> status warning");
                }
                else
                {
                    WriteVerbose($"Performance counter \\{_performanceCounter.CategoryName}\\{_performanceCounter.CounterName}\\{_performanceCounter.InstanceName} -> status ok");
                }
            }
        }

        private string NormalizeNetworkInterface(string networkInterface)
        {
            return _networkInterfaceReplacements.Aggregate(networkInterface, (result, s) => result.Replace(s.Key, s.Value));
        }
        private string DeNormalizeNetworkInterface(string networkInterface)
        {
            return _networkInterfaceReplacements.Aggregate(networkInterface, (result, s) => result.Replace(s.Value, s.Key));
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