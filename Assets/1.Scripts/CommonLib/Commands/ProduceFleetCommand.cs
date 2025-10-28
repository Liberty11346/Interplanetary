using System;

namespace CommonLib.Commands
{
    [Serializable]
    public class ProduceFleetCommand : IGameCommand
    {
        public int PlayerId { get; set; }
        public long TickNumber { get; set; }
        public GameCommandType Type => GameCommandType.ProduceFleet;
        public int TargetId { get; set; }
    }
}