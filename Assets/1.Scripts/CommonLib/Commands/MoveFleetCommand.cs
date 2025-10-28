using System;

namespace CommonLib.Commands
{
    [Serializable]
    public class MoveFleetCommand : IGameCommand
    {
        public int PlayerId { get; set; }
        public long TickNumber { get; set; }
        public GameCommandType Type => GameCommandType.MoveFleet;
        public int TargetFleet { get; set; }
        public int TargetPlanetId { get; set; }
    }
}