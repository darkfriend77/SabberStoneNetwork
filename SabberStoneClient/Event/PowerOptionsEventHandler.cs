using System;
using System.Collections.Generic;
using SabberStoneClient.Client;
using SabberStoneCore.Kettle;

namespace SabberStoneClient.Event
{
    public delegate void PowerOptionsEventHandler(object source, PowerOptionsEventArgs e);

    public class PowerOptionsEventArgs : EventArgs
    {
        private readonly List<PowerOption> _powerOptionList;

        public PowerOptionsEventArgs(List<PowerOption> powerOptionList)
        {
            _powerOptionList = powerOptionList;
        }
        public List<PowerOption> GetPowerOptions()
        {
            return _powerOptionList;
        }
    }
}