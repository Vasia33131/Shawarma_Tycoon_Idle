using System;
using UnityEngine;

namespace ShaurmaTycoon.Ambient
{
    /// <summary>Высота транспорта на экране (картинки нарисованы в разном масштабе).</summary>
    [Serializable]
    public struct VehicleSize
    {
        public Sprite Sprite;
        public float Height;
    }
}
