# Check Win Perfmon
Plugin for Icinga/Nagios that allow to check a group of Windows performance counters.

It checks value of performance counter based on threshold specified.

Returns exit and performance data in Icinga/Nagios format.

![Performance output](https://github.com/juangranados/check_win_perfmon/blob/master/PerformanceOutput.PNG)

[Download](https://github.com/juangranados/check_win_perfmon/files/1368625/check_win_perfmon.zip) Check Win Perfmon. 

Please read below prior use it! 

Examples
--------
***Example CPU counters:*** check_win_perfmon.exe -f PerfMonCPU.xml
```
OK - All performance counters between range | 'ProcessorTime'=3%;95;100;0;100 'UserTime'=2%;85;95;0;100 'DPCTime'=0%;15;20;0;100 'InterruptTime'=0%;10;15;0;100 'ProcessorQueueLength'=0;4;8;;
```
***Example Memory counters:*** check_win_perfmon.exe -f PerfMonMem.xml
```
OK - All performance counters between range | 'CommittedBytesInUse'=57%;80;90;0;100 'AvailableMBytes'=4083MB;1024;512;0;8192 'AvailableMBytesPercent'=50%;13;6;0;100 'FreeSystemPageTableEntries'=2867405056;5000;4000;; 'PagesSec'=0;5000;6000;;
```
***Example Physical Disk counters:*** check_win_perfmon.exe -f PerfMonPhysicalDisk.xml
```
OK - All performance counters between range | 'AvgDiskSecTransfer'=0.0002s;0.04;0.05;; 'CurrentDiskQueueLength'=0;32;40;; 'AvgDiskSecWrite'=0.0002s;0.04;0.05;0; 'AvgDiskSecRead'=0s;0.04;0.05;0; 'IdleTime'=100%;20;15;0;100
```
***Example Network counters:*** check_win_perfmon.exe -f PerfMonNetwork.xml
```
OK - All performance counters between range | 'BytesTotalSec'=1885.7051B;15728640;17825790;0;20971520 'BytesTotalSecPercent'=0%;75;85;0;100 'OutputQueueLength'=0;2;3;;
```
In downloaded zip package, there are several .xml files preconfigured:

* ***PerfMonNetwork.xml***: Performance Counters to check network load.

* ***PerfMonPhysicalDisk.xml***:Performance Counters to check physical Disk load.

* ***PerfMonCPU.xml***: Performance Counters to check CPU load.

* ***PerfMonMem.xml***: Performance Counters to check Memory (RAM and virtual) load.

***These values and counters are based on System Center Operations Manager checkins. You can check it out [here](http://mpwiki.viacode.com/default.aspx?g=posts&t=219816).***

You can set up your own performance counters adding them to xml files or creating new ones.

To list available performance counters on a system in a PowerShell console type:

```PowerShell
Get-Counter -ListSet * | Select-Object -ExpandProperty Counter
```
You can check performance counters on a Windows system: Start Menu->Administrative Tools->Performance Monitor->Clic on plus symbol

Usage
-----

check_win_perfmon.exe [parameters]:
* -f, --xmlFile        (Default: perfcounts.xml) XML file with performance counters configuration.

* -s, --maxSamples     (Default: 3) Amount of samples to take from perfmon.

* -t, --timeSamples    (Default: 1000) Time between samples in ms.

* -v, --verbose        Verbose output for debuging.

**Example:** check_win_perfmon.exe -f PerfMonMem.xml -s 10 -t 2000

Check performance counters of PerfMonMem.xml taking 10 samples with 2 sec interval.

XML Format
----------
XML file used must have the following format, for example:

```xml
<?xml version="1.0" encoding="UTF-8" ?>
<perfcounters>
	<perfcounter>
		<category>Processor</category>
		<name>% Processor Time</name>
		<instance>_Total</instance>
		<friendlyname>ProcessorTime</friendlyname>
		<units>%</units>
		<warning>95</warning>
		<critical>100</critical>
		<min>0</min>
		<max>100</max>
	</perfcounter>
	<perfcounter>
		<category>Memory</category>
		<name>Available MBytes</name>
		<instance>none</instance>
		<friendlyname>AvailableMBytes</friendlyname>
		<units>MB</units>
		<warning>1024</warning>
		<critical>512</critical>
		<min>0</min>
		<max>auto</max>
	</perfcounter>
</perfcounters> 
```

In the example above, program will check two counters. For each counter, we need to set:

* **category:** Category of performance counter
* **name:** Name of the performance counter.
* **instance:** Instance of performance counter. Some performance counter does not have instance, in this case the value must be: none. This value can be auto for Network category, program will autodetect best interface to check.
* **friendlyname:** name of performance counter which program returns in his output.
* **units:** units program return in output.
* **warning:** Warning threshold for performance counter.
* **critical:** Critical threshold for performance counter.
* **min:** minimun value of performance counter. If you do not know the minimun value, it has to be: none.
* **max:** maximum value of performance counter.  If you do not know the minimun value, it has to be: none. This value can be auto for Memory category, program will detect the amount of memory installed on system.

If max and min are specified, program returns one more result for percent value.
Max and min must have different value.

Icinga Agent Configuration
--------------------------
**Command**

```
object CheckCommand "check_win_perfmon" {
	import "plugin-check-command"
	command = [ "C:\\Program Files\\ICINGA2\\sbin\\check_win_perfmon.exe" ]
	arguments = {
		"-f" = {
			value = "$xml$"
			order = 1
			description = "XML file"
		}
		"-t" = {
			value = "$interval$"
			order = 2
			description = "Time between samples"
		}
		"-s" = {
			value = "$samples$"
			order = 3
			description = "Samples to take"
		}
	}
}
```

**Service**

```
apply Service "CPU Load" {
  import "generic-service"
  check_command = "check_win_perfmon"
  vars.xml = "C:\\Program Files\\ICINGA2\\sbin\\PerfMonCPU.xml"
  command_endpoint = host.name
  assign where host.vars.os == "Windows"
}

apply Service "Network Load" {
  import "generic-service"
  check_command = "check_win_perfmon"
  vars.xml = "C:\\Program Files\\ICINGA2\\sbin\\PerfMonNetwork.xml"
  command_endpoint = host.name
  assign where host.vars.os == "Windows"
}

apply Service "Disk_0 Load" {
  import "generic-service"
  check_command = "check_win_perfmon"
  vars.xml = "C:\\Program Files\\ICINGA2\\sbin\\PerfMonPhysicalDisk.xml"
  command_endpoint = host.name
  assign where host.vars.os == "Windows"
}

apply Service "Memory Load" {
  import "generic-service"
  check_command = "check_win_perfmon"
  vars.xml = "C:\\Program Files\\ICINGA2\\sbin\\PerfMonMem.xml"
  command_endpoint = host.name
  assign where host.vars.os == "Windows"
}
```
