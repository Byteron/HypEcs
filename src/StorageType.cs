using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace HypEcs;

public readonly struct StorageType : IComparable<StorageType>
{
    public readonly Type Type;
    public readonly ulong Value;
    public readonly bool IsRelation;

    public ushort TypeId
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => TypeIdConverter.Type(Value);
    }

    public Identity Identity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => TypeIdConverter.Identity(Value);
    }

    public StorageType (
        Type type,
        ulong value,
        bool isRelation
    ) {
        Type = type;
        Value = value;
        IsRelation = isRelation;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StorageType Create<T>(Identity identity = default)
    {
        return new StorageType(
            value: TypeIdConverter.Value<T>(identity),
            type: typeof(T),
            isRelation: identity.Id > 0
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(StorageType other)
    {
        return Value.CompareTo(other.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj)
    {
        return (obj is StorageType other) && Value == other.Value;
    }
        
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(StorageType other)
    {
        return Value == other.Value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString()
    {
        return IsRelation ? $"{GetHashCode()} {Type.Name}::{Identity}" : $"{GetHashCode()} {Type.Name}";
    }

    public static bool operator ==(StorageType left, StorageType right) => left.Equals(right);
    public static bool operator !=(StorageType left, StorageType right) => !left.Equals(right);
}
    
public static class TypeIdConverter
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Value<T>(Identity identity)
    {
        return TypeIdAssigner<T>.Id | (ulong)identity.Generation << 16 | (ulong)identity.Id << 32;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Identity Identity(ulong value)
    {
        return new Identity((int)(value >> 32), (ushort)(value >> 16));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort Type(ulong value)
    {
        return (ushort)value;
    }

    class TypeIdAssigner
    {
        protected static ushort Counter;
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    class TypeIdAssigner<T> : TypeIdAssigner
    {
        // ReSharper disable once StaticMemberInGenericType
        public static readonly ushort Id;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static TypeIdAssigner()
        {
            Id = ++Counter;
        }
    }
}