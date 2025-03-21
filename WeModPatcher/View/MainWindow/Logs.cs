namespace WeModPatcher.View.MainWindow
{
    public enum ELogType
    {
        Info,
        Warn,
        Error,
        Success
    }
    public class LogEntry
    {
        public ELogType LogType { get; set; }
        public string Message { get; set; }
    }
}