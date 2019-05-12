using System;
using System.Threading;
using SabberStoneClient;
using SabberStoneClient.Client;
using SabberStoneCommon.Contract;
using SabberStoneServer;

namespace SabberStoneGodSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread server = new Thread(new ThreadStart(ServerStart));
            server.Start();

            //var gameServer = new GameServer();
            //gameServer.Start();

            Thread client1 = new Thread(new ThreadStart(Client1));
            client1.Start();

            Thread.Sleep(2000);

            //Thread client2 = new Thread(new ThreadStart(Client2));
            //client2.Start();

            //var gameClient1 = new GameClient();

            //gameClient1.Connect();

            //Thread.Sleep(1000);

            //gameClient1.RequestHandShake("Bot1");

            //Thread.Sleep(1000);

            //gameClient1.RequestQueue(GameType.Normal);

            //Thread.Sleep(2000);

            //gameClient1.RequestStats();

            //Thread.Sleep(10000);

            //gameServer.Broadcast();

            //gameClient.Disconnect();

            Console.ReadKey();
        }

        private static void Client1()
        {
            var client = new GameClient(true);

            client.Connect();

            Thread.Sleep(1000);

            client.RequestHandShake("Bot1");

            Thread.Sleep(1000);

            client.RequestQueue(GameType.Normal);

            Thread.Sleep(10000);

            //client.UpdatedServerStats(true);
        }

        private static void Client2()
        {
            var client = new GameClient(true);

            client.Connect();

            Thread.Sleep(1000);

            client.RequestHandShake("Bot2");

            Thread.Sleep(1000);

            client.RequestQueue(GameType.Normal);

            Thread.Sleep(2000);

            //client.RequestStats();
        }

        private static void ServerStart()
        {
            var gameServer = new GameServer();
            gameServer.Start();
        }
    }
}
