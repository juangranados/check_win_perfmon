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
        // Performance counter System object.
        private PerformanceCounter _performanceCounter;
        // Status of performance counter: OK, Warning or Critical
        public NagiosStatus CounterStatus { get; } = new NagiosStatus();
        // Friendly name of performance counter without spaces or non ASCII characters
        private readonly string _friendlyName;
        // Units of performance metrics in Nagios / Icinga format
        private readonly string _units;
        // Warning threshold
        private readonly float _warning;
        // Critical threshold
        private readonly float _critical;
        // Max value for counter
        private float _min;
        // Min value for counter
        private float _max;
        // Set if counter will be checked based on thresholds
        private readonly bool _check = true;
        // Samples taken for counter
        private int _samplesCount;
        // Average result based on samplesCount
        private float _result;
        // Set if performance counter has been initialized because some performance counters returns 0 on first value.
        private bool _initialized;
        // Set if performance counter has been disposed
        private bool _disposed;
        // Format string to convert float to string without scientific notation.
        private static readonly string FormatFloat = "0." + new string('#', 324);
        // Set verbose output to console
        private readonly bool _verbose;
        // Stores original network interface name before change forbidden characters.
        private readonly string _interfacename;
        // Set if only critical threshold is checked
        private readonly char _checkOnlyCritical;
        // Set if only warning threshold is checked
        private readonly char _checkOnlyWarning;
        // Stores performance counter in format \\_performanceCounter.CategoryName\\_performanceCounter.CounterName\\_performanceCounter.InstanceName
        private readonly string _performanceCounterString;

        /// <summary>
        /// Get and set for resultString, string that contains result of counter (OK, warning or critical)
        /// </summary>
        public string ResultString { get; private set; }

        /// <summary>
        /// Get and set for PerfString, string that contains performance data in Icinga/Nagios format
        /// </summary>
        public string PerfString { get; private set; }
        // Getters
        public float GetWarning(){ return _warning; }
        public float GetCritical(){ return _critical; }
        public float GetMax() { return _max; }
        public float GetMin() { return _min; }
        public float GetResult() { return _result; }

        /// <summary>
        /// Constructor of class PerfCounter
        /// </summary>
        /// <param name="categoryName">Category of performance counter</param>
        /// <param name="counterName">Name of performance counter</param>
        /// <param name="instanceName">Instance of performance counter. "none" if performance counter does not have an instance</param>
        /// <param name="friendlyName">Friendly name to return check result in Icinga/Nagios format</param>
        /// <param name="units">Units of performance counter in Icinga/Nagios format</param>
        /// <param name="warning">Warning threshold of performance counter</param>
        /// <param name="critical">Critical threshold of performance counter</param>
        /// <param name="min">Minimal value of performance counter</param>
        /// <param name="max">Max value of performance counter.</param>
        /// <param name="verbose">Enable verbose output</param>
        public PerfCounter(string categoryName, string counterName, string instanceName, string friendlyName, string units, string warning, string critical, string min, string max, bool verbose = false)
        {
            _verbose = verbose;

            //Write performance counter creation
            WriteVerbose($"Creating performance counter class for: \\{categoryName}\\{counterName}\\{instanceName}");

            //All parameters must have value
            if (categoryName == null || counterName == null || instanceName == null || friendlyName == null || units == null || warning == null || critical == null || min == null || max == null)
            {
                throw new Exception($"Incorrect format/values in XML for counter {counterName}. Values can not be null");
            }

            //Min and max can not be the same, only in case of "none"
            if (min == max && min != "none")
            {
                throw new ArgumentException($"Min and max can not be the same on {counterName}",nameof(max));
            }
            // Create new performance counter object
            try
            {
                // Performance counter with instance
                if (instanceName != "none")
                {
                    //If instance name is "auto", try to detect instance
                    if (instanceName == "auto")
                    {
                        switch (categoryName)
                        {
                            //Auto detect physical interface or adapter.
                            case "Network Interface":
                            case "Network Adapter":
                                // Get network interface connected to Internet.
                                _interfacename = Utils.GetNetworkInterface();
                                // Check if _interfacename has forbidden characters and change to performance counters convention
                                instanceName = Utils.NormalizeNetworkInterface(_interfacename);
                                // If Utils.GetNetworkInterface() cannot detect network interface connected to Internet, throw exception
                                if (instanceName == "unknown")
                                {
                                    throw new ArgumentException($"Error detecting network interface on \\{categoryName}\\{counterName}.", nameof(instanceName));
                                }
                                break;
                            //Auto detect physical disk 
                            case "PhysicalDisk":
                                // Auto detect physical disk 0
                                instanceName = Utils.GetDiskName();
                                // If Utils.GetDiskName() cannot detect physical disk 0, throw exception
                                if (instanceName == "_total")
                                {
                                    throw new ArgumentException($"Error detecting physical disk 0 on \\{categoryName}\\{counterName}.", nameof(instanceName));
                                }
                                break;
                            // Parameter auto for instanceName is only supported for Network Interface, Network Adapter and PhysicalDisk
                            default:
                                throw new ArgumentException(
                                    $"Parameter auto not supported for \\{categoryName}\\{counterName} counter instance.", nameof(instanceName));
                        }
                        // Write verbose auto detection has been successful
                        WriteVerbose($"Detected instance {instanceName} for counter \\{categoryName}\\{counterName}");
                    }

                    // Create performance counter with instance
                    _performanceCounter = new PerformanceCounter(categoryName, counterName, instanceName, true);
                    // Save performance counter string to ease verbose outputs.
                    _performanceCounterString =
                        $"\\{_performanceCounter.CategoryName}\\{_performanceCounter.CounterName}\\{_performanceCounter.InstanceName}";
                }
                //Performance counter without instance
                else
                {
                    // Create performance counter without instance
                    _performanceCounter = new PerformanceCounter(categoryName, counterName, true);
                    // Save performance counter string to ease verbose outputs.
                    _performanceCounterString =
                        $"\\{_performanceCounter.CategoryName}\\{_performanceCounter.CounterName}";
                }
                // Write verbose creation of performance counter object has been successful
                WriteVerbose($"Created read only performance counter {_performanceCounterString}");
            }
            // Something went wrong on performance object creation, throws an exception
            catch (Exception e)
            {
                throw new Exception($"Error creating performance counter \\{categoryName}\\{counterName}: " + e.Message);
            }

            // Assign friendly name without spaces or non ASCII characters
            _friendlyName = Regex.Replace(friendlyName != "none" ? friendlyName : counterName, @"[^A-Za-z0-9]+", "");
            
            // Check if counter has measure units, if not, assign empty string to compile with Nagios standard 
            _units = units != "none" ? units : "";
            
            // Min has value
            if (min != "none")
            {
                try
                {
                    // Parse min into float
                    _min = float.Parse(min, CultureInfo.InvariantCulture.NumberFormat);
                }
                // Something went wrong on parsing min into float, throws an exception
                catch (Exception)
                {
                    throw new ArgumentException($"Error parsing min in counter {_performanceCounterString}. Please, check it is a number.", nameof(min));
                }
            }
            //No min specified
            else
            {
                // Set min to -1 to avoid add it in performance output
                _min = -1;
            }
            // Max has value and is not zero
            if (max != "none" && max != "0")
            {
                // Max is auto, trying auto detection
                if (max == "auto")
                {
                    // Write verbose auto detection for max
                    WriteVerbose($"Detecting max for: {_performanceCounterString}");
                    // Inspect categoryName in order to perform auto detection
                    switch (categoryName)
                    {
                        // Auto detect system memory
                        case "Memory":
                        case "SQLServer:Memory Manager":
                        case "MSSQL$MICROSOFT##WID:Memory Manager":
                            // Write verbose auto detection for system memory
                            WriteVerbose("Getting system memory");
                            // Try to detect system installed memory
                            _max = Utils.GetTotalMemory(units);
                            // Utils.GetTotalMemory could not detect system memory, throws an exception
                            if (_max <= 0)
                            {
                                throw new ArgumentException($"Error detecting system memory in counter {_performanceCounterString}.", nameof(max));
                            }
                            break;
                        // Auto detect network speed
                        case "Network Interface":
                        case "Network Adapter":
                            // If _interfacename is null, network adapter name has not been auto detected
                            if (_interfacename == null)
                            {
                                // Using interface name provided in instanceName
                                _interfacename = instanceName;
                            }
                            // Write verbose auto detect interface speed
                            WriteVerbose($"Getting interface {_interfacename} speed");
                            // Try to detect interface speed
                            _max = Utils.GetNetworkInterfaceSpeed(_interfacename);
                            // Utils.GetNetworkInterfaceSpeed could not detect interface speed, throws an exception
                            if (_max <= 0)
                            {
                                throw new ArgumentException($"Error detecting interface {_interfacename} speed in counter {_performanceCounterString}.", nameof(max));
                            }
                            break;
                        // Parameter auto for max is only supported for Memory, SQLServer:Memory Manager, MSSQL$MICROSOFT##WID:Memory Manager, Network Interface and Network Adapter categoryName
                        default:
                            throw new ArgumentException($"Parameter auto not supported for max in counter {_performanceCounterString}.", nameof(max));
                    }
                    // Write verbose detection of max has been successful
                    WriteVerbose($"Detected max of: {_max.ToString(FormatFloat)} for counter: {_performanceCounterString}");
                }
                // Max is specified
                else
                {
                    try
                    {
                        // Parse max into float
                        _max = float.Parse(max, CultureInfo.InvariantCulture.NumberFormat);
                    }
                    // Something went wrong on parsing max into float, throws an exception
                    catch (Exception)
                    {
                        throw new ArgumentException($"Error parsing max in counter {_performanceCounterString}. Please, check it is a number.", nameof(max));
                    }
                }
            }
            //No max specified
            else
            {
                // Set max to -1 to avoid add it in performance output and calculate percent
                _max = -1;
            }

            // Warning and critical has values
            if (warning != "none" && critical != "none")
            {
                //Parse warning and critical into float
                ParseIntoFloat(warning,out _warning);
                ParseIntoFloat(critical, out _critical);
            }
            // Critical is none and warning has value, only warning will be checked, user need to specify greater or less to compare
            else if (warning != "none")
            {
                // Set critical to zero for performance counter string 
                _critical = 0;
                // Get first two characters of warning string
                var warningSubstring = warning.Substring(0, warning.Length > 1 ? 2 : warning.Length);
                // Check correct format, has to be lt or gt
                if (warning.Length < 3 && warningSubstring != "lt" && warningSubstring != "gt")
                {
                    // Invalid format for warning
                    throw new ArgumentException($"If critical is none, only warning will be checked. You must specify lt or gt before its value in order to calculate it on counter {_performanceCounterString}.", nameof(warning));
                }
                // Get only first character, less or greater
                _checkOnlyWarning = warning[0];
                // Parse numeric value of warning in to float
                ParseIntoFloat(warning.Substring(2), out _warning);
            }
            // Warning is none and warning has value, only critical will be checked, user need to specify greater or less to compare
            else if (critical != "none")
            {
                // Set warning to zero for performance counter string 
                _warning = 0;
                // Get 2 first characters of critical string
                var criticalSubstring = critical.Substring(0, critical.Length > 1 ? 2 : critical.Length);
                // Check correct format, has to be lt or gt
                if (critical.Length < 3 && criticalSubstring != "lt" && criticalSubstring != "gt")
                {
                    // Invalid format for critical
                    throw new ArgumentException($"If warning is none, only critical will be checked. You must specify lg or gt before its value in order to calculate it on counter {_performanceCounterString}.", nameof(critical));
                }
                // Get only first character, less or greater
                _checkOnlyCritical = critical[0];
                // Parse numeric value of warning in to float
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
                // Max greater than zero, percent could be calculated
                if (_max > 0)
                {
                    try
                    {
                        // Get number before % character
                        var percent = float.Parse(value.Substring(0, value.IndexOf("%", StringComparison.Ordinal)),
                        CultureInfo.InvariantCulture.NumberFormat);
                        // Calculate percent
                        field = _max * percent / 100;
                    }
                    catch (Exception)
                    {
                        // Error on parsing
                        throw new ArgumentException($"Error parsing warning or critical with value {value} in counter on counter {_performanceCounterString}. Please, check it is a number.");
                    }
                }
                // Max is zero or less, cannot calculate percent without max value
                else
                {
                    throw new InvalidOperationException($"Can not calculate % of max because is none or zero in counter on counter {_performanceCounterString}.");
                }
            }
            // Max is not a percent
            else
            {
                try
                {
                    // Parse max into float
                    field = float.Parse(value, CultureInfo.InvariantCulture.NumberFormat);
                }
                catch (Exception)
                {
                    // Error on parsing
                    throw new ArgumentException($"Error parsing warning or critical  with value {value} in counter on counter {_performanceCounterString}. Please, check it is a number.");
                }
            }
        }

        /// <summary>
        /// Calculate next value of a counter and add it to total
       /// </summary>
        public float NextValue()
        {
            // Check if counter has been initialized
            if (!_initialized)
            {
                // Some counters returns zero on first value because they need two values in order to be calculated
                throw new InvalidOperationException($"Counter on counter {_performanceCounterString} has not been initialized.");
            }
            // Generate next value of performance counter
            var nextValue = _performanceCounter.NextValue();
            // Add value to _result
            _result = _result + nextValue;
            // One more sample taken
            _samplesCount = _samplesCount + 1;

            // Write verbose next value taken
            WriteVerbose($"Next value of performance counter on counter {_performanceCounterString}: {nextValue}");

            // Return nextValue for testing purposes
            return nextValue;
        }
        /// <summary>
        /// Initialize performance counter for new calculations.
        /// Some counters returns zero on first value because they need two values in order to be calculated.
        /// </summary>
        public void Initialize()
        {
            // Take one values
            _performanceCounter.NextValue();
            // Set performance counter as initialized
            _initialized = true;
            // Initialize samples counter to zero
            _samplesCount = 0;
            // Initialize total result to zero
            _result = 0;
            // Set counter status equal to OK (0)
            CounterStatus.Initialize();

            // Write verbose counter has been initialized
            WriteVerbose($"Initialized performance counter on counter {_performanceCounterString}");
        }
        /// <summary>
        /// Calculate performance string and status, OK, Warning or Critical based on thresholds
        /// </summary>
        public void Calculate()
        {
            // Write verbose number of samples taken and calculation start
            WriteVerbose($"{_samplesCount} samples taken for performance counter on counter {_performanceCounterString}. Calculating performance and status.");
            // Calculate average of samples based on samples taken
            _result = _result / _samplesCount;
            // Calculate performance string 
            CalculatePerformance();
            // Check status of a counter if check conditions are granted
            if (_check)
            {
                CalculateStatus();
            }
            // Set the need of reinitialization
            _initialized = false;
        }

        /// <summary>
        /// Calculate performance output and saves it in PerfString
        /// </summary>
        private void CalculatePerformance()
        {
            // Check if warning and critical has valid values, if not, leave empty in performance output
            var warning = _warning > 0 ? _warning.ToString(FormatFloat) : "";
            var critical = _critical > 0 ? _critical.ToString(FormatFloat) : "";
            
            //Units are percent
            if (_units == "%")
            {
                //Max and min are 0,100
                _max = 100;
                _min = 0;

                //Store performance output in Nagios / Icinga format
                PerfString = $"'{_friendlyName}'={Math.Floor(_result)}{_units};{warning};{critical};0;100";
            }
            //Units are not a percent
            else
            {
                // Check if min and max has valid values, if not, leave empty in performance output
                var min = _min > -1 ? _min.ToString(FormatFloat) : "";
                var max = _max > 0 ? _max.ToString(FormatFloat) : "";

                //Store performance output in Nagios / Icinga format
                PerfString = $"'{_friendlyName}'={Math.Round(_result, 4, MidpointRounding.AwayFromZero).ToString(FormatFloat)}{_units};{_warning.ToString(FormatFloat)};{_critical.ToString(FormatFloat)};{min};{max}";
                
                //If max has a value, calculate new performance output with percent values
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
        /// Calculate status of performance counter based on thresholds and saves it in resultString
        /// </summary>
        private void CalculateStatus()
        {
            // If only warning has a value, checkOnlyWarning is not empty
            if (_checkOnlyWarning != '\0')
            {
                // Check warning against threshold
                switch (_checkOnlyWarning)
                {
                    // Check status warning -> if performance counter value is less than _warning
                    case 'l':
                        // Write verbose current performance counter value has to be greater than _warning to be OK
                        WriteVerbose(
                            $"Average result for performance counter on counter {_performanceCounterString} = {_result} -> Must be greater than {_warning} to be OK");
                        // If performance counter value is less or equal than _warning -> Warning status of counter
                        if (_result <= _warning)
                        {
                            // Change performance counter status to warning and write verbose
                            RegisterStatusOnCounter(NagiosStatusEnum.Warning);
                            WriteVerbose($"Performance counter on counter {_performanceCounterString} -> status warning");
                        }
                        else
                        {
                            // Write verbose all is OK
                            WriteVerbose($"Performance counter on counter {_performanceCounterString} -> status OK");
                        }
                        // Status checked, exiting
                        return;
                    case 'g':
                        WriteVerbose(
                            $"Average result for performance counter on counter {_performanceCounterString} = {_result} -> Must be less than {_warning} to be OK");
                        if (_result >= _warning)
                        {
                            RegisterStatusOnCounter(NagiosStatusEnum.Warning);
                            WriteVerbose($"Performance counter on counter {_performanceCounterString} -> status warning");
                        }
                        else
                        {
                            WriteVerbose($"Performance counter on counter {_performanceCounterString} -> status OK");
                        }

                        return;
                }
            }

            if (_checkOnlyCritical != '\0')
            {
                switch (_checkOnlyCritical)
                {
                    case 'l':
                        WriteVerbose(
                            $"Average result for performance counter on counter {_performanceCounterString} = {_result} -> Must be greater than {_critical} to be OK");
                        if (_result <= _critical)
                        {
                            RegisterStatusOnCounter(NagiosStatusEnum.Critical);
                            WriteVerbose($"Performance counter on counter {_performanceCounterString} -> status critical");
                        }
                        else
                        {
                            WriteVerbose($"Performance counter on counter {_performanceCounterString} -> status OK");
                        }

                        return;
                    case 'g':
                        WriteVerbose(
                            $"Average result for performance counter on counter {_performanceCounterString} = {_result} -> Must be less than {_critical} to be OK");
                        if (_result >= _critical)
                        {
                            RegisterStatusOnCounter(NagiosStatusEnum.Critical);
                            WriteVerbose($"Performance counter on counter {_performanceCounterString} -> status critical");
                        }
                        else
                        {
                            WriteVerbose($"Performance counter on counter {_performanceCounterString} -> status OK");
                        }

                        return;
                }
            }

            //Warning greater than critical->Counter has to be less than warning and critical to be OK.
            if (_warning < _critical)
            {
                WriteVerbose($"Average result for performance counter on counter {_performanceCounterString} = {_result} -> Must be less than {_critical} and {_warning} to be OK");
                //Status critical
                if (_result >= _critical)
                {
                    RegisterStatusOnCounter(NagiosStatusEnum.Critical);
                }
                //Status warning
                else if (_result >= _warning)
                {
                    RegisterStatusOnCounter(NagiosStatusEnum.Warning);
                }
                else
                {
                    WriteVerbose($"Performance counter on counter {_performanceCounterString} -> status ok");
                }
            }
            //Warning less than critical->Counter has to be greater than warning and critical to be ok.
            else
            {
                WriteVerbose($"Average result for performance counter on counter {_performanceCounterString} = {_result} -> Must be greater than {_critical} and {_warning} to be ok");
                //Status critical
                if (_result <= _critical)
                {
                    RegisterStatusOnCounter(NagiosStatusEnum.Critical);
                }
                //Status warning
                else if (_result <= _warning)
                {
                    RegisterStatusOnCounter(NagiosStatusEnum.Warning);
                }
                else
                {
                    WriteVerbose($"Performance counter on counter {_performanceCounterString} -> status ok");
                }
            }
        }

        private void RegisterStatusOnCounter(NagiosStatusEnum nagiosStatusEnum )
        {
            if (nagiosStatusEnum == NagiosStatusEnum.Warning)
            {
                CounterStatus.SetWarning();
                WriteVerbose($"Performance counter on counter {_performanceCounterString} = {_result} >= {_warning} -> status warning");
            }
            else
            {
                CounterStatus.SetCritical();
                WriteVerbose($"Performance counter on counter {_performanceCounterString} = {_result} >= {_critical} -> status critical");
            }
            ResultString = $"{_friendlyName} = {Math.Round(_result, 4, MidpointRounding.AwayFromZero).ToString(FormatFloat)} {nagiosStatusEnum.ToString().ToLower()}.";
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
                    WriteVerbose($"Disposing Performance counter on counter {_performanceCounterString}");
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