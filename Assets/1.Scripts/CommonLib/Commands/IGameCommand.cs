namespace CommonLib.Commands
{
    public enum GameCommandType
    {
        ProduceFleet = 1,
        MoveFleet = 2
    }

    public interface IGameCommand
    {
        int PlayerId { get; set; }
        long TickNumber { get; set; }
        GameCommandType Type { get; }
    }
}