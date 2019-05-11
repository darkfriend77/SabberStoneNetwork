﻿using GodSharp.Sockets;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using log4net;
using log4net.Config;
using Newtonsoft.Json;
using SabberStoneCommon.Contract;
using SabberStoneServer.Server;

namespace SabberStoneServer
{
    public class UserInfoData : UserInfo
    {
        public string Token { get; set; }
        public ITcpConnection Connection { get; set; }
    }

    public class GameServer
    {
        //public event UserStatChangeEventHandler OnUserStatChangeEvent;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly TcpServer _gameServer;

        private readonly int _id;

        private readonly string _token;

        private int _index = 10000;
        public int Index => _index++;

        private readonly ConcurrentDictionary<string, UserInfoData> _registredUsers;

        public ICollection<UserInfoData> RegistredUsers => _registredUsers.Values;

        private readonly MatchMaker _matchMaker;

        public GameServer()
        {
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

            Log.Info("Initializing SabberStone GameServer!");

            _gameServer = new TcpServer(2010, "127.0.0.1");
            _gameServer.OnConnected += OnConnected;
            _gameServer.OnDisconnected += OnDisconnected;
            _gameServer.OnReceived += OnReceived;
            _gameServer.OnException += OnException;
            _gameServer.OnStarted += OnStarted;
            _gameServer.OnStopped += OnStopped;

            _id = 1; // server id
            _token = "server"; // server token

            _registredUsers = new ConcurrentDictionary<string, UserInfoData>();

            _matchMaker = new MatchMaker(this);
        }

        private void OnStopped(NetServerEventArgs c)
        {
            Log.Info($"{c.LocalEndPoint} stopped.");
        }

        private void OnStarted(NetServerEventArgs c)
        {
            Log.Info($"{c.LocalEndPoint} started.");
        }

        private void OnException(NetClientEventArgs<ITcpConnection> c)
        {
            Log.Error($"{c.RemoteEndPoint} exception:{c.Exception.StackTrace.ToString()}.");
        }

        private void OnReceived(NetClientReceivedEventArgs<ITcpConnection> c)
        {
            //Log("INFO", string.Join(" ", c.Buffers.Select(x => x.ToString("X2")).ToArray()));

            var sendData = DataPacketBuilder.Deserialize(c.Buffers);
            Log.Info($"Received from {sendData.Id}: {sendData.MessageType}");
            switch (sendData.MessageType)
            {
                case MessageType.HandShake:
                    c.NetConnection.Send(ResponseHandShake(c.NetConnection, sendData.Id, sendData.Token, sendData));
                    break;

                case MessageType.Stats:
                    c.NetConnection.Send(ResponseStats(c.NetConnection, sendData.Id, sendData.Token, sendData));
                    break;

                case MessageType.Queue:
                    c.NetConnection.Send(ResponseQueue(c.NetConnection, sendData.Id, sendData.Token, sendData));
                    break;

                case MessageType.GameResponse:
                    _matchMaker.ProcessData(c.NetConnection, sendData.Id, sendData.Token, sendData.GameId, JsonConvert.DeserializeObject<GameResponse>(sendData.MessageData));
                    break;

                case MessageType.GameRequest:
                case MessageType.Response:
                case MessageType.None:
                default:
                    Log.Warn($"[Id:{sendData.Id}][Token:{sendData.Token}][{sendData.MessageType}] Not implemented! (Sent:{c.RemoteEndPoint})");
                    break;
            }
            //c.NetConnection.Send(c.Buffers);
        }

        public void ChangeUserState(UserInfoData userInfoData, UserState userState)
        {
            var oldUserState = userInfoData.UserState;
            userInfoData.UserState = userState;
            //OnUserStatChangeEvent?.Invoke(this, new UserStatChangeEventArgs(userInfoData.Id, userInfoData.UserState, oldUserState));
        }

        private void Broadcast(string message)
        {
            foreach (var user in RegistredUsers)
            {
                user.Connection.Send(message, Encoding.UTF8);
            }
        }

        private byte[] ResponseQueue(ITcpConnection connection, int dataPacketId, string dataPacketToken, SabberDataPacket sendData)
        {
            var requestState = RequestState.Success;

            if (_registredUsers.TryGetValue(dataPacketToken, out UserInfoData userInfoData) && userInfoData.UserState == UserState.None)
            {
                ChangeUserState(userInfoData, UserState.Queued);
            }
            else
            {
                requestState = RequestState.Fail;
            }

            return DataPacketBuilder.ResponseServerQueue(_id, _token, requestState, 0);
        }

        private byte[] ResponseStats(ITcpConnection connection, int dataPacketId, string dataPacketToken, SabberDataPacket sendData)
        {
            var list = new List<UserInfo>();
            _registredUsers.Values.ToList().ForEach(p =>
                list.Add(new UserInfo()
                {
                    Id = p.Id,
                    AccountName = p.AccountName,
                    UserState = p.UserState,
                    GameId = p.GameId,
                    DeckType = p.DeckType,
                    DeckData = p.DeckData,
                    PlayerState = p.PlayerState
                }));

            return DataPacketBuilder.ResponseServerStats(_id, _token, RequestState.Success, list);
        }

        private byte[] ResponseHandShake(ITcpConnection connection, int dataPacketId, string dataPacketToken, SabberDataPacket sendData)
        {
            var handShake = JsonConvert.DeserializeObject<HandShakeRequest>(sendData.MessageData);
            var requestState = RequestState.Success;

            // create token here
            int userIndex = Index;
            var salted = (handShake.AccountName + userIndex).GetHashCode();
            var userToken = salted.ToString();
            //Console.WriteLine($"Hash: {salted} -> {token}");

            var newUserInfo = new UserInfoData
            {
                Id = userIndex,
                Token = userToken,
                AccountName = handShake.AccountName,
                Connection = connection,
                UserState = UserState.None,
                GameId = -1,
                DeckType = DeckType.None,
                DeckData = string.Empty,
                PlayerState = PlayerState.None
            };

            var user = _registredUsers.Values.ToList().Find(p => p.AccountName == handShake.AccountName);

            if (user != null && user.Connection.Equals(connection))
            {
                Log.Warn($"Account {handShake.AccountName} already registred! EndPoint: {user.Connection.LocalEndPoint}, Key: {user.Connection.Key}, {user.Connection.Equals(connection)}");
                requestState = RequestState.Fail;
            }
            else if (user != null && !_registredUsers.TryUpdate(user.Token, newUserInfo, user))
            {
                Log.Warn($"Account {handShake.AccountName} couldn't be updated!");
                requestState = RequestState.Fail;
            }
            else if (!_registredUsers.TryAdd(userToken, newUserInfo))
            {
                Log.Warn($"Account {handShake.AccountName} couldn't be registred!");
                requestState = RequestState.Fail;
            }

            if (requestState == RequestState.Success)
            {
                Log.Info($"Registred account {handShake.AccountName}!");
            }

            return DataPacketBuilder.ResponseServerHandShake(_id, _token, requestState, userIndex, userToken);
        }

        private void OnDisconnected(NetClientEventArgs<ITcpConnection> c)
        {
            var user = _registredUsers.Values.ToList().Find(p => p.Connection == c.NetConnection);
            if (user != null && !_registredUsers.TryRemove(user.Token, out _))
            {
                Log.Warn($"Unable to unregister {user.AccountName} after disconnect.");
            }
            else
            {
                Log.Info($"{(user != null ? user.AccountName : c.RemoteEndPoint.ToString())} disconnected.");
            }
        }

        private void OnConnected(NetClientEventArgs<ITcpConnection> c)
        {
            Log.Info($"{c.RemoteEndPoint} connected.");
        }

        public void Start()
        {
            Log.Info("SabberStone GameServer started!");
            _gameServer.Start();
            _matchMaker.Start();
        }

        public void Stop()
        {
            Log.Info("SabberStone GameServer stopped!");
            _matchMaker.Stop();
            _gameServer.Stop();
        }

    }
}
