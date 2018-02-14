namespace check_win_perfmon
{
    /// <summary>
    /// Class to store status of final message
    /// </summary>
    public class Status
    {
        public Status()
        {
            Critical = false;
            Warning = false;
        }
        public bool Critical { get; set; }

        public bool Warning { get; set; }
    }
}