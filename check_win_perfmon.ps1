<#PSScriptInfo

.VERSION 1.0

.AUTHOR Juan Granados

.COPYRIGHT 2022 Juan Granados

.LICENSEURI https://raw.githubusercontent.com/juangranados/check_win_perfmon/master/LICENSE

.PROJECTURI https://github.com/juangranados/check_win_perfmon

.RELEASENOTES Initial release

#>

<#
.SYNOPSIS
    Download check_win_perform.exe and runs it.
.DESCRIPTION
    Download check_win_perform and runs it.
 .PARAMETER file
    XML file with performance counters.
 .PARAMETER time
     Time between samples in ms.
     Default 1000.
 .PARAMETER samples
    Amount of samples to take from perfmon.
    Default 3.
 .PARAMETER noalerts
    Allways returns ok status.
    Default false.
.EXAMPLE
    check_win_perform.ps1 -f "C:\ProgramData\icinga2\var\lib\icinga2\api\zones\global-templates\_etc\scripts\PerfMonNetwork.xml"
.LINK
    https://github.com/juangranados/check_win_perfmon
.NOTES
    Author: Juan Granados 
#>

Param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$file,
    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [int]$time = 1000,
    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [int]$samples = 3,
    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [switch]$noalerts
)

## ------------------------------------------------------------------
# function Invoke-Process
# https://stackoverflow.com/a/66700583
## ------------------------------------------------------------------
function Invoke-Process {
    param
    (
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$FilePath,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]$ArgumentList,

        [ValidateSet("Full", "StdOut", "StdErr", "ExitCode", "None")]
        [string]$DisplayLevel
    )

    $ErrorActionPreference = 'Stop'

    try {
        $pinfo = New-Object System.Diagnostics.ProcessStartInfo
        $pinfo.FileName = $FilePath
        $pinfo.RedirectStandardError = $true
        $pinfo.RedirectStandardOutput = $true
        $pinfo.UseShellExecute = $false
        $pinfo.WindowStyle = 'Hidden'
        $pinfo.CreateNoWindow = $true
        $pinfo.Arguments = $ArgumentList
        $p = New-Object System.Diagnostics.Process
        $p.StartInfo = $pinfo
        $p.Start() | Out-Null
        $result = [pscustomobject]@{
            Title     = ($MyInvocation.MyCommand).Name
            Command   = $FilePath
            Arguments = $ArgumentList
            StdOut    = $p.StandardOutput.ReadToEnd()
            StdErr    = $p.StandardError.ReadToEnd()
            ExitCode  = $p.ExitCode
        }
        $p.WaitForExit()

        if (-not([string]::IsNullOrEmpty($DisplayLevel))) {
            switch ($DisplayLevel) {
                "Full" { return $result; break }
                "StdOut" { return $result.StdOut; break }
                "StdErr" { return $result.StdErr; break }
                "ExitCode" { return $result.ExitCode; break }
            }
        }
        else {
            return $result
        }
    }
    catch {
        Write-Host "An error has ocurred"
    }
}

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$exePath = "$env:ProgramFiles\ICINGA2\sbin\check_win_perfmon.exe"
$url = 'https://github.com/juangranados/check_win_perfmon/releases/download/1.5/check_win_perfmon.exe'
$hash = '83DDC35C71336472D33D612BAEF81E69FBA775DB35D5FA36777933D81D896247'

$ErrorActionPreference = "SilentlyContinue"
$path = $exePath.Substring(0, $file.LastIndexOf('\'))
if (-not (Test-Path $path)) {
    mkdir $path | Out-Null
}
if (-not (Test-Path $exePath)) {
    Invoke-WebRequest $url -OutFile $exePath
}
else {
    if ((Get-FileHash $exePath).Hash -ne $hash) {
        Invoke-WebRequest $url -OutFile $exePath
    }
}
if (-not (Test-Path $exePath)) {
    Write-Host "Error downloading check_win_perfmon.exe"
    Exit 3
}
if (!$noalerts) {
    $checkResult = Invoke-Process -FilePath $exePath -ArgumentList "-f $file -t $time -s $samples"
}
else {
    $checkResult = Invoke-Process -FilePath $exePath -ArgumentList "-f $file -t $time -s $samples -n"
}

Write-Host $checkResult.StdOut
Exit $checkResult.ExitCode