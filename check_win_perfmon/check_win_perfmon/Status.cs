namespace check_win_perfmon
{
    /// <summary>
    /// Class to store status of final message
    /// </summary>
    public class Status
    {
        private bool _ok = true;
        private bool _warning;
        private bool _critical;

        public bool Ok { get => _ok; set => ChangeState(value,ref _ok,ref _warning,ref _critical); }
        public bool Warning { get => _warning; set => ChangeState(value, ref _warning, ref _ok, ref _critical); }
        public bool Critical { get => _critical; set => ChangeState(value, ref _critical, ref _ok, ref _warning); }

        private void ChangeState(bool state, ref bool field, ref bool field2, ref bool field3)
        {
            field = state;
            if (field)
            {
                field2 = false;
                field3 = false;
            }
        }

        public string GetStatus()
        {
            if (Ok) return "Ok";
            return Warning ? "Warning" : "Critical";
        }

        public int GetExitCode()
        {
            if (Ok) return 0;
            return Warning ? 1 : 2;
        }
    }
}