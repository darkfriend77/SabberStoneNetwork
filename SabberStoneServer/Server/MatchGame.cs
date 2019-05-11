using System;
using System.Collections.Generic;
using System.Reflection;
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


    public enum MatchState
    {

    }

    internal class MatchGame
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly int _id;

        private readonly string _token;

        private readonly GameServer _gameServer;

        private Random _random;

        private readonly int _gameId;

        private readonly UserInfoData _player1;

        private PowerAllOptions _powerAllOptionsPlayer1;

        private readonly UserInfoData _player2;

        private PowerAllOptions _powerAllOptionsPlayer2;

        private Game _game;

        private UserInfoData UserById(int id) => _player1.Id == id ? _player1 : _player2.Id == id ? _player2 : null;

        public MatchGame(GameServer gameServer, int index, UserInfoData player1, UserInfoData player2)
        {
            _gameServer = gameServer;
            _random = new Random();
            _gameId = index;
            _player1 = player1;
            _player1.PlayerState = PlayerState.None;
            _player2 = player2;
            _player2.PlayerState = PlayerState.None;

            _id = 2;
            _token = $"matchgame{_gameId}";

            _game = null;
        }

        public void Initialize()
        {
            // game invitation request for player 1
            _player1.PlayerState = PlayerState.Invitation;
            _player1.Connection.Send(DataPacketBuilder.RequestServerGameInvitation(_id, _token, _gameId, 1));

            // game invitation request for player 2
            _player2.PlayerState = PlayerState.Invitation;
            _player2.Connection.Send(DataPacketBuilder.RequestServerGameInvitation(_id, _token, _gameId, 2));
        }

        public void Start()
        {
            Log.Info($"[_gameId:{_gameId}] Game creation is happening in a few seconds!!!");
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

            if (_game == null)
            {
                _game = newGame;
                Log.Info($"[_gameId:{_gameId}] Game creation done!");
                _game.StartGame();

                ProcessPowerHistoryData(1, _player1, _game.PowerHistory.Last);

                _powerAllOptionsPlayer1 = PowerOptionsBuilder.AllOptions(_game, _game.Player1.Options());

                ProcessPowerOptionsData(1, _player1, _powerAllOptionsPlayer1);

                ProcessPowerHistoryData(2, _player2, _game.PowerHistory.Last);

                _powerAllOptionsPlayer2 = PowerOptionsBuilder.AllOptions(_game, _game.Player2.Options());

                ProcessPowerOptionsData(2, _player2, _powerAllOptionsPlayer2);

                foreach (var historyEntry in _game.PowerHistory.Last)
                {
                    //Log.Info($"{historyEntry.Print()}");
                }
            }
        }

        private void ProcessPowerOptionsData(int playerId, UserInfoData userInfoData, PowerAllOptions allOptions)
        {

            userInfoData.Connection.Send(DataPacketBuilder.RequestServerGamePowerOptions(_id, _token, _gameId, playerId, allOptions.Index, allOptions.PowerOptionList));
        }

        private void ProcessPowerHistoryData(int playerId, UserInfoData userInfoData, List<IPowerHistoryEntry> powerHistoryLast)
        {
            foreach (var historyEntry in powerHistoryLast)
            {
                userInfoData.Connection.Send(DataPacketBuilder.RequestServerGamePowerHistory(_id, _token, _gameId, playerId, historyEntry));
                //Log.Warn($"[_gameId:{_gameId}] should be stopped here, isn't implemented!!!");
                Thread.Sleep(50);
            }
            
            //userInfoData.Connection.Send(DataPacketBuilder.RequestServerGamePowerHistoryX(_id, "matchgame", _gameId, playerId, powerHistoryLast), Encoding.UTF8);

        }

        public void Stop()
        {
            // stop game for both players now!
            Log.Warn($"[_gameId:{_gameId}] should be stopped here, isn't implemented!!!");
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
                    userInfoData.Connection.Send(DataPacketBuilder.RequestServerGamePreparation(_id, _token, _gameId));
                    break;

                case GameResponseType.Preparation:

                    var gameResponsePreparation = JsonConvert.DeserializeObject<GameResponsePreparation>(gameResponse.GameResponseData);
                    userInfoData.DeckType = gameResponsePreparation.DeckType;
                    userInfoData.DeckData = gameResponsePreparation.DeckData;
                    _gameServer.ChangeUserState(userInfoData, UserState.InGame);
                    if (_player1.UserState == UserState.InGame && _player2.UserState == UserState.InGame)
                    {
                        _player1.Connection.Send(DataPacketBuilder.RequestServerGameStart(_id, _token, _gameId, _player1, _player2));
                        _player2.Connection.Send(DataPacketBuilder.RequestServerGameStart(_id, _token, _gameId, _player1, _player2));
                        Thread.Sleep(500);
                        Start();
                    }
                    break;

                case GameResponseType.PowerOption:
                    //ProcessPowerOptionsData();
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        public PlayerTask ProcessPowerOptionsData(int sendOptionId, int sendOptionMainOption, int sendOptionTarget, int sendOptionPosition, int sendOptionSubOption)
        {
            var allOptions = _game.AllOptionsMap[sendOptionId];

            var tasks = allOptions.PlayerTaskList;

            var powerOption = allOptions.PowerOptionList[sendOptionMainOption];
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
                                if (target != null)
                                    task = HeroAttackTask.Any(_game.CurrentPlayer, target);
                                else
                                    task = PlayCardTask.Any(_game.CurrentPlayer, source);
                                break;
                            case CardType.HERO_POWER:
                                task = HeroPowerTask.Any(_game.CurrentPlayer, target);
                                break;
                            default:
                                task = PlayCardTask.Any(_game.CurrentPlayer, source, target, sendOptionPosition,
                                    sendOptionSubOption);
                                break;
                        }
                    }
                    break;

                case OptionType.PASS:
                    break;

                default:
                    throw new NotImplementedException();
            }

            return task;
        }
    }
}