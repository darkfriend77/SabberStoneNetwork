using System;
using System.Collections.Generic;
using SabberStoneClient.Client;
using SabberStoneCore.Kettle;

namespace SabberStoneClient.Event
{
    public delegate void PowerHistoryEventHandler(object source, PowerHistoryEventArgs e);

    public class PowerHistoryEventArgs : EventArgs
    {
        private readonly List<IPowerHistoryEntry> _powerHistoryEntries;

        public PowerHistoryEventArgs(List<IPowerHistoryEntry> powerHistoryEntries)
        {
            _powerHistoryEntries = powerHistoryEntries;
        }
        public List<IPowerHistoryEntry> GetPowerHistory()
        {
            return _powerHistoryEntries;
        }
    }
}