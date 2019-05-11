using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using GodSharp.Sockets;
using Newtonsoft.Json;
using SabberStoneCommon.Contract;
using System.Threading;
using log4net;
using SabberStoneCore.Config;
using SabberStoneCore.Enums;
using SabberStoneCore.Kettle;
using SabberStoneCore.Model;
using SabberStoneCore.Model.Entities;
using SabberStoneCore.Tasks.PlayerTasks;

namespace SabberStoneServer.Server
{
    internal class MatchGame
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public int GameId { get; }

        private readonly GameServer _gameServer;

        private readonly Random _random;

        public UserInfoData Player1 { get; }

        public PlayState Play1State => _game.Player1.PlayState;

        private PowerAllOptions _powerAllOptionsPlayer1;

        public UserInfoData Player2 { get; }

        public PlayState Play2State => _game.Player2.PlayState;

        private PowerAllOptions _powerAllOptionsPlayer2;

        private Game _game;

        private readonly int _id;

        private readonly string _token;

        private UserInfoData UserById(int id) => Player1.Id == id ? Player1 : Player2.Id == id ? Player2 : null;

        public bool IsFinished => Player1.PlayerState == PlayerState.Quit && Player2.PlayerState == PlayerState.Quit;

        public MatchGame(GameServer gameServer, int index, UserInfoData player1, UserInfoData player2)
        {
            _gameServer = gameServer;
            _random = new Random();
            GameId = index;
            Player1 = player1;
            Player1.PlayerState = PlayerState.None;
            Player2 = player2;
            Player2.PlayerState = PlayerState.None;

            _id = 2;
            _token = $"matchgame{GameId}";

            _game = null;
        }

        public void Initialize()
        {
            // game invitation request for player 1
            Player1.PlayerState = PlayerState.Invitation;
            Player1.Connection.Send(DataPacketBuilder.RequestServerGameInvitation(_id, _token, GameId, 1));

            // game invitation request for player 2
            Player2.PlayerState = PlayerState.Invitation;
            Player2.Connection.Send(DataPacketBuilder.RequestServerGameInvitation(_id, _token, GameId, 2));
        }

        public void Start()
        {
            Log.Info($"[_gameId:{GameId}] Game creation is happening in a few seconds!!!");
            var newGame = new Game(new GameConfig
            {
                //StartPlayer = 1,
                FormatType = FormatType.FT_STANDARD,
                Player1HeroClass = Cards.HeroClasses[_random.Next(9)],
                Player1Deck = new List<Card>(),
                Player2HeroClass = Cards.HeroClasses[_random.Next(9)],
                Player2Deck = new List<Card>(),
                SkipMulligan = true,
                Shuffle = true,
                FillDecks = true,
                Logging = true,
                History = true
            });

            // don't start when game is null
            if (_game != null)
            {
                return;
            }

            _game = newGame;
            Log.Info($"[_gameId:{GameId}] Game creation done!");
            _game.StartGame();

            ProcessPowerHistoryData(1, Player1, _game.PowerHistory.Last);
            ProcessPowerHistoryData(2, Player2, _game.PowerHistory.Last);

            SendPowerOptionsToPlayers();
        }

        private void SendPowerOptionsToPlayers()
        {
            _powerAllOptionsPlayer1 = PowerOptionsBuilder.AllOptions(_game, _game.Player1.Options());
            ProcessPowerOptionsData(1, Player1, _powerAllOptionsPlayer1);

            _powerAllOptionsPlayer2 = PowerOptionsBuilder.AllOptions(_game, _game.Player2.Options());
            ProcessPowerOptionsData(2, Player2, _powerAllOptionsPlayer2);
        }

        private void ProcessPowerOptionsData(int playerId, UserInfoData userInfoData, PowerAllOptions allOptions)
        {
            userInfoData.Connection.Send(DataPacketBuilder.RequestServerGamePowerOptions(_id, _token, GameId, playerId, allOptions.Index, allOptions.PowerOptionList));
        }

        private void ProcessPowerHistoryData(int playerId, UserInfoData userInfoData, List<IPowerHistoryEntry> powerHistoryLast)
        {
            var buffer = DataPacketBuilder.RequestServerGamePowerHistory(_id, "matchgame", GameId, playerId, powerHistoryLast);

            Log.Info($"BufferSize sending: {buffer.Length}");
            userInfoData.Connection.Send(buffer);
        }

        public void Stop()
        {
            // stop game for both players now!
            Log.Warn($"[_gameId:{GameId}] should be stopped here, isn't implemented!!!");

            Player1.Connection.Send(DataPacketBuilder.RequestServerGameStop(_id, _token, GameId, _game.Player1.PlayState, _game.Player2.PlayState));
            Player2.Connection.Send(DataPacketBuilder.RequestServerGameStop(_id, _token, GameId, _game.Player1.PlayState, _game.Player2.PlayState));
        }

        public void ProcessGameResponse(int dataPacketId, string dataPacketToken, GameResponse gameResponse)
        {
            var userInfoData = UserById(dataPacketId);
            Log.Info($"GameResponse:{gameResponse.GameResponseType} from {userInfoData?.AccountName}, {gameResponse.RequestState}");

            if (userInfoData == null || gameResponse.RequestState != RequestState.Success)
            {
                Stop();
                return;
            }

            switch (gameResponse.GameResponseType)
            {
                case GameResponseType.Invitation:
                    _gameServer.ChangeUserState(userInfoData, UserState.Prepared);
                    userInfoData.Connection.Send(DataPacketBuilder.RequestServerGamePreparation(_id, _token, GameId));
                    break;

                case GameResponseType.Preparation:
                    var gameResponsePreparation = JsonConvert.DeserializeObject<GameResponsePreparation>(gameResponse.GameResponseData);
                    userInfoData.DeckType = gameResponsePreparation.DeckType;
                    userInfoData.DeckData = gameResponsePreparation.DeckData;
                    _gameServer.ChangeUserState(userInfoData, UserState.InGame);
                    if (Player1.UserState == UserState.InGame && Player2.UserState == UserState.InGame)
                    {
                        Player1.Connection.Send(DataPacketBuilder.RequestServerGameStart(_id, _token, GameId, Player1, Player2));
                        Player2.Connection.Send(DataPacketBuilder.RequestServerGameStart(_id, _token, GameId, Player1, Player2));
                        Thread.Sleep(500);
                        Start();
                    }
                    break;

                case GameResponseType.PowerOption:
                    var gameResponsePowerOption = JsonConvert.DeserializeObject<GameResponsePowerOption>(gameResponse.GameResponseData);
                    var task = ProcessPowerOptionsData(gameResponsePowerOption.PowerOption, gameResponsePowerOption.Target, 0, gameResponsePowerOption.SubOption);
                    _game.Process(task);
                    if (_game.State == State.RUNNING)
                    {
                        SendPowerOptionsToPlayers();
                    }
                    else
                    {
                        Stop();
                    }
                    break;

                case GameResponseType.GameStop:
                    var gameResponseGameStop = JsonConvert.DeserializeObject<GameResponseGameStop>(gameResponse.GameResponseData);
                    switch (gameResponseGameStop.PlayerId)
                    {
                        case 1:
                            Player1.PlayerState = PlayerState.Quit;
                            break;
                        case 2:
                            Player2.PlayerState = PlayerState.Quit;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        //public PlayerTask ProcessPowerOptionsData(int sendOptionId, int sendOptionMainOption, int sendOptionTarget, int sendOptionPosition, int sendOptionSubOption)
        public PlayerTask ProcessPowerOptionsData(PowerOption powerOption, int sendOptionTarget, int sendOptionPosition, int sendOptionSubOption)
        {

            //var allOptions = _game.AllOptionsMap[sendOptionId];
            //var tasks = allOptions.PlayerTaskList;
            //var powerOption = allOptions.PowerOptionList[sendOptionMainOption];
            var optionType = powerOption.OptionType;

            PlayerTask task = null;
            switch (optionType)
            {
                case OptionType.END_TURN:
                    task = EndTurnTask.Any(_game.CurrentPlayer);
                    break;

                case OptionType.POWER:
                    var mainOption = powerOption.MainOption;
                    var source = _game.IdEntityDic[mainOption.EntityId];
                    var target = sendOptionTarget > 0 ? (ICharacter)_game.IdEntityDic[sendOptionTarget] : null;
                    var subObtions = powerOption.SubOptions;

                    if (source.Zone?.Type == Zone.PLAY)
                    {
                        task = MinionAttackTask.Any(_game.CurrentPlayer, source, target);
                    }
                    else
                    {
                        switch (source.Card.Type)
                        {
                            case CardType.HERO:
                                task = target != null
                                    ? (PlayerTask) HeroAttackTask.Any(_game.CurrentPlayer, target)
                                    : PlayCardTask.Any(_game.CurrentPlayer, source);
                                break;

                            case CardType.HERO_POWER:
                                task = HeroPowerTask.Any(_game.CurrentPlayer, target);
                                break;

                            default:
                                task = PlayCardTask.Any(_game.CurrentPlayer, source, target, sendOptionPosition,sendOptionSubOption);
                                break;
                        }
                    }
                    break;

                case OptionType.PASS:
                    break;

                default:
                    throw new NotImplementedException();
            }

            Log.Info($"{task?.FullPrint()}");

            return task;
        }
    }
}