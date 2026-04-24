using System.Collections.Generic;

using UnityEngine;

namespace ModifiableVariable.Comparers
{
    public sealed class FloatComparer : IEqualityComparer<float>
    {
        public static readonly FloatComparer Default = new(0.0001f);
        readonly float _epsilon;
        public FloatComparer(float epsilon = 0.0001f) => _epsilon = epsilon;
        public bool Equals(float a, float b) => Mathf.Abs(a - b) < _epsilon;
        public int GetHashCode(float v) => v.GetHashCode();
    }

    public sealed class IntComparer : IEqualityComparer<int>
    {
        public bool Equals(int a, int b) => a == b;
        public int GetHashCode(int v) => v;
    }

    public sealed class Vector2Comparer : IEqualityComparer<Vector2>
    {
        public static readonly Vector2Comparer Default = new(0.0001f);
        readonly float _epsilon;
        public Vector2Comparer(float epsilon = 0.0001f) => _epsilon = epsilon;
        public bool Equals(Vector2 a, Vector2 b) => (a - b).sqrMagnitude < _epsilon * _epsilon;
        public int GetHashCode(Vector2 v) => v.GetHashCode();
    }

    public sealed class Vector2IntComparer : IEqualityComparer<Vector2Int>
    {
        public bool Equals(Vector2Int a, Vector2Int b) => a == b;
        public int GetHashCode(Vector2Int v) => v.GetHashCode();
    }

    public sealed class Vector3Comparer : IEqualityComparer<Vector3>
    {
        public static readonly Vector3Comparer Default = new(0.0001f);
        readonly float _epsilon;
        public Vector3Comparer(float epsilon = 0.0001f) => _epsilon = epsilon;
        public bool Equals(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < _epsilon * _epsilon;
        public int GetHashCode(Vector3 v) => v.GetHashCode();
    }

    public sealed class Vector3IntComparer : IEqualityComparer<Vector3Int>
    {
        public bool Equals(Vector3Int a, Vector3Int b) => a == b;
        public int GetHashCode(Vector3Int v) => v.GetHashCode();
    }

    public sealed class Vector4Comparer : IEqualityComparer<Vector4>
    {
        public bool Equals(Vector4 a, Vector4 b) => a == b;
        public int GetHashCode(Vector4 v) => v.GetHashCode();
    }

    public sealed class ColorComparer : IEqualityComparer<Color>
    {
        public bool Equals(Color a, Color b) => a == b;
        public int GetHashCode(Color v) => v.GetHashCode();
    }

    public sealed class Color32Comparer : IEqualityComparer<Color32>
    {
        public bool Equals(Color32 a, Color32 b) => a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;
        public int GetHashCode(Color32 v) => v.GetHashCode();
    }

    public sealed class QuaternionComparer : IEqualityComparer<Quaternion>
    {
        public bool Equals(Quaternion a, Quaternion b) => a == b;
        public int GetHashCode(Quaternion v) => v.GetHashCode();
    }
}