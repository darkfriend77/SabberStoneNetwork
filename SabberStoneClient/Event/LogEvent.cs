using System;

namespace SabberStoneClient.Event
{

    public delegate void LogEventHandler(object source, LogEventArgs e);
    public class LogEventArgs : EventArgs
    {
        private readonly string _logString;

        public LogEventArgs(string logString)
        {
            _logString = logString;

        }
        public string GetLog()
        {
            return _logString;
        }
    }
}