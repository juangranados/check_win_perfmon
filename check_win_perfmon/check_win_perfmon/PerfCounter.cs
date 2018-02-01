using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Linq;
/// <summary>
/// Class to manage performance counter, generate and calculate output in Icinga/Nagios format.
/// </summary>
public class PerfCounter
{
    private PerformanceCounter performanceCounter;
    private string friendlyName;
    private string units;
    private float warning;
    private float critical;
    private float min;
    private float max;
    private Status status;
    private int samples;
    private bool check;
    private int samplesCount = 0;
    private float result = 0;
    private string resultString = null;
    private string perfString = null;
    private bool initialized = false;
    private bool _disposed = false;
    static string format = "0." + new string('#', 324);
    bool verbose = false;
    float nextValue;
    /// <summary>
    /// Get and set for resultString, string that contains result of counter (Ok, warning or critical)
    /// </summary>
    public string ResultString { get => resultString; set => resultString = value; }
    /// <summary>
    /// Get and set for perfstring, string that contains performance data in Icinga/Nagios format
    /// </summary>
    public string PerfString { get => perfString; set => perfString = value; }
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
    /// <param name="samples">Number of performance samples to take</param>
    public PerfCounter(string categoryName, string counterName, string instanceName, string friendlyName, string units, string warning, string critical, string min, string max, Status status, int samples,bool verbose = false)
    {
        //Initializing variables
        this.status = status;
        this.samples = samples;
        this.verbose = verbose;

        //Show performance counter creation
        WriteVerbose($"Creating performance counter \\{categoryName}\\{counterName}\\{instanceName}");

        //All paramenters must have value
        if (categoryName == null || counterName == null || instanceName == null || friendlyName == null || units == null || warning == null || critical == null || min == null || max == null)
        {
            Exception e = new Exception($"Incorrect format in xml or empty values for counter {counterName}. Please, check xml file.");
            throw (e);
        }
        //Min and max can not be the same
        if (min == max && min != "none" && max != "none")
        {
            Exception e = new Exception($"Min and max can not be the same on counter {counterName}");
            throw (e);
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
                    if ((categoryName == "Network Interface") || (categoryName == "Network Adapter"))
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
                        throw new Exception(message: $"Parameter auto not supported for {categoryName} counter instance.");
                    }
                }
                performanceCounter = new PerformanceCounter(categoryName, counterName, instanceName, true);
            }
            //Performance counter without instance
            else
            {
                performanceCounter = new PerformanceCounter(categoryName, counterName, true);
            }
            WriteVerbose($"Created read only performance counter \\{performanceCounter.CategoryName}\\{performanceCounter.CounterName}\\{performanceCounter.InstanceName}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"{counterName} is invalid");
            throw (e);
        }
        //Assign friendly name without spaces or non ascii characters
        if (friendlyName != "none")
        {
            this.friendlyName = Regex.Replace(friendlyName, @"[^A-Za-z0-9]+", "");
        }
        //Gererate friendlyName if not specified
        else
        {
            this.friendlyName = Regex.Replace(counterName, @"[^A-Za-z0-9]+", "");
        }
        //Counter does have measure units
        if (units != "none")
        {
            this.units = units;
        }
        //Counter does not have measure unit
        else
        {
            this.units = "";
        }
        //Parse min into float
        if (min != "none")
        {
            try
            {
                this.min = float.Parse(min, CultureInfo.InvariantCulture.NumberFormat);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error parsing min. Please, check it is a number.");
                throw (e);
            }
        }
        //No min specified
        else
        {
            this.min = -1;
        }
        //Parse max into float
        if (max != "none")
        {
            if (max == "automemory")
            {
                this.max = Utils.GetTotalMemory(units);
            }
           else
            {
                try
                {
                    this.max = float.Parse(max, CultureInfo.InvariantCulture.NumberFormat);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error parsing max. Please, check it is a number.");
                    throw (e);
                }
            }
        }
        //No max specified
        else
        {
            this.max = -1;
        }
        //Parse warning and critical into float
        if (warning != "none" && critical != "none")
        {
            try
            {
                //warning
                if (warning.Contains('%'))
                {
                    if  (this.max != -1)
                    {
                        float percent = float.Parse(warning.Substring(0, warning.IndexOf("%")), CultureInfo.InvariantCulture.NumberFormat);
                        this.warning = this.max * (percent / 100);
                    }
                    else
                    {
                        throw new Exception(message: "Can not calculate % of max because is none.");
                    }
                }
                else
                {
                    this.warning = float.Parse(warning, CultureInfo.InvariantCulture.NumberFormat);
                }
                //Critical
                if (critical.Contains('%'))
                {
                    if (this.max != -1)
                    {
                        float percent = float.Parse(critical.Substring(0, critical.IndexOf("%")), CultureInfo.InvariantCulture.NumberFormat);
                        this.critical = this.max * (percent / 100);
                    }
                    else
                    {
                        throw new Exception(message: "Can not calculate % of max because is none.");
                    }
                }
                else
                {
                    this.critical = float.Parse(critical, CultureInfo.InvariantCulture.NumberFormat);
                }
                //Counter will be checked.
                check = true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error parsing warning and critical.");
                throw (e);
            }
        }
        //if warning and critical are "none" counter will not check 
        else
        {
            this.warning = 0;
            this.critical = 0;
            check = false;
        }
    }
    /// <summary>
    /// Calculate value of a counter and generate result if last sample is reached
    /// </summary>
    public void NextValue()
    {
        //Some counters returns zero on first value because they need two values in order to be calculated.
        if (!initialized)
        {
            //Initialice counter
            performanceCounter.NextValue();
            initialized = true;
            WriteVerbose($"Initialized performance counter \\{performanceCounter.CategoryName}\\{performanceCounter.CounterName}\\{performanceCounter.InstanceName}");
        }
        //Counter already initialized
        else
        {
            //One sample more taken
            samplesCount = samplesCount + 1;
            //Add value to total result
            try
            {
                nextValue = performanceCounter.NextValue();
                WriteVerbose($"Next value of performance counter \\{performanceCounter.CategoryName}\\{performanceCounter.CounterName}\\{performanceCounter.InstanceName}: {nextValue}");
                result = result + nextValue;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error on counter {performanceCounter.CounterName}");
                throw (e);
            }
            //Last sample taken
            if (samples == samplesCount)
            {
                WriteVerbose($"Last sample of {samples} taken for performance counter \\{performanceCounter.CategoryName}\\{performanceCounter.CounterName}\\{performanceCounter.InstanceName}");
                //Calculate average of samples
                result = result / samples;
                CalculatePerformance();
                //Check status of a counter
                if (check)
                {
                    CalculateStatus();
                }
            }
        }
    }
    /// <summary>
    /// Calculate performance output and saves it in perfString
    /// </summary>
    private void CalculatePerformance()
    {
        //Units are percent
        if (units == "%")
        {
            //Max and min are 0,100
            max = 100;
            min = 0;

            //Store performance output
            perfString = $"'{friendlyName}'={Math.Floor(result)}{units};{warning};{critical};0;100 ";
        }
        //Units are not a percent
        else
        {
            //Store performance output
            perfString = $"'{friendlyName}'={(Math.Round(result, 4, MidpointRounding.AwayFromZero)).ToString(format)}{units};{warning.ToString(format)};{critical.ToString(format)}";
            //check if min and max have values to add them in performance output
            if (min > -1)
            {
                perfString = perfString + $";{min.ToString(format)}";
            }
            else
            {
                perfString = perfString + ";";
            }
            if (max > -1)
            {
                perfString = perfString + $";{max.ToString(format)} ";
            }
            else
            {
                perfString = perfString + "; ";
            }
            //If max have a value, calculate percent
            if (max > 0)
            {
                //Calculate percent of result and round to zero floats
                //Calculate percent of warning and critical and round to zero floats
                //Add new percent to performance output
                perfString = perfString + $"'{friendlyName}Percent'={Math.Floor(((result * 100) / max))}%;{Math.Floor(((warning * 100) / max))};{Math.Floor(((critical * 100) / max))};0;100 ";
            }
        }
    }
    /// <summary>
    /// Calculate result of performance counter check and saves it in resultString
    /// </summary>
    private void CalculateStatus()
    {
        //Warning greater than critical->Counter has to be less than warning and critical to be ok.
        if (warning < critical)
        {
            WriteVerbose($"Average result for performance counter \\{performanceCounter.CategoryName}\\{performanceCounter.CounterName}\\{performanceCounter.InstanceName} = {result} -> Must be less than {critical} and {warning} to be ok");
            //Status critical
            if (result >= critical)
            {
                //Change global status to critical
                status.Critical = true;
                //Generate error message
                resultString = $"{friendlyName} = {(Math.Round(result, 4)).ToString(format)} critical.";
                WriteVerbose($"Performance counter \\{performanceCounter.CategoryName}\\{performanceCounter.CounterName}\\{performanceCounter.InstanceName} = {result} >= {critical} -> status critical");
            }
            //Status warning
            else if (result >= warning)
            {
                status.Warning = true;
                resultString = $"{friendlyName} = {(Math.Round(result, 4)).ToString(format)} warning.";
                WriteVerbose($"Performance counter \\{performanceCounter.CategoryName}\\{performanceCounter.CounterName}\\{performanceCounter.InstanceName} = {result} >= {warning} -> status warning");
            }
            else
            {
                WriteVerbose($"Performance counter \\{performanceCounter.CategoryName}\\{performanceCounter.CounterName}\\{performanceCounter.InstanceName} -> status ok");
            }
        }
        //Warning less than critical->Counter has to be greater than warning and critical to be ok.
        else
        {
            WriteVerbose($"Average result for performance counter \\{performanceCounter.CategoryName}\\{performanceCounter.CounterName}\\{performanceCounter.InstanceName} = {result} -> Must be greater than {critical} and {warning} to be ok");
            //Status critical
            if (result <= critical)
            {
                status.Critical = true;
                resultString = $"{friendlyName} = {(Math.Round(result, 4, MidpointRounding.AwayFromZero)).ToString(format)} critical.";
                WriteVerbose($"Performance counter \\{performanceCounter.CategoryName}\\{performanceCounter.CounterName}\\{performanceCounter.InstanceName} = {result} <= {critical} -> status critical");
            }
            //Status warning
            else if (result <= warning)
            {
                status.Warning = true;
                resultString = $"{friendlyName} = {(Math.Round(result, 4, MidpointRounding.AwayFromZero)).ToString(format)} warning.";
                WriteVerbose($"Performance counter \\{performanceCounter.CategoryName}\\{performanceCounter.CounterName}\\{performanceCounter.InstanceName} = {result} <= {warning} -> status warning");
            }
             else
            {
                WriteVerbose($"Performance counter \\{performanceCounter.CategoryName}\\{performanceCounter.CounterName}\\{performanceCounter.InstanceName} -> status ok");
            }
        }
    }
    private void WriteVerbose(string output)
    {
        if (verbose)
        {
            Console.WriteLine($"[{DateTime.Now.ToString("MM/dd/yyyy-HH:mm:ss.fff", System.Globalization.DateTimeFormatInfo.InvariantInfo)}] {output}");
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
                WriteVerbose($"Disposing Performance counter \\{performanceCounter.CategoryName}\\{performanceCounter.CounterName}\\{performanceCounter.InstanceName}");
                performanceCounter.Close();
                performanceCounter.Dispose();
                performanceCounter = null;
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