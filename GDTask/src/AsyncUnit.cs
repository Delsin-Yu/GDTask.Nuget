#pragma warning disable CS1591 // Missing XML comment for publicly visible type or 

using System;

namespace GodotTask
{
    public readonly struct AsyncUnit : IEquatable<AsyncUnit>
    {
        public static readonly AsyncUnit Default = new AsyncUnit();

        public override int GetHashCode() => 0;

        public bool Equals(AsyncUnit other) => true;

        public override string ToString() => "()";

        public override bool Equals(object obj) => obj is AsyncUnit;

        public static bool operator ==(AsyncUnit left, AsyncUnit right) => true;

        public static bool operator !=(AsyncUnit left, AsyncUnit right) => false;
    }
}
