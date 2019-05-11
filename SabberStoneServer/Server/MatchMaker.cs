using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using GodSharp.Sockets;
using log4net;
using SabberStoneCommon.Contract;

namespace SabberStoneServer.Server
{
    internal class MatchMaker
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly GameServer _gameServer;

        private readonly Timer _timer;

        private int _maxGamesPerCall = 5;

        private int _index = 10000;
        public int Index => _index++;

        private readonly ConcurrentDictionary<int, MatchGame> _matchGames;

        public MatchMaker(GameServer gameServer)
        {
            _gameServer = gameServer;
            _timer = new Timer((e) => { MatchMakerService(); }, null, Timeout.Infinite, Timeout.Infinite);
            _matchGames = new ConcurrentDictionary<int, MatchGame>();
        }

        private void MatchMakerService()
        {
            var finishedMatches = _matchGames.Values.Where(matchGamesValue => matchGamesValue.IsFinished).ToList();
            finishedMatches.ForEach(p =>
            {
                if (_matchGames.TryRemove(p.GameId, out var finishedMatch))
                {
                    Log.Info($"[GameId:{finishedMatch.GameId}] finished, [{finishedMatch.Player1.AccountName}:{finishedMatch.Play1State}] vs [{finishedMatch.Player2.AccountName}:{finishedMatch.Play2State}].");
                }
            });

            var queuedUsers = _gameServer.RegistredUsers.ToList().Where(user => user.UserState == UserState.Queued).ToList();
            Log.Info($"{queuedUsers.Count} users queued for matchmaking.");

            for (int i = 0; i < _maxGamesPerCall && queuedUsers.Count > 1; i++)
            {
                var player1 = queuedUsers.ElementAt(0);
                var player2 = queuedUsers.ElementAt(1);
                queuedUsers.RemoveRange(0, 2);

                _gameServer.ChangeUserState(player1, UserState.Invited);
                _gameServer.ChangeUserState(player2, UserState.Invited);

                var gameId = Index;
                var matchgame = new MatchGame(_gameServer, gameId, player1, player2);
                if (_matchGames.TryAdd(gameId, matchgame))
                {
                    matchgame.Initialize();
                }
                else
                {
                    Log.Error($"Couldn't add [GameId:{gameId}] match game with {player1.AccountName} and {player2.AccountName}.");
                }
            }
        }

        public void Start()
        {
            var startTimeSpan = TimeSpan.Zero;
            var periodTimeSpan = TimeSpan.FromSeconds(7);

            _timer.Change(startTimeSpan, periodTimeSpan);
        }

        public void Stop()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public void ProcessData(ITcpConnection cNetConnection, int dataPacketId, string dataPacketToken, int gameId, GameResponse gameResponse)
        {
            if (!_matchGames.TryGetValue(gameId, out var matchGame))
            {
                Log.Warn($"Couldn't find match game with [GameId:{gameId}]. Not processing game data.");
                return;
            }

            // processing game data
            Log.Info($"Processing game data for [GameId:{gameId}].");
            matchGame.ProcessGameResponse(dataPacketId, dataPacketToken, gameResponse);
        }
    }
}