using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace CommonLib
{
    public class Protocol
    {
        public int Type { get; set; }
        public long Timestamp { get; set; }
        private Dictionary<string, object> _parameters = new Dictionary<string, object>();

        public Protocol(int type)
        {
            Type = type;
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public Protocol AddParam(string key, object value)
        {
            _parameters[key] = value;
            return this;
        }

        public T GetParam<T>(string key)
        {
            if (_parameters.TryGetValue(key, out object value))
            {
                if (value is T directValue)
                    return directValue;

                // JSON 문자열인 경우 역직렬화 시도
                if (value is string jsonString && typeof(T) != typeof(string))
                {
                    try
                    {
                        return JsonConvert.DeserializeObject<T>(jsonString);
                    }
                    catch
                    {
                        // 역직렬화 실패 시 기본값 반환
                    }
                }

                // 타입 변환 시도
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    // 변환 실패 시 기본값 반환
                }
            }
            return default(T);
        }

        public T GetStruct<T>(string key) where T : struct
        {
            if (_parameters.TryGetValue(key, out object value))
            {
                if (value is string jsonString)
                {
                    try
                    {
                        return JsonConvert.DeserializeObject<T>(jsonString);
                    }
                    catch
                    {
                        // 역직렬화 실패 시 기본값 반환
                    }
                }
            }
            return default(T);
        }

        public byte[] Serialize()
        {
            // 간단한 바이너리 직렬화 구현
            string jsonData = JsonConvert.SerializeObject(_parameters);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonData);

            byte[] result = new byte[18 + jsonBytes.Length]; // 헤더(18) + 데이터

            // 전체 크기 (4바이트)
            BitConverter.GetBytes(result.Length).CopyTo(result, 0);

            // 프로토콜 타입 (4바이트)
            BitConverter.GetBytes(Type).CopyTo(result, 4);

            // 타임스탬프 (8바이트)
            BitConverter.GetBytes(Timestamp).CopyTo(result, 8);

            // 데이터 개수 (2바이트)
            BitConverter.GetBytes((ushort)_parameters.Count).CopyTo(result, 16);

            // JSON 데이터
            jsonBytes.CopyTo(result, 18);

            return result;
        }

        public static Protocol Deserialize(byte[] data)
        {
            if (data.Length < 18) throw new ArgumentException("Invalid protocol data");

            int type = BitConverter.ToInt32(data, 4);
            long timestamp = BitConverter.ToInt64(data, 8);

            Protocol protocol = new Protocol(type) { Timestamp = timestamp };

            if (data.Length > 18)
            {
                string jsonData = Encoding.UTF8.GetString(data, 18, data.Length - 18);
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData);
                if (parameters != null)
                {
                    protocol._parameters = parameters;
                }
            }

            return protocol;
        }
    }
}
