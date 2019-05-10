using System;
using System.Collections.Generic;
using SabberStoneCommon;
using SabberStoneCommon.Contract;

namespace SabberStoneClient.Event
{
    public delegate void ServerStatsEventHandler(object source, ServerStatsEventArgs e);

    public class ServerStatsEventArgs : EventArgs
    {
        private readonly List<UserInfo> _userInfos;

        public ServerStatsEventArgs(List<UserInfo> userInfos)
        {
            _userInfos = userInfos;
        }
        public List<UserInfo> GetUserInfos()
        {
            return _userInfos;
        }
    }
}