/// <summary>
/// Class to store status of final message
/// </summary>
public class Status
{
    private bool critical;
    private bool warning;
    public Status()
    {
        Critical = false;
        Warning = false;
    }
    public bool Critical { get => critical; set { critical = value; } }
    public bool Warning { get => warning; set => warning = value; }
}