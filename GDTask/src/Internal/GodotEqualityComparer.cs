using System;
using System.Collections.Generic;
using Godot;

namespace GodotTask.Internal
{
    internal static class GodotEqualityComparer
    {
        public static readonly IEqualityComparer<Vector2> Vector2 = new Vector2EqualityComparer();
        public static readonly IEqualityComparer<Vector3> Vector3 = new Vector3EqualityComparer();
        public static readonly IEqualityComparer<Color> Color = new ColorEqualityComparer();
        public static readonly IEqualityComparer<Rect2> Rect2 = new Rect2EqualityComparer();
        public static readonly IEqualityComparer<Aabb> AABB = new AABBEqualityComparer();
        public static readonly IEqualityComparer<Quaternion> Quaternion = new QuatEqualityComparer();

        private static readonly RuntimeTypeHandle vector2Type = typeof(Vector2).TypeHandle;
        private static readonly RuntimeTypeHandle vector3Type = typeof(Vector3).TypeHandle;
        private static readonly RuntimeTypeHandle colorType = typeof(Color).TypeHandle;
        private static readonly RuntimeTypeHandle rectType = typeof(Rect2).TypeHandle;
        private static readonly RuntimeTypeHandle AABBType = typeof(Aabb).TypeHandle;
        private static readonly RuntimeTypeHandle quaternionType = typeof(Quaternion).TypeHandle;

        private static class Cache<T>
        {
            public static readonly IEqualityComparer<T> Comparer;

            static Cache()
            {
                var comparer = GetDefaultHelper(typeof(T));
                if (comparer == null)
                {
                    Comparer = EqualityComparer<T>.Default;
                }
                else
                {
                    Comparer = (IEqualityComparer<T>)comparer;
                }
            }
        }

        public static IEqualityComparer<T> GetDefault<T>()
        {
            return Cache<T>.Comparer;
        }

        private static object GetDefaultHelper(Type type)
        {
            var t = type.TypeHandle;

            if (t.Equals(vector2Type)) return Vector2;
            if (t.Equals(vector3Type)) return Vector3;
            if (t.Equals(colorType)) return Color;
            if (t.Equals(rectType)) return Rect2;
            if (t.Equals(AABBType)) return AABB;
            if (t.Equals(quaternionType)) return Quaternion;

            return null;
        }

        private sealed class Vector2EqualityComparer : IEqualityComparer<Vector2>
        {
            public bool Equals(Vector2 self, Vector2 vector) => self.Equals(vector);

            public int GetHashCode(Vector2 obj) => obj.GetHashCode();
        }

        private sealed class Vector3EqualityComparer : IEqualityComparer<Vector3>
        {
            public bool Equals(Vector3 self, Vector3 vector) => self.Equals(vector);

            public int GetHashCode(Vector3 obj) => obj.GetHashCode();
        }

        private sealed class ColorEqualityComparer : IEqualityComparer<Color>
        {
            public bool Equals(Color self, Color other) => self.Equals(other);

            public int GetHashCode(Color obj) => obj.GetHashCode();
        }

        private sealed class Rect2EqualityComparer : IEqualityComparer<Rect2>
        {
            public bool Equals(Rect2 self, Rect2 other) => self.Equals(other);

            public int GetHashCode(Rect2 obj) => obj.GetHashCode();
        }

        private sealed class AABBEqualityComparer : IEqualityComparer<Aabb>
        {
            public bool Equals(Aabb self, Aabb vector) => self.Equals(vector);

            public int GetHashCode(Aabb obj) => obj.GetHashCode();
        }

        private sealed class QuatEqualityComparer : IEqualityComparer<Quaternion>
        {
            public bool Equals(Quaternion self, Quaternion vector) => self.Equals(vector);

            public int GetHashCode(Quaternion obj) => obj.GetHashCode();
        }
    }
}
