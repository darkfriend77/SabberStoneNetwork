using System.Collections.Generic;
using Newtonsoft.Json;
using SabberStoneCommon.PowerObjects;
using SabberStoneCore.Kettle;

namespace SabberStoneCommon.Contract
{
    public class DataPacketBuilder
    {
        #region CLIENT_REQUESTS
        public static string RequestClientHandShake(int id, string token, string accountName)
        {
            return JsonConvert.SerializeObject(new DataPacket
            {
                Id = id,
                Token = token,
                SendData = JsonConvert.SerializeObject(
                    new SendData
                    {
                        MessageType = MessageType.HandShake,
                        MessageData = JsonConvert.SerializeObject(
                            new HandShakeRequest
                            {
                                AccountName = accountName
                            })
                    })
            });
        }
        public static string RequestClientStats(int id, string token)
        {
            return JsonConvert.SerializeObject(new DataPacket()
            {
                Id = id,
                Token = token,
                SendData = JsonConvert.SerializeObject(
                    new SendData
                    {
                        MessageType = MessageType.Stats,
                        MessageData = JsonConvert.SerializeObject(
                            new StatsRequest())
                    })
            });
        }
        public static string RequestClientQueue(int id, string token, GameType gameType)
        {
            return JsonConvert.SerializeObject(new DataPacket()
            {
                Id = id,
                Token = token,
                SendData = JsonConvert.SerializeObject(
                    new SendData
                    {
                        MessageType = MessageType.Queue,
                        MessageData = JsonConvert.SerializeObject(
                            new QueueRequest
                            {
                                GameType = gameType
                            })
                    })
            });
        }
        #endregion

        #region SERVER_RESPONSES
        public static string ResponseServerHandShake(int id, string token, RequestState requestState, int userIndex, string userToken)
        {
            return JsonConvert.SerializeObject(new DataPacket()
            {
                Id = id,
                Token = token,
                SendData = JsonConvert.SerializeObject(
                    new SendData
                    {
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
                                        }) :
                                    ""
                            })
                    })
            });
        }
        public static string ResponseServerQueue(int id, string token, RequestState requestState, int queueSize)
        {
            return JsonConvert.SerializeObject(new DataPacket()
            {
                Id = id,
                Token = token,
                SendData = JsonConvert.SerializeObject(
                    new SendData
                    {
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
                    })
            });
        }
        public static string ResponseServerStats(int id, string token, RequestState requestState, List<UserInfo> userInfos)
        {
            return JsonConvert.SerializeObject(new DataPacket()
            {
                Id = id,
                Token = token,
                SendData = JsonConvert.SerializeObject(
                    new SendData
                    {
                        MessageType = MessageType.Response,
                        MessageData = JsonConvert.SerializeObject(
                            new Response()
                            {
                                RequestState = requestState,
                                ResponseType = ResponseType.Stats,
                                ResponseData = JsonConvert.SerializeObject(new StatsResponse { UserInfos = userInfos })
                            })
                    })
            });
        }
        #endregion

        #region SERVER_REQUESTS
        public static string RequestServerGameInvitation(int id, string token, int gameId, int playerId)
        {
            return JsonConvert.SerializeObject(new DataPacket()
            {
                Id = id,
                Token = token,
                SendData = JsonConvert.SerializeObject(
                    new SendData
                    {
                        MessageType = MessageType.Game,
                        MessageData = JsonConvert.SerializeObject(
                            new GameData
                            {
                                GameId = gameId,
                                GameMessageType = GameMessageType.GameRequest,
                                GameMessageData = JsonConvert.SerializeObject(
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
                            })
                    })
            });
        }
        public static string RequestServerGamePreparation(int id, string token, int gameId)
        {
            return JsonConvert.SerializeObject(new DataPacket()
            {
                Id = id,
                Token = token,
                SendData = JsonConvert.SerializeObject(
                    new SendData
                    {
                        MessageType = MessageType.Game,
                        MessageData = JsonConvert.SerializeObject(
                            new GameData
                            {
                                GameId = gameId,
                                GameMessageType = GameMessageType.GameRequest,
                                GameMessageData = JsonConvert.SerializeObject(
                                    new GameRequest
                                    {
                                        GameRequestType = GameRequestType.Preparation,
                                        GameRequestData = JsonConvert.SerializeObject(
                                            new GameRequestPreparation {})
                                    })
                            })
                    })
            });
        }
        public static string RequestServerGameStart(int id, string token, int gameId, UserInfo player1, UserInfo player2)
        {
            return JsonConvert.SerializeObject(new DataPacket
            {
                Id = id,
                Token = token,
                SendData = JsonConvert.SerializeObject(
                    new SendData
                    {
                        MessageType = MessageType.Game,
                        MessageData = JsonConvert.SerializeObject(
                            new GameData
                            {
                                GameId = gameId,
                                GameMessageType = GameMessageType.GameRequest,
                                GameMessageData = JsonConvert.SerializeObject(
                                    new GameRequest
                                    {
                                        GameRequestType = GameRequestType.GameStart,
                                        GameRequestData = JsonConvert.SerializeObject(
                                            new GameRequestGameStart
                                            {
                                                Player1 = new UserInfo { Id = player1.Id, AccountName = player1.AccountName},
                                                Player2 = new UserInfo { Id = player2.Id, AccountName = player2.AccountName}
                                            })
                                    })
                            })
                    })
            });
        }
        public static string RequestServerGamePowerHistory(int id, string token, int gameId, int playerId, IPowerHistoryEntry powerHistoryLast)
        {
            return JsonConvert.SerializeObject(new DataPacket
            {
                Id = id,
                Token = token,
                SendData = JsonConvert.SerializeObject(
                    new SendData
                    {
                        MessageType = MessageType.Game,
                        MessageData = JsonConvert.SerializeObject(
                            new GameData
                            {
                                GameId = gameId,
                                GameMessageType = GameMessageType.GameRequest,
                                GameMessageData = JsonConvert.SerializeObject(
                                    new GameRequest
                                    {
                                        GameRequestType = GameRequestType.PowerHistory,
                                        GameRequestData = JsonConvert.SerializeObject(
                                            new GameRequestPowerHistory
                                            {
                                                PlayerId = playerId,
                                                PowerType = powerHistoryLast.PowerType,
                                                PowerHistory = PowerJsonHelper.Serialize(powerHistoryLast)
                                            })
                                    })
                            })
                    })
            });
        }
        public static string RequestServerGamePowerOptions(int id, string token, int gameId, int playerId, int index, List<PowerOption> powerOptionList)
        {
            return JsonConvert.SerializeObject(new DataPacket()
            {
                Id = id,
                Token = token,
                SendData = JsonConvert.SerializeObject(
                    new SendData
                    {
                        MessageType = MessageType.Game,
                        MessageData = JsonConvert.SerializeObject(
                            new GameData
                            {
                                GameId = gameId,
                                GameMessageType = GameMessageType.GameRequest,
                                GameMessageData = JsonConvert.SerializeObject(
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
                            })
                    })
            });
        }
        #endregion

        #region CLIENT_RESPONSES
        public static string ResponseClientGameInvitation(int id, string token, int gameId, RequestState requestState)
        {
            return JsonConvert.SerializeObject(new DataPacket()
            {
                Id = id,
                Token = token,
                SendData = JsonConvert.SerializeObject(
                    new SendData
                    {
                        MessageType = MessageType.Game,
                        MessageData = JsonConvert.SerializeObject(
                            new GameData
                            {
                                GameId = gameId,
                                GameMessageType = GameMessageType.GameResponse,
                                GameMessageData = JsonConvert.SerializeObject(
                                    new GameResponse
                                    {
                                        RequestState = requestState,
                                        GameResponseType = GameResponseType.Invitation,
                                        GameResponseData = JsonConvert.SerializeObject(new GameResponseInvitation())
                                    })
                            })
                    })
            });
        }
        public static string ResponseClientGamePreparation(int id, string token, int gameId, DeckType deckType, string deckData, RequestState requestState)
        {
            return JsonConvert.SerializeObject(new DataPacket()
            {
                Id = id,
                Token = token,
                SendData = JsonConvert.SerializeObject(
                    new SendData
                    {
                        MessageType = MessageType.Game,
                        MessageData = JsonConvert.SerializeObject(
                            new GameData
                            {
                                GameId = gameId,
                                GameMessageType = GameMessageType.GameResponse,
                                GameMessageData = JsonConvert.SerializeObject(
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
                            })
                    })
            });
        }
        public static string ResponseClientGamePowerOption(int id, string token, int gameId, PowerOption powerOption)
        {
            return JsonConvert.SerializeObject(new DataPacket()
            {
                Id = id,
                Token = token,
                SendData = JsonConvert.SerializeObject(
                    new SendData
                    {
                        MessageType = MessageType.Game,
                        MessageData = JsonConvert.SerializeObject(
                            new GameData
                            {
                                GameId = gameId,
                                GameMessageType = GameMessageType.GameResponse,
                                GameMessageData = JsonConvert.SerializeObject(
                                    new GameResponse
                                    {
                                        RequestState = RequestState.Success,
                                        GameResponseType = GameResponseType.Preparation,
                                        GameResponseData = JsonConvert.SerializeObject(
                                            new GameResponsePowerOption
                                            {
                                                PowerOption = powerOption
                                            })
                                    })
                            })
                    })
            });
        }
        #endregion

    }

    public class DataPacket
    {
        public virtual int Id { get; set; }

        public virtual string Token { get; set; }

        public virtual string SendData { get; set; }
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