namespace SabberStoneCommon.Contract
{
    public enum MessageType
    {
        None,
        HandShake,
        Stats,
        Queue,
        Response,
        Game
    }

    public class SendData
    {
        public virtual MessageType MessageType { get; set; }

        public virtual string MessageData { get; set; }
    }
}