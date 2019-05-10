namespace SabberStoneCommon.Contract
{
    public enum ResponseType
    {
        None,
        HandShake,
        Stats,
        Queue
    }

    public enum RequestState
    {
        None,
        Fail,
        Success
    }

    public class Response
    {
        public virtual RequestState RequestState { get; set; }

        public virtual ResponseType ResponseType { get; set; }

        public virtual string ResponseData { get; set; }
    }
}