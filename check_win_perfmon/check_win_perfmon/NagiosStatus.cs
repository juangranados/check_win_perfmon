namespace check_win_perfmon
{
    /// <summary>
    /// Class to store Nagios / Icinga status code
    /// </summary>
    public class NagiosStatus
    {
        private NagiosStatusEnum NagiosStatusMessage { get; set; }
        /// <summary>
        /// Set status to OK
        /// </summary>
        public NagiosStatus()
        {
            NagiosStatusMessage = NagiosStatusEnum.Ok;
        }

        public NagiosStatus(NagiosStatusEnum nagiosStatusEnum)
        {
            NagiosStatusMessage = nagiosStatusEnum;
        }
        /// <summary>
        /// Set status warning only if previous status is OK
        /// </summary>
        public void SetWarning()
        {
            if (NagiosStatusMessage == NagiosStatusEnum.Ok)
            {
                NagiosStatusMessage = NagiosStatusEnum.Warning;
            }
        }
        /// <summary>
        /// Set status critical
        /// </summary>
        public void SetCritical()
        {
            NagiosStatusMessage = NagiosStatusEnum.Critical;
        }
        /// <summary>
        /// Set status OK
        /// </summary>
        public void Initialize()
        {
            NagiosStatusMessage = NagiosStatusEnum.Ok;
        }
        /// <summary>
        /// Get Nagios status numeric code
        /// </summary>
        /// <returns>0: OK, 1: Warning, 2: Critical</returns>
        public int GetNagiosExitCode()
        {
            return (int)NagiosStatusMessage;
        }
        /// <summary>
        /// Get Nagios Status string code
        /// </summary>
        /// <returns>OK, Warning or critical</returns>
        public string GetNagiosStatus()
        {
            return NagiosStatusMessage.ToString();
        }

    }
}