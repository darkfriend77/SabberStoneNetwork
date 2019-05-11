using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using ProtoBuf;
using SabberStoneCommon.PowerObjects;
using SabberStoneCore.Enums;
using SabberStoneCore.Kettle;
using SabberStoneCore.Model.Zones;

namespace SabberStoneCommon.Contract
{
    public class DataPacketBuilder
    {
        public static byte[] Serialize(SabberDataPacket data)
        {
            var mem = new MemoryStream();
            Serializer.Serialize(mem, data);
            return mem.ToArray();
        }

        public static SabberDataPacket Deserialize(byte[] buffer)
        {
            return Serializer.Deserialize<SabberDataPacket>(new MemoryStream(buffer));
        }

        private static byte[] SerializePowerHistory(List<IPowerHistoryEntry> data)
        {
            var mem = new MemoryStream();
            Serializer.Serialize(mem, data);
            return mem.ToArray();
        }

        #region CLIENT_REQUESTS
        public static byte[] RequestClientHandShake(int id, string token, string accountName)
        {
            return Serialize(new SabberDataPacket()
            {
                Id = id,
                Token = token,
                GameId = -1,
                MessageType = MessageType.HandShake,
                MessageData = JsonConvert.SerializeObject(
                    new HandShakeRequest
                    {
                        AccountName = accountName
                    })
            });
        }
        public static byte[] RequestClientStats(int id, string token)
        {
            return Serialize(new SabberDataPacket()
            {
                Id = id,
                Token = token,
                GameId = -1,
                MessageType = MessageType.Stats,
                MessageData = JsonConvert.SerializeObject(
                            new StatsRequest())
            });
        }
        public static byte[] RequestClientQueue(int id, string token, GameType gameType)
        {
            return Serialize(new SabberDataPacket()
            {
                Id = id,
                Token = token,
                GameId = -1,
                MessageType = MessageType.Queue,
                MessageData = JsonConvert.SerializeObject(
                    new QueueRequest
                    {
                        GameType = gameType
                    })
            });
        }
        #endregion

        #region SERVER_RESPONSES
        public static byte[] ResponseServerHandShake(int id, string token, RequestState requestState, int userIndex, string userToken)
        {
            return Serialize(new SabberDataPacket()
            {
                Id = id,
                Token = token,
                GameId = -1,
                MessageType = MessageType.Response,
                MessageData = JsonConvert.SerializeObject(
                    new Response
                    {
                        RequestState = requestState,
                        ResponseType = ResponseType.HandShake,
                        ResponseData = requestState == RequestState.Success ?
                            JsonConvert.SerializeObject(
                                new HandShakeResponse
                                {
                                    Id = userIndex,
                                    Token = userToken
                                }) : ""
                    })
            });
        }
        public static byte[] ResponseServerQueue(int id, string token, RequestState requestState, int queueSize)
        {
            return Serialize(new SabberDataPacket()
            {
                Id = id,
                Token = token,
                GameId = -1,
                MessageType = MessageType.Response,
                MessageData = JsonConvert.SerializeObject(
                    new Response
                    {
                        RequestState = requestState,
                        ResponseType = ResponseType.Queue,
                        ResponseData = JsonConvert.SerializeObject(
                            new QueueResponse
                            {
                                QueueSize = queueSize
                            })
                    })
            });
        }
        public static byte[] ResponseServerStats(int id, string token, RequestState requestState, List<UserInfo> userInfos)
        {
            return Serialize(new SabberDataPacket()
            {
                Id = id,
                Token = token,
                GameId = -1,
                MessageType = MessageType.Response,
                MessageData = JsonConvert.SerializeObject(
                    new Response()
                    {
                        RequestState = requestState,
                        ResponseType = ResponseType.Stats,
                        ResponseData = JsonConvert.SerializeObject(new StatsResponse { UserInfos = userInfos })
                    })
            });
        }
        #endregion

        #region SERVER_REQUESTS
        public static byte[] RequestServerGameInvitation(int id, string token, int gameId, int playerId)
        {
            return Serialize(new SabberDataPacket()
            {
                Id = id,
                Token = token,
                GameId = gameId,
                MessageType = MessageType.GameRequest,
                MessageData = JsonConvert.SerializeObject(
                    new GameRequest
                    {
                        GameRequestType = GameRequestType.Invitation,
                        GameRequestData = JsonConvert.SerializeObject(
                            new GameRequestInvitation
                            {
                                GameId = gameId,
                                PlayerId = playerId
                            })
                    })

            });
        }
        public static byte[] RequestServerGamePreparation(int id, string token, int gameId)
        {
            return Serialize(new SabberDataPacket()
            {
                Id = id,
                Token = token,
                GameId = gameId,
                MessageType = MessageType.GameRequest,
                MessageData = JsonConvert.SerializeObject(
                    new GameRequest
                    {
                        GameRequestType = GameRequestType.Preparation,
                        GameRequestData = JsonConvert.SerializeObject(
                            new GameRequestPreparation { })
                    })
            });
        }
        public static byte[] RequestServerGameStart(int id, string token, int gameId, UserInfo player1, UserInfo player2)
        {
            return Serialize(new SabberDataPacket()
            {
                Id = id,
                Token = token,
                GameId = gameId,
                MessageType = MessageType.GameRequest,
                MessageData = JsonConvert.SerializeObject(
                    new GameRequest
                    {
                        GameRequestType = GameRequestType.GameStart,
                        GameRequestData = JsonConvert.SerializeObject(
                            new GameRequestGameStart
                            {
                                Player1 = new UserInfo { Id = player1.Id, AccountName = player1.AccountName },
                                Player2 = new UserInfo { Id = player2.Id, AccountName = player2.AccountName }
                            })
                    })
            });
        }
        public static byte[] RequestServerGameStop(int id, string token, int gameId, PlayState play1State, PlayState play2State)
        {
            return Serialize(new SabberDataPacket()
            {
                Id = id,
                Token = token,
                GameId = gameId,
                MessageType = MessageType.GameRequest,
                MessageData = JsonConvert.SerializeObject(
                    new GameRequest
                    {
                        GameRequestType = GameRequestType.GameStop,
                        GameRequestData = JsonConvert.SerializeObject(
                            new GameRequestGameStop
                            {
                                Play1State = play1State,
                                Play2State = play2State
                            })
                    })
            });
        }
        public static byte[] RequestServerGamePowerHistory(int id, string token, int gameId, int playerId, List<IPowerHistoryEntry> powerHistoryLast)
        {
            return Serialize(new SabberDataPacket()
            {
                Id = id,
                Token = token,
                GameId = gameId,
                MessageType = MessageType.GameRequest,
                MessageData = JsonConvert.SerializeObject(
                    new GameRequest
                    {
                        GameRequestType = GameRequestType.PowerHistory,
                        GameRequestData = JsonConvert.SerializeObject(
                            new GameRequestPowerHistory
                            {
                                PlayerId = playerId,
                                PowerHistory = JsonConvert.SerializeObject(powerHistoryLast)
                            })
                    })
            });
        }
        public static byte[] RequestServerGamePowerOptions(int id, string token, int gameId, int playerId, int index, List<PowerOption> powerOptionList)
        {
            return Serialize(new SabberDataPacket()
            {
                Id = id,
                Token = token,
                GameId = gameId,
                MessageType = MessageType.GameRequest,
                MessageData = JsonConvert.SerializeObject(
                    new GameRequest
                    {
                        GameRequestType = GameRequestType.PowerAllOptions,
                        GameRequestData = JsonConvert.SerializeObject(
                            new GameRequestPowerAllOptions
                            {
                                PlayerId = playerId,
                                PowerOptionIndex = index,
                                PowerOptionList = powerOptionList
                            })
                    })
            });
        }
        #endregion

        #region CLIENT_RESPONSES
        public static byte[] ResponseClientGameInvitation(int id, string token, int gameId, RequestState requestState)
        {
            return Serialize(new SabberDataPacket()
            {
                Id = id,
                Token = token,
                GameId = gameId,
                MessageType = MessageType.GameResponse,
                MessageData = JsonConvert.SerializeObject(
                    new GameResponse
                    {
                        RequestState = requestState,
                        GameResponseType = GameResponseType.Invitation,
                        GameResponseData = JsonConvert.SerializeObject(
                            new GameResponseInvitation())
                    })
            });
        }
        public static byte[] ResponseClientGamePreparation(int id, string token, int gameId, DeckType deckType, string deckData, RequestState requestState)
        {
            return Serialize(new SabberDataPacket()
            {
                Id = id,
                Token = token,
                GameId = gameId,
                MessageType = MessageType.GameResponse,
                MessageData = JsonConvert.SerializeObject(
                    new GameResponse
                    {
                        RequestState = requestState,
                        GameResponseType = GameResponseType.Preparation,
                        GameResponseData = JsonConvert.SerializeObject(
                            new GameResponsePreparation
                            {
                                DeckType = deckType,
                                DeckData = deckData
                            })
                    })
            });
        }
        public static byte[] ResponseClientGamePowerOption(int id, string token, int gameId, PowerOption powerOption, int target, int position, int subOption)
        {
            return Serialize(new SabberDataPacket()
            {
                Id = id,
                Token = token,
                GameId = gameId,
                MessageType = MessageType.GameResponse,
                MessageData = JsonConvert.SerializeObject(
                    new GameResponse
                    {
                        RequestState = RequestState.Success,
                        GameResponseType = GameResponseType.PowerOption,
                        GameResponseData = JsonConvert.SerializeObject(
                            new GameResponsePowerOption
                            {
                                PowerOption = powerOption,
                                Target = target,
                                Position = position,
                                SubOption = subOption

                            })
                    })
            });
        }
        public static byte[] ResponseClientGameStop(int id, string token, int gameId, int playerId, RequestState requestState)
        {
            return Serialize(new SabberDataPacket()
            {
                Id = id,
                Token = token,
                GameId = gameId,
                MessageType = MessageType.GameResponse,
                MessageData = JsonConvert.SerializeObject(
                    new GameResponse
                    {
                        RequestState = requestState,
                        GameResponseType = GameResponseType.GameStop,
                        GameResponseData = JsonConvert.SerializeObject(
                            new GameResponseGameStop
                            {
                                PlayerId = playerId
                            })
                    })
            });
        }
        #endregion

    }

    [ProtoContract]
    public class SabberDataPacket
    {
        [ProtoMember(1)]
        public virtual int Id { get; set; }
        [ProtoMember(2)]
        public virtual int GameId { get; set; }
        [ProtoMember(3)]
        public virtual int PlayerId { get; set; }
        [ProtoMember(4)]
        public virtual string Token { get; set; }
        [ProtoMember(5)]
        public virtual MessageType MessageType { get; set; }
        [ProtoMember(6)]
        public virtual string MessageData { get; set; }
    }

    public enum MessageType
    {
        None,
        HandShake,
        Stats,
        Queue,
        Response,
        GameRequest,
        GameResponse
    }

    public class HandShakeRequest
    {
        public virtual string AccountName { get; set; }
    }

    public class HandShakeResponse
    {
        public virtual int Id { get; set; }
        public virtual string Token { get; set; }
    }

    public class StatsRequest { }
    public class StatsResponse
    {
        public virtual List<UserInfo> UserInfos { get; set; }
    }
    public class UserInfo
    {
        public virtual int Id { get; set; }
        public virtual string AccountName { get; set; }
        public virtual UserState UserState { get; set; }
        public virtual int GameId { get; set; }
        public virtual DeckType DeckType { get; set; }
        public virtual string DeckData { get; set; }
        public virtual PlayerState PlayerState { get; set; }
    }
    public enum UserState
    {
        None,
        Queued,
        Invited,
        Prepared,
        InGame
    }
    public enum PlayerState
    {
        None,
        Invitation,
        Config,
        Game,
        Quit
    }
    public class QueueRequest
    {
        public virtual GameType GameType { get; set; }
    }

    public class QueueResponse
    {
        public virtual int QueueSize { get; set; }
    }
    public enum GameType
    {
        Normal
    }
}