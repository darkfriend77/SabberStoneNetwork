using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SabberStoneCore.Config;
using SabberStoneCore.Enums;
using SabberStoneCore.Kettle;
using SabberStoneCore.Model;

namespace SabberStoneTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var game =
                new Game(new GameConfig
                {
                    StartPlayer = 1,
                    Player1HeroClass = CardClass.DRUID,
                    Player2HeroClass = CardClass.MAGE,
                    History = true,
                    SkipMulligan = true,
                    Shuffle = true,
                    FillDecks = true
                });
            game.StartGame();

            //var test = JsonConvert.SerializeObject(game.PowerHistory.Last);
            //var list = JsonConvert.DeserializeObject<List<IPowerHistoryEntry>>(test);
        }
    }
}
