namespace CommonLib
{
    public static class ProtocolType
    {
        // 클라이언트 → 서버
        public const int JOIN_ROOM = 1001;
        public const int LEAVE_ROOM = 1002;
        public const int CHAT_MESSAGE = 1003;
        public const int HEARTBEAT = 1004;
        public const int CREATE_ROOM = 3100;
        public const int JOIN_ROOM_REQUEST = 3101;
        public const int GET_ROOM_LIST = 3103;
        public const int READY = 3104;
        public const int GET_MAP_LIST = 3105;
        public const int GET_TABLE_DATA = 3106;
        public const int SUBMIT_COMMAND = 3010;

        // 서버 → 클라이언트
        public const int JOIN_SUCCESS = 2001;
        public const int JOIN_FAILED = 2002;
        public const int LEAVE_SUCCESS = 2003;
        public const int USER_JOINED = 2004;
        public const int USER_LEFT = 2005;
        public const int CHAT_BROADCAST = 2006;
        public const int ROOM_CLOSED = 2007;
        public const int HEARTBEAT_ACK = 2008;
        public const int ERROR = 2999;

        // !!!
        //public const int COMMAND_FAILED = 2998;  // 명령 실패 프로토콜 추가
        // !!!

        // 로비 응답
        public const int ROOM_JOINED = 4201;
        public const int PLAYER_JOINED_ROOM = 4203;
        public const int PLAYER_LEFT_ROOM = 4204;
        public const int ROOM_LIST = 4205;
        public const int PLAYER_READY_STATE = 4206;
        public const int MAP_LIST = 4210;
        public const int TABLE_DATA = 4211;

        // 게임 이벤트
        public const int GAME_STARTED = 4002;
        public const int RESOURCES_UPDATED = 4003;
        public const int FLEET_SPAWNED = 4004;
        public const int FLEET_MOVING = 4005;
        public const int FLEET_ARRIVED = 4006;
        public const int COMBAT_STARTED = 4007;
        public const int COMBAT_ENDED = 4008;
        public const int PLANET_CONQUERED = 4009;
        public const int GAME_ENDED = 4010;
        public const int TICK_COMMANDS = 4103;
    }
}
