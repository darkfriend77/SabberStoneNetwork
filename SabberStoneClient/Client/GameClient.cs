using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using GodSharp.Sockets;
using Newtonsoft.Json;
using ProtoBuf;
using SabberStoneClient.Event;
using SabberStoneCommon;
using SabberStoneCommon.Contract;
using SabberStoneCommon.PowerObjects;
using SabberStoneCore.Kettle;
using TcpClient = GodSharp.Sockets.TcpClient;

namespace SabberStoneClient.Client
{
    public enum ClientState
    {
        Disconnected,
        Connected,
        HandShake,
        Registred,
        Queued,
        Invited,
        Prepared,
        InGame
    }

    public class GameClient
    {
        public event ServerStatsEventHandler OnServerStatsEvent;

        public event ClientStateEventHandler OnClientStateEvent;

        public event PowerOptionsEventHandler OnPowerOptionsEvent;

        public event LogEventHandler OnLogEvent;

        private readonly bool _isBot;

        public List<UserInfo> UserInfos { get; }

        public ConcurrentQueue<IPowerHistoryEntry> HistoryEntries { get; }

        public List<PowerOption> PowerOptionList { get; private set; }

        public int PlayerId { get; set; }

        public UserInfo Player1 { get; private set; }

        public UserInfo Player2 { get; private set; }

        private readonly TcpClient _gameClient;

        private ClientState _clientState;
        public ClientState ClientState
        {
            get => _clientState;
            set
            {
                var oldClientState = _clientState;
                _clientState = value;
                Log("INFO", $"changed ClientState[{oldClientState}->{value}]");
                OnClientStateEvent?.Invoke(this, new ClientStateEventArgs(_clientState, oldClientState));
            }
        }

        private int _id;

        private string _token;

        private int _gameId;

        private readonly Timer _timer;

        public GameClient(bool isBot = false)
        {
            Log("INFO", "SabberStone TCP Client!");
            _isBot = isBot;
            _gameClient = new TcpClient("127.0.0.1", 2010);
            _gameClient.OnConnected += OnConnected;
            _gameClient.OnDisconnected += OnDisconnected;
            _gameClient.OnReceived += OnReceived;
            _gameClient.OnException += OnException;
            _gameClient.OnStarted += OnStarted;
            _gameClient.OnStopped += OnStopped;

            UserInfos = new List<UserInfo>();

            _id = -1;
            _token = "";
            _gameId = -1;
            PlayerId = -1;

            _timer = new Timer((e) => { RequestStats(); }, null, Timeout.Infinite, Timeout.Infinite);

            HistoryEntries = new ConcurrentQueue<IPowerHistoryEntry>();
            PowerOptionList = new List<PowerOption>();

            var tt = new SabberDataPacket { Id = 1 };
            using (var mem = new MemoryStream())
            {
                Serializer.Serialize(mem, tt);
                _gameClient.Connection.Send(mem.GetBuffer());
                var nn = Serializer.Deserialize<SabberDataPacket>(mem);
            }
        }

        public byte[] ProtoSerialize(SabberDataPacket data)
        {
            var mem = new MemoryStream();
            Serializer.Serialize(mem, data);
            return mem.GetBuffer();
        }

        public SabberDataPacket ProtoDeserialize(byte[] buffer)
        {
            return Serializer.Deserialize<SabberDataPacket>(new MemoryStream(buffer));
        }

        private void OnStopped(NetClientEventArgs<ITcpConnection> c)
        {
            Log("INFO", $"{c.RemoteEndPoint} stopped.");
        }

        private void OnStarted(NetClientEventArgs<ITcpConnection> c)
        {
            Log("INFO", $"{c.RemoteEndPoint} started.");
        }

        private void OnException(NetClientEventArgs<ITcpConnection> c)
        {
            Log("ERROR", $"{c.RemoteEndPoint} exception:{c.Exception.StackTrace.ToString()}.");
        }

        private void OnReceived(NetClientReceivedEventArgs<ITcpConnection> c)
        {
            //Log("INFO", $"Received from {c.RemoteEndPoint}:");
            //Log("INFO", string.Join(" ", c.Buffers.Select(x => x.ToString("X2")).ToArray()));
            var sDataPacket = DataPacketBuilder.Deserialize(c.Buffers);

            switch (sDataPacket.MessageType)
            {
                case MessageType.Response:
                    var response = JsonConvert.DeserializeObject<Response>(sDataPacket.MessageData);
                    Log("INFO", $"Message[{sDataPacket.MessageType}]: {response.ResponseType} => {response.RequestState}");
                    ProcessResponse(response);
                    break;

                case MessageType.Game:
                    var gameData = JsonConvert.DeserializeObject<GameData>(sDataPacket.MessageData);
                    if (gameData.GameMessageType == GameMessageType.GameRequest)
                    {
                        var gameRequest = JsonConvert.DeserializeObject<GameRequest>(gameData.GameMessageData);
                        //Log("INFO", $"GameMessage[{gameData.GameMessageType}]: {gameRequest.GameRequestType}");
                        ProcessGameRequest(gameRequest);
                    }
                    else
                    {
                        Log("WARN", $"GameMessage[{gameData.GameMessageType}]: Not implemented!");
                    }
                    break;

                default:
                    Log("WARN", $"[Id:{sDataPacket.Id}][Token:{sDataPacket.Token}][{sDataPacket.MessageType}] Not implemented! (Sent:{c.RemoteEndPoint})");
                    break;
            }

            //c.NetConnection.Send(c.Buffers);
        }

        private void OnDisconnected(NetClientEventArgs<ITcpConnection> c)
        {
            ClientState = ClientState.Disconnected;
        }

        private void OnConnected(NetClientEventArgs<ITcpConnection> e)
        {
            ClientState = ClientState.Connected;
        }

        public void Connect()
        {
            _gameClient.Start();
        }

        public void Disconnect()
        {
            _gameClient.Stop();
        }

        public void UpdatedServerStats(bool flag)
        {
            if (!flag)
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                return;
            }
            var startTimeSpan = TimeSpan.Zero;
            var periodTimeSpan = TimeSpan.FromSeconds(10);
            _timer.Change(startTimeSpan, periodTimeSpan);
        }

        #region DATA_REQUESTS

        public void RequestHandShake(string accountName)
        {
            if (ClientState != ClientState.Connected)
            {
                Log("WARN", "Wrong client state to request a handshake!");
                return;
            }

            ClientState = ClientState.HandShake;
            _gameClient.Connection.Send(DataPacketBuilder.RequestClientHandShake(_id, _token, accountName));
        }

        public void RequestStats()
        {
            if (ClientState == ClientState.Disconnected)
            {
                Log("WARN", "Can't request stats without connection!");
                return;
            }

            _gameClient.Connection.Send(DataPacketBuilder.RequestClientStats(_id, _token));
        }

        public void RequestQueue(GameType gameType)
        {
            if (ClientState != ClientState.Registred)
            {
                Log("WARN", "Wrong client state to request a game!");
                return;
            }

            _gameClient.Connection.Send(DataPacketBuilder.RequestClientQueue(_id, _token, gameType));
        }

        #endregion

        #region DATA_RESPONSES

        private void ProcessResponse(Response response)
        {
            if (response.RequestState != RequestState.Success)
            {
                Log("WARN", $"Request[{response.ResponseType}]: failed! ");
                return;
            }

            switch (response.ResponseType)
            {
                case ResponseType.HandShake:
                    ClientState = ClientState.Registred;
                    var handShakeResponse = JsonConvert.DeserializeObject<HandShakeResponse>(response.ResponseData);
                    _id = handShakeResponse.Id;
                    _token = handShakeResponse.Token;
                    break;

                case ResponseType.Stats:
                    var statsResponse = JsonConvert.DeserializeObject<StatsResponse>(response.ResponseData);
                    UserInfos.Clear();
                    statsResponse.UserInfos.ForEach(p =>
                    {
                        Log("INFO", $" -> {p.AccountName}[Id:{p.Id}][GameId:{p.GameId}]: {p.UserState} ({p.DeckType}->'{p.DeckData}')");
                        UserInfos.Add(p);
                    });
                    OnServerStatsEvent?.Invoke(this, new ServerStatsEventArgs(UserInfos));
                    break;

                case ResponseType.Queue:
                    var gameResponse = JsonConvert.DeserializeObject<QueueResponse>(response.ResponseData);
                    ClientState = ClientState.Queued;
                    break;

                case ResponseType.None:
                    break;

                default:
                    break;
            }
        }

        #endregion

        #region GAME_REQUESTS

        private void ProcessGameRequest(GameRequest gameRequest)
        {
            switch (gameRequest.GameRequestType)
            {
                case GameRequestType.Invitation:
                    var gameRequestInvitation = JsonConvert.DeserializeObject<GameRequestInvitation>(gameRequest.GameRequestData);
                    _gameId = gameRequestInvitation.GameId;
                    PlayerId = gameRequestInvitation.PlayerId;
                    if (ClientState == ClientState.Queued)
                    {
                        ClientState = ClientState.Invited;
                    }
                    else
                    {
                        ResponseInvitation(RequestState.Fail);
                    }

                    if (_isBot)
                    {
                        ResponseInvitation(RequestState.Success);
                    }
                    break;

                case GameRequestType.Preparation:

                    if (ClientState == ClientState.Invited)
                    {
                        ClientState = ClientState.Prepared;
                    }
                    else
                    {
                        ResponsePreparation(DeckType.Random, string.Empty, RequestState.Fail);
                    }

                    if (_isBot)
                    {
                        ResponsePreparation(DeckType.Random, string.Empty, RequestState.Success);
                    }
                    break;

                case GameRequestType.GameStart:
                    var gameRequestGameStart = JsonConvert.DeserializeObject<GameRequestGameStart>(gameRequest.GameRequestData);
                    Player1 = gameRequestGameStart.Player1;
                    Player2 = gameRequestGameStart.Player2;
                    ClientState = ClientState.InGame;
                    break;

                case GameRequestType.PowerHistory:
                    var gameRequestPowerHistory = JsonConvert.DeserializeObject<GameRequestPowerHistory>(gameRequest.GameRequestData);
                    var powerHistoryEntry = PowerJsonHelper.Deserialize(gameRequestPowerHistory.PowerType, gameRequestPowerHistory.PowerHistory);
                    HistoryEntries.Enqueue(powerHistoryEntry);
                    break;

                case GameRequestType.PowerAllOptions:
                    var gameRequestPowerAllOptions = JsonConvert.DeserializeObject<GameRequestPowerAllOptions>(gameRequest.GameRequestData);
                    if (gameRequestPowerAllOptions.PowerOptionList != null &&
                        gameRequestPowerAllOptions.PowerOptionList.Count > 0)
                    {
                        OnPowerOptionsEvent?.Invoke(this, new PowerOptionsEventArgs(gameRequestPowerAllOptions.PowerOptionList));
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion

        #region GAME_RESPONSES

        public void ResponseInvitation(RequestState requestState)
        {
            _gameClient.Connection.Send(DataPacketBuilder.ResponseClientGameInvitation(_id, _token, _gameId, requestState));
        }

        public void ResponsePreparation(DeckType deckType, string deckString, RequestState requestState)
        {
            _gameClient.Connection.Send(DataPacketBuilder.ResponseClientGamePreparation(_id, _token, _gameId, deckType, deckString, requestState));
        }

        public void ResponsePowerOption(PowerOption powerOption)
        {
            _gameClient.Connection.Send(DataPacketBuilder.ResponseClientGamePowerOption(_id, _token, _gameId, powerOption));
        }

        #endregion

        private void Log(string logLevel, string s)
        {
            Console.WriteLine($"CLIENT[{logLevel}]: {s}");
            OnLogEvent?.Invoke(this, new LogEventArgs($"CLIENT[{logLevel}]: {s}"));
        }
    }
}
