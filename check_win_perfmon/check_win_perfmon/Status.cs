namespace check_win_perfmon
{
    /// <summary>
    /// Class to store status of all counters
    /// </summary>
    public class Status
    {
        public StatusEnum StatusMessage { get; private set; } = StatusEnum.Ok;

        public void SetWarning()
        {
            if (StatusMessage == StatusEnum.Ok)
            {
                StatusMessage = StatusEnum.Warning;
            }
        }

        public void SetCritical()
        {
            StatusMessage = StatusEnum.Critical;
        }

        public int GetExitCode()
        {
            return (int)StatusMessage;
        }
        public string GetStatus()
        {
            return StatusMessage.ToString();
        }
    }
}