# check_win_perfmon
Plugin for Icinga/Nagios that allow to check a group of Windows performance counters.
It checks value of performance counter based on threshold specified and returns exit and performance data in Icinga/Nagios format.

***Example:*** check_win_perfmon.exe -f PerfMonCPU.xml

>OK - All performance counters between range | 'ProcessorTime'=3%;95;100;0;100 'UserTime'=2%;85;95;0;100 'DPCTime'=0%;15;20;0;100 'InterruptTime'=0%;10;15;0;100 'ProcessorQueueLength'=0;4;8;;

***Example:*** check_win_perfmon.exe -f PerfMonMem.xml

>OK - All performance counters between range | 'CommittedBytesInUse'=57%;80;90;0;100 'AvailableMBytes'=4083MB;1024;512;0;8192 'AvailableMBytesPercent'=50%;13;6;0;100 'FreeSystemPageTableEntries'=2867405056;5000;4000;; 'PagesSec'=0;5000;6000;;

***Example:*** check_win_perfmon.exe -f PerfMonPhysicalDisk.xml

>OK - All performance counters between range | 'AvgDiskSecTransfer'=0.0002s;0.04;0.05;; 'CurrentDiskQueueLength'=0;32;40;; 'AvgDiskSecWrite'=0.0002s;0.04;0.05;0; 'AvgDiskSecRead'=0s;0.04;0.05;0; 'IdleTime'=100%;20;15;0;100

***Example:*** check_win_perfmon.exe -f PerfMonNetwork.xml

>OK - All performance counters between range | 'BytesTotalSec'=1885.7051B;15728640;17825790;0;20971520 'BytesTotalSecPercent'=0%;75;85;0;100 'OutputQueueLength'=0;2;3;;

In downloaded zip package, there are several .xml files preconfigured:

* ***PerfMonNetwork.xml***: Performance Counters to check network load.

* ***PerfMonPhysicalDisk.xml***:Performance Counters to check physical Disk load.

* ***PerfMonCPU.xml***: Performance Counters to check CPU load.

* ***PerfMonMem.xml***: Performance Counters to check Memory (RAM and virtual) load.

You can set up your own performance counters adding them to xml files or creating new ones.

To list available performance counters on a system in a PowerShell console type:

> Get-Counter -ListSet * | Select-Object -ExpandProperty Counter

Usage
-----

check_win_perfmon.exe [parameters]:
* -f, --xmlFile        (Default: perfcounts.xml) XML file with performance counters configuration.

* -s, --maxSamples     (Default: 3) Amount of samples to take from perfmon.

* -t, --timeSamples    (Default: 1000) Time between samples in ms.

* -v, --verbose        (Default: False) Verbose output for debuging.

**Example:** check_win_perfmon.exe -f PerfMonMem.xml -s 10 -t 2000

Check performance counters of PerfMonMem.xml taking 10 samples with 2 sec interval.
