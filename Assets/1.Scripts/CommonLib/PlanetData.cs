using System;
using UnityEngine;

namespace CommonLib
{
    [Serializable]
    public struct PlanetData
    {
        public int PlanetId;
        public int OwnerId;
        public Vector2 Position;
        public int Minerals;
        public int Gas;
        public string Name;
        public int Supply;
    }
}
