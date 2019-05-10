using System;
using SabberStoneClient.Client;

namespace SabberStoneClient.Event
{
    public delegate void ClientStateEventHandler(object source, ClientStateEventArgs e);

    public class ClientStateEventArgs : EventArgs
    {
        private readonly ClientState _newClientState;

        private readonly ClientState _oldClientState;

        public ClientStateEventArgs(ClientState newClientState, ClientState oldClientState)
        {
            _newClientState = newClientState;
            _oldClientState = oldClientState;
        }
        public ClientState GetNewClientState()
        {
            return _newClientState;
        }

        public ClientState GetOldClientState()
        {
            return _oldClientState;
        }
    }
}