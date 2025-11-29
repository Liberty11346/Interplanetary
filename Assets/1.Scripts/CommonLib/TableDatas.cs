using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.TableData
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class DbColumnAttribute : Attribute
    {
        public string ColumnName { get; }

        public DbColumnAttribute(string columnName)
        {
            ColumnName = columnName;
        }
    }

    // 바이너리 데이터 리더
    public class BinaryDataReader
    {
        private byte[] _data;
        private int _position;

        public BinaryDataReader(byte[] data)
        {
            _data = data;
            _position = 0;
        }

        public int ReadInt32()
        {
            int value = BitConverter.ToInt32(_data, _position);
            _position += 4;
            return value;
        }

        public float ReadSingle()
        {
            float value = BitConverter.ToSingle(_data, _position);
            _position += 4;
            return value;
        }

        public string ReadString()
        {
            int length = ReadInt32();
            if (length == 0)
                return string.Empty;

            string value = Encoding.UTF8.GetString(_data, _position, length);
            _position += length;
            return value;
        }

        public bool CanRead => _position < _data.Length;

        public void Reset() => _position = 0;
    }

    [Serializable]
    public class FleetInfoData
    {
        [DbColumn("id")] public readonly int id;
        [DbColumn("name")] public readonly string Name;
        [DbColumn("type")] public readonly int Type;
        [DbColumn("max_health")] public readonly int MaxHealth;
        [DbColumn("attack_power")] public readonly int AttackPower;
        [DbColumn("move_speed")] public readonly float MoveSpeed;

        public FleetInfoData(int id, string name, int type, int maxHealth, int attackPower, float moveSpeed)
        {
            this.id = id;
            this.Name = name;
            this.Type = type;
            this.MaxHealth = maxHealth;
            this.AttackPower = attackPower;
            this.MoveSpeed = moveSpeed;
        }

        public static Dictionary<int, FleetInfoData> Convert(byte[] data)
        {
            var reader = new BinaryDataReader(data);
            var result = new Dictionary<int, FleetInfoData>();

            int count = reader.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                try
                {
                    int id = reader.ReadInt32();
                    string name = reader.ReadString();
                    int type = reader.ReadInt32();
                    int maxHealth = reader.ReadInt32();
                    int attackPower = reader.ReadInt32();
                    float moveSpeed = reader.ReadSingle();

                    if (string.IsNullOrEmpty(name))
                        continue;

                    FleetInfoData fleetData = new FleetInfoData(
                        id: id,
                        name: name,
                        type: type,
                        maxHealth: maxHealth,
                        attackPower: attackPower,
                        moveSpeed: moveSpeed
                    );

                    result.Add(fleetData.id, fleetData);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"Error reading FleetInfoData at index {i}: {ex.Message}");
                    continue;
                }
            }

            return result;
        }

        public static byte[] Serialize(Dictionary<int, FleetInfoData> data)
        {
            using (var ms = new System.IO.MemoryStream())
            using (var writer = new System.IO.BinaryWriter(ms))
            {
                writer.Write(data.Count);

                foreach (var item in data.Values)
                {
                    writer.Write(item.id);

                    byte[] nameBytes = Encoding.UTF8.GetBytes(item.Name);
                    writer.Write(nameBytes.Length);
                    writer.Write(nameBytes);

                    writer.Write(item.Type);
                    writer.Write(item.MaxHealth);
                    writer.Write(item.AttackPower);
                    writer.Write(item.MoveSpeed);
                }

                return ms.ToArray();
            }
        }
    }

    [Serializable]
    public class ProductionInfoData
    {
        [DbColumn("id")] public readonly int id;
        [DbColumn("target_id")] public readonly int Targetid;
        [DbColumn("production_time")] public readonly float ProductionTime;
        [DbColumn("gas_cost")] public readonly int GasCost;
        [DbColumn("mineral_cost")] public readonly int MineralCost;
        [DbColumn("supply_cost")] public readonly int SupplyCost;

        public ProductionInfoData(int id, int targetid, float productionTime, int gasCost, int mineralCost, int supplyCost)
        {
            this.id = id;
            this.Targetid = targetid;
            this.ProductionTime = productionTime;
            this.GasCost = gasCost;
            this.MineralCost = mineralCost;
            this.SupplyCost = supplyCost;
        }

        public static Dictionary<int, ProductionInfoData> Convert(byte[] data)
        {
            var reader = new BinaryDataReader(data);
            var result = new Dictionary<int, ProductionInfoData>();

            int count = reader.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                try
                {
                    int id = reader.ReadInt32();
                    int targetId = reader.ReadInt32();
                    float productionTime = reader.ReadSingle();
                    int gasCost = reader.ReadInt32();
                    int mineralCost = reader.ReadInt32();
                    int supplyCost = reader.ReadInt32();

                    ProductionInfoData productionData = new ProductionInfoData(
                        id: id,
                        targetid: targetId,
                        productionTime: productionTime,
                        gasCost: gasCost,
                        mineralCost: mineralCost,
                        supplyCost: supplyCost
                    );

                    result.Add(productionData.id, productionData);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"Error reading ProductionInfoData at index {i}: {ex.Message}");
                    continue;
                }
            }

            return result;
        }

        public static byte[] Serialize(Dictionary<int, ProductionInfoData> data)
        {
            using (var ms = new System.IO.MemoryStream())
            using (var writer = new System.IO.BinaryWriter(ms))
            {
                writer.Write(data.Count);

                foreach (var item in data.Values)
                {
                    writer.Write(item.id);
                    writer.Write(item.Targetid);
                    writer.Write(item.ProductionTime);
                    writer.Write(item.GasCost);
                    writer.Write(item.MineralCost);
                    writer.Write(item.SupplyCost);
                }

                return ms.ToArray();
            }
        }
    }

    [Serializable]
    public class PlanetInfoData
    {
        [DbColumn("id")] public readonly int id;
        [DbColumn("name")] public readonly string Name;
        [DbColumn("gas")] public readonly int Gas;
        [DbColumn("mineral")] public readonly int Mineral;
        [DbColumn("supply")] public readonly int Supply;

        public PlanetInfoData(int id, string name, int gas, int mineral, int supply)
        {
            this.id = id;
            this.Name = name;
            this.Gas = gas;
            this.Mineral = mineral;
            this.Supply = supply;
        }

        public static Dictionary<int, PlanetInfoData> Convert(byte[] data)
        {
            var reader = new BinaryDataReader(data);
            var result = new Dictionary<int, PlanetInfoData>();

            int count = reader.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                try
                {
                    int id = reader.ReadInt32();
                    string name = reader.ReadString();
                    int gas = reader.ReadInt32();
                    int mineral = reader.ReadInt32();
                    int supply = reader.ReadInt32();

                    if (string.IsNullOrEmpty(name))
                        continue;

                    PlanetInfoData planetData = new PlanetInfoData(
                        id: id,
                        name: name,
                        gas: gas,
                        mineral: mineral,
                        supply: supply
                    );

                    result.Add(planetData.id, planetData);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"Error reading PlanetInfoData at index {i}: {ex.Message}");
                    continue;
                }
            }

            return result;
        }

        public static byte[] Serialize(Dictionary<int, PlanetInfoData> data)
        {
            using (var ms = new System.IO.MemoryStream())
            using (var writer = new System.IO.BinaryWriter(ms))
            {
                writer.Write(data.Count);

                foreach (var item in data.Values)
                {
                    writer.Write(item.id);

                    byte[] nameBytes = Encoding.UTF8.GetBytes(item.Name);
                    writer.Write(nameBytes.Length);
                    writer.Write(nameBytes);

                    writer.Write(item.Gas);
                    writer.Write(item.Mineral);
                    writer.Write(item.Supply);
                }

                return ms.ToArray();
            }
        }
    }

    [Serializable]
    public class MapInfoData
    {
        [DbColumn("id")] public readonly int id;
        [DbColumn("name")] public readonly string Name;
        [DbColumn("description")] public readonly string Description;
        [DbColumn("player1_homeworld_id")] public readonly int Player1_HomeID;
        [DbColumn("player2_homeworld_id")] public readonly int Player2_HomeID;

        public MapInfoData(int id, string name, string description, int player1HomeID, int player2HomeID)
        {
            this.id = id;
            this.Name = name;
            this.Description = description;
            this.Player1_HomeID = player1HomeID;
            this.Player2_HomeID = player2HomeID;
        }

        public static Dictionary<int, MapInfoData> Convert(byte[] data)
        {
            var reader = new BinaryDataReader(data);
            var result = new Dictionary<int, MapInfoData>();

            int count = reader.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                try
                {
                    int id = reader.ReadInt32();
                    string name = reader.ReadString();
                    string description = reader.ReadString();
                    int player1HomeId = reader.ReadInt32();
                    int player2HomeId = reader.ReadInt32();

                    if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(description))
                        continue;

                    MapInfoData mapData = new MapInfoData(
                        id: id,
                        name: name,
                        description: description,
                        player1HomeID: player1HomeId,
                        player2HomeID: player2HomeId
                    );

                    result.Add(mapData.id, mapData);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"Error reading MapInfoData at index {i}: {ex.Message}");
                    continue;
                }
            }

            return result;
        }

        public static byte[] Serialize(Dictionary<int, MapInfoData> data)
        {
            using (var ms = new System.IO.MemoryStream())
            using (var writer = new System.IO.BinaryWriter(ms))
            {
                writer.Write(data.Count);

                foreach (var item in data.Values)
                {
                    writer.Write(item.id);

                    byte[] nameBytes = Encoding.UTF8.GetBytes(item.Name);
                    writer.Write(nameBytes.Length);
                    writer.Write(nameBytes);

                    byte[] descBytes = Encoding.UTF8.GetBytes(item.Description);
                    writer.Write(descBytes.Length);
                    writer.Write(descBytes);

                    writer.Write(item.Player1_HomeID);
                    writer.Write(item.Player2_HomeID);
                }

                return ms.ToArray();
            }
        }
    }

    [Serializable]
    public class MapPlanetInfoData
    {
        [DbColumn("id")] public readonly int id;
        [DbColumn("map_id")] public readonly int mapId;
        [DbColumn("planet_id")] public readonly int planetId;
        [DbColumn("position_x")] public readonly float PositionX;
        [DbColumn("position_y")] public readonly float PositionY;

        public MapPlanetInfoData(int id, int mapId, int planetId, float positionX, float positionY)
        {
            this.id = id;
            this.mapId = mapId;
            this.planetId = planetId;
            this.PositionX = positionX;
            this.PositionY = positionY;
        }

        public static Dictionary<int, MapPlanetInfoData> Convert(byte[] data)
        {
            var reader = new BinaryDataReader(data);
            var result = new Dictionary<int, MapPlanetInfoData>();

            int count = reader.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                try
                {
                    int id = reader.ReadInt32();
                    int mapId = reader.ReadInt32();
                    int planetId = reader.ReadInt32();
                    float positionX = reader.ReadSingle();
                    float positionY = reader.ReadSingle();

                    MapPlanetInfoData mapPlanetData = new MapPlanetInfoData(
                        id: id,
                        mapId: mapId,
                        planetId: planetId,
                        positionX: positionX,
                        positionY: positionY
                    );

                    result.Add(mapPlanetData.id, mapPlanetData);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"Error reading MapPlanetInfoData at index {i}: {ex.Message}");
                    continue;
                }
            }

            return result;
        }

        public static byte[] Serialize(Dictionary<int, MapPlanetInfoData> data)
        {
            using (var ms = new System.IO.MemoryStream())
            using (var writer = new System.IO.BinaryWriter(ms))
            {
                writer.Write(data.Count);

                foreach (var item in data.Values)
                {
                    writer.Write(item.id);
                    writer.Write(item.mapId);
                    writer.Write(item.planetId);
                    writer.Write(item.PositionX);
                    writer.Write(item.PositionY);
                }

                return ms.ToArray();
            }
        }
    }

    [Serializable]
    public class MapRouteInfoData
    {
        [DbColumn("id")] public readonly int id;
        [DbColumn("map_id")] public readonly int mapId;
        [DbColumn("planet_from_id")] public readonly int planetFromId;
        [DbColumn("planet_to_id")] public readonly int planetToId;

        public MapRouteInfoData(int id, int mapId, int planetFromId, int planetToId)
        {
            this.id = id;
            this.mapId = mapId;
            this.planetFromId = planetFromId;
            this.planetToId = planetToId;
        }

        public static Dictionary<int, MapRouteInfoData> Convert(byte[] data)
        {
            var reader = new BinaryDataReader(data);
            var result = new Dictionary<int, MapRouteInfoData>();

            int count = reader.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                try
                {
                    int id = reader.ReadInt32();
                    int mapId = reader.ReadInt32();
                    int planetFromId = reader.ReadInt32();
                    int planetToId = reader.ReadInt32();

                    MapRouteInfoData mapRouteData = new MapRouteInfoData(
                        id: id,
                        mapId: mapId,
                        planetFromId: planetFromId,
                        planetToId: planetToId
                    );

                    result.Add(mapRouteData.id, mapRouteData);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"Error reading MapRouteInfoData at index {i}: {ex.Message}");
                    continue;
                }
            }

            return result;
        }

        public static byte[] Serialize(Dictionary<int, MapRouteInfoData> data)
        {
            using (var ms = new System.IO.MemoryStream())
            using (var writer = new System.IO.BinaryWriter(ms))
            {
                writer.Write(data.Count);

                foreach (var item in data.Values)
                {
                    writer.Write(item.id);
                    writer.Write(item.mapId);
                    writer.Write(item.planetFromId);
                    writer.Write(item.planetToId);
                }

                return ms.ToArray();
            }
        }
    }
}
