using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using SabberStoneCommon.Contract;

namespace SabberStoneServer.Packet
{
    public class PacketHelper
    {
        public static string CreateDataPacket(int id, string token, SendData sendData)
        {
            var dataPacket =
                new DataPacket
                {
                    Id = id,
                    Token = token,
                    SendData = JsonConvert.SerializeObject(sendData)
                };
            return JsonConvert.SerializeObject(dataPacket);
        }
    }
}
