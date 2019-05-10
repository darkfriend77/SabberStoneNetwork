using System;
using SabberStoneCommon.Contract;

namespace SabberStoneServer.Server
{
    public delegate void UserStatChangeEventHandler(object source, UserStatChangeEventArgs e);

    public class UserStatChangeEventArgs : EventArgs
    {
        private readonly int _userId;

        private readonly UserState _newUserState;

        private readonly UserState _oldUserState;

        public UserStatChangeEventArgs(int userId, UserState newUserState, UserState oldUserState)
        {
            _userId = userId;
            _newUserState = newUserState;
            _oldUserState = oldUserState;
        }
        public int GetUserId()
        {
            return _userId;
        }
        public UserState GetNewUserState()
        {
            return _newUserState;
        }
        public UserState GetOldUserState()
        {
            return _oldUserState;
        }
    }
}