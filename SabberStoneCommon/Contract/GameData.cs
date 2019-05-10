using System.Collections.Generic;
using SabberStoneCommon.PowerObjects;
using SabberStoneCore.Enums;
using SabberStoneCore.Kettle;

namespace SabberStoneCommon.Contract
{
    public enum GameMessageType
    {
        GameRequest,
        GameResponse
    }

    public class GameData
    {
        public virtual int GameId { get; set; }
        public virtual GameMessageType GameMessageType { get; set; }
        public virtual string GameMessageData { get; set; }
    }

    public enum GameRequestType
    {
        Invitation,
        Preparation,
        GameStart,
        PowerHistory,
        PowerAllOptions
    }
    public enum GameResponseType
    {
        Invitation,
        Preparation,
        PowerOption
    }
    public class GameRequest
    {
        public virtual GameRequestType GameRequestType { get; set; }
        public virtual string GameRequestData { get; set; }
    }
    public class GameResponse
    {
        public virtual RequestState RequestState { get; set; }
        public virtual GameResponseType GameResponseType { get; set; }
        public virtual string GameResponseData { get; set; }
    }
    public class GameRequestInvitation
    {
        public virtual int GameId { get; set; }
        public virtual int PlayerId { get; set; }
    }
    public class GameResponseInvitation { }
    public class GameRequestPreparation { }
    public class GameRequestGameStart
    {
        public virtual UserInfo Player1 { get; set; }
        public virtual UserInfo Player2 { get; set; }
    }
    public class GameResponsePreparation
    {
        public virtual DeckType DeckType { get; set; }
        public virtual string DeckData { get; set; }
    }
    public class GameRequestPowerHistory
    {
        public virtual int PlayerId { get; set; }
        public virtual PowerType PowerType { get; set; }
        public virtual string PowerHistory { get; set; }
    }

    public class GameRequestPowerAllOptions
    {
        public virtual int PlayerId { get; set; }
        public virtual int PowerOptionIndex { get; set; }
        public virtual List<PowerOption> PowerOptionList { get; set; }
    }
    public class GameResponsePowerOption
    {
        public virtual PowerOption PowerOption { get; set; }
    }
    public enum DeckType
    {
        None,
        Random,
        DeckString,
        CardIds
    }
}