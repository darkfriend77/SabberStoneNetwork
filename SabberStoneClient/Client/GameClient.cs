using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using GodSharp.Sockets;
using Newtonsoft.Json;
using ProtoBuf;
using SabberStoneClient.Event;
using SabberStoneCommon;
using SabberStoneCommon.Contract;
using SabberStoneCommon.PowerObjects;
using SabberStoneCore.Enums;
using SabberStoneCore.Kettle;
using DeckType = SabberStoneCommon.Contract.DeckType;
using GameType = SabberStoneCommon.Contract.GameType;
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

        public event PowerHistoryEventHandler OnPowerHistoryEvent;

        public event PowerOptionsEventHandler OnPowerOptionsEvent;

        public event LogEventHandler OnLogEvent;

        private readonly bool _isBot;

        private readonly Random _random;

        public List<UserInfo> UserInfos { get; }

        public ConcurrentQueue<IPowerHistoryEntry> HistoryEntries { get; }

        public List<PowerOption> PowerOptionList { get; private set; }

        public int PlayerId { get; set; }

        public UserInfo Player1 { get; private set; }

        public UserInfo Player2 { get; private set; }

        public Dictionary<PlayState, int> Statistics { get; private set; }

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
            _random = new Random();

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

            Statistics = new Dictionary<PlayState, int>();
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
            Log("INFO", $"BufferSize received: {c.Buffers.Length}");
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

                case MessageType.GameRequest:
                    var gameRequest = JsonConvert.DeserializeObject<GameRequest>(sDataPacket.MessageData);
                    ProcessGameRequest(gameRequest);
                    break;

                case MessageType.GameResponse:
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
            //Log("INFO", $"{gameRequest.GameRequestType}");
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
                    var powerHistoryEntries = JsonConvert.DeserializeObject<List<IPowerHistoryEntry>>(gameRequestPowerHistory.PowerHistory, new PowerHistoryConverter());
                    powerHistoryEntries.ForEach(p => HistoryEntries.Enqueue(p));
                    OnPowerHistoryEvent?.Invoke(this, new PowerHistoryEventArgs(powerHistoryEntries));

                    break;

                case GameRequestType.PowerAllOptions:
                    var gameRequestPowerAllOptions = JsonConvert.DeserializeObject<GameRequestPowerAllOptions>(gameRequest.GameRequestData);
                    if (gameRequestPowerAllOptions.PowerOptionList != null &&
                        gameRequestPowerAllOptions.PowerOptionList.Count > 0)
                    {
                        PowerOptionList = gameRequestPowerAllOptions.PowerOptionList;
                        OnPowerOptionsEvent?.Invoke(this, new PowerOptionsEventArgs(gameRequestPowerAllOptions.PowerOptionList));
                        //PowerOptionList.ForEach(p => Log("INFO", p.Print()));

                        if (_isBot)
                        {
                            var powerOptionId = _random.Next(PowerOptionList.Count);
                            var powerOption = PowerOptionList.ElementAt(powerOptionId);
                            var target = powerOption.MainOption?.Targets != null && powerOption.MainOption.Targets.Count > 0
                                ? powerOption.MainOption.Targets.ElementAt(_random.Next(powerOption.MainOption.Targets.Count))
                                : 0;
                            var subOption = powerOption.SubOptions != null && powerOption.SubOptions.Count > 0
                                ? _random.Next(powerOption.SubOptions.Count)
                                : 0;
                            ResponsePowerOption(powerOption, target, 0, subOption);
                            Log("INFO", $"target:{target}, position:0, suboption: {subOption} {powerOption.Print()}");
                            PowerOptionList.Clear();
                        }
                    }

                    break;

                case GameRequestType.GameStop:
                    var gameRequestGameStop = JsonConvert.DeserializeObject<GameRequestGameStop>(gameRequest.GameRequestData);
                    var playState = PlayerId == 1 ? gameRequestGameStop.Play1State : gameRequestGameStop.Play2State;
                    Statistics[playState] = Statistics.ContainsKey(playState) ? Statistics[playState] + 1 : 1;

                    if (ClientState == ClientState.InGame)
                    {
                        ClientState = ClientState.Registred;
                    }


                    if (_isBot)
                    {
                        ResponseGameStop(PlayerId, RequestState.Success);
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

        public void ResponsePowerOption(PowerOption powerOption, int target, int position, int subOption)
        {
            _gameClient.Connection.Send(DataPacketBuilder.ResponseClientGamePowerOption(_id, _token, _gameId, powerOption, target, position, subOption));
        }
        public void ResponseGameStop(int playerId, RequestState requestState)
        {
            _gameClient.Connection.Send(DataPacketBuilder.ResponseClientGameStop(_id, _token, _gameId, playerId, requestState));

            // reseting ids ...
            _gameId = -1;
            PlayerId = -1;
        }
        #endregion

        private void Log(string logLevel, string s)
        {
            Console.WriteLine($"CLIENT[{logLevel}]: {s}");
            OnLogEvent?.Invoke(this, new LogEventArgs($"CLIENT[{logLevel}]: {s}"));
        }
    }
}
