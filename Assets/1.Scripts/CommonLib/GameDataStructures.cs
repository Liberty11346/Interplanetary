using System;
using System.Data;

namespace CommonLib
{
    // 전송용 struct들 중복되거나, 비효율적인 구조체가 있을 수 있음 
    








    /// <summary>
    /// 명령 불발시 클라이언트에 피드백 가능하게 만들어보려고 함, 아직 사용 안함
    /// </summary>
    [Serializable]
    public struct CommandFailureData
    {
        public enum FailureReason
        {
            Unknown = 0,
            InvalidPath = 1,
            InsufficientResources = 2,
            InvalidTarget = 3,
            NotOwner = 4,
            CooldownActive = 5,
            MaxSupplyReached = 6,
            InvalidCommand = 7
        }

        public int CommandId;
        public int PlayerId;
        public CommandType CommandType;
        public FailureReason Reason;
        public string Message;
    }

    [Serializable]
    public struct RoomInfo
    {
        public string RoomId;
        public int PlayerCount;
        public int MaxPlayers;
        public bool IsGameStarted;
    }

    [Serializable]
    public struct ChatMessage
    {
        public string SenderId;
        public string Message;
        public long Timestamp;
    }

    [Serializable]
    public struct GameStartData
    {
        public int GameId;
        public int MapId;
        public string PlayersJson;
        public PlanetData[] Planets;
    }

    [Serializable]
    public struct ResourceUpdate
    {
        public int PlayerId;
        public float Minerals;
        public float Gas;
        public int CurrentSupply;
        public int MaxSupply;
    }

    [Serializable]
    public struct PlanetResourceUpdate
    {
        public int PlanetId;
        public int OwnerId;
        public float Minerals;
        public float Gas;
        public int CurrentSupply;
        public int MaxSupply;
    }

    /// <summary>
    /// 없애고 플릿 데이터랑 합치고 싶음
    /// </summary>
    [Serializable]
    public struct FleetSpawnData
    {
        public int FleetId;
        public int FleetType;
        public int OwnerId;
        public int PlanetId;
    }


/// <summary>
/// 함대 이동 데이터
/// </summary>
    [Serializable]
    public struct FleetMoveData
    {
        public int FleetId;
        public int FromPlanetId;
        public int ToPlanetId;
        public float Progress;
    }

    [Serializable]
    public struct CombatResult
    {
        public int AttackerId;
        public int DefenderId;
        public int PlanetId;
        public bool AttackerWon;
        public int AttackerLosses;
        public int DefenderLosses;
    }

    [Serializable]
    public struct PlanetConquerData
    {
        public int PlanetId;
        public int NewOwnerId;
        public int PreviousOwnerId;
    }

    [Serializable]
    public struct GameEndData
    {
        public int WinnerId;
        public string Reason;
        public long GameDuration;
    }

    [Serializable]
    public struct PlayerData
    {
        public int PlayerId;
        public string SessionId;
        public string PlayerName;
    }
}
