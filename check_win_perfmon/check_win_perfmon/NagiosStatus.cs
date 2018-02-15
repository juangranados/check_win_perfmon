namespace check_win_perfmon
{
    /// <summary>
    /// Class to store status of all counters
    /// </summary>
    public class NagiosStatus
    {
        public NagiosStatusEnum NagiosStatusMessage { get; private set; } = NagiosStatusEnum.Ok;

        public void SetWarning()
        {
            if (NagiosStatusMessage == NagiosStatusEnum.Ok)
            {
                NagiosStatusMessage = NagiosStatusEnum.Warning;
            }
        }

        public void SetCritical()
        {
            NagiosStatusMessage = NagiosStatusEnum.Critical;
        }

        public void Initialize()
        {
            NagiosStatusMessage = NagiosStatusEnum.Ok;
        }

        public int GetNagiosExitCode()
        {
            return (int)NagiosStatusMessage;
        }
        public string GetNagiosStatus()
        {
            return NagiosStatusMessage.ToString();
        }

    }
}