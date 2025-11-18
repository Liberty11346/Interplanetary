using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLib
{
    /// <summary>
    /// 행성의 종류를 나타내는 열거형
    /// </summary>
    public enum PlanetType
    {
        Terrestrial, // 지구형 행성
        GasGiant,    // 가스 거인
        IceGiant,    // 얼음 거인
        DwarfPlanet  // 왜소 행성
    }

    /// <summary>
    /// 함대 종류를 나타내는 열거형
    /// </summary>
    public enum FleetType
    {
        Scout,      // 정찰함
        Fighter,    // 전투함
        Cruiser,    // 순양함
        Battleship  // 전함
    }

    [Flags]
    public enum RoomState
    {
        None = 0,
        Open = 1 << 0,      // 1
        Full = 1 << 1,      // 2
        Ingame = 1 << 2,    // 4
        Disabled = 1 << 3,  // 8
        Closed = 1 << 4,    // 16
        Error = 1 << 5,     // 32

        // 유용한 조합들을 미리 정의
        Joinable = Open,
        Active = Open | Full | Ingame,
        Inactive = Disabled | Closed | Error,
        All = Open | Full | Ingame | Disabled | Closed | Error
    }

    /// <summary>
    /// 서버 응답 코드
    /// </summary>
    public enum ServerMessage
    {
        Success = 1,
        Error = 2
    }

}
