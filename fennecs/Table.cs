// SPDX-License-Identifier: MIT

using System.Text;

namespace fennecs;

public sealed class TableEdge
{
    public Table? Add;
    public Table? Remove;
}

public sealed class Table
{
    private const int StartCapacity = 4;

    public readonly int Id;

    public readonly SortedSet<TypeExpression> Types;

    public Identity[] Identities => _identities;
    
    internal Array[] Storages => _storages;

    public int Count { get; private set; }
    public bool IsEmpty => Count == 0;

    internal int Version => Volatile.Read(ref _version);
    public int Capacity => _identities.Length;

    private readonly World _archetypes;

    private Identity[] _identities;
    private readonly Array[] _storages;

    private readonly Dictionary<TypeExpression, TableEdge> _edges = new();
    private readonly Dictionary<TypeExpression, int> _indices = new();

    // Used by Queries to check if the table has been modified while enumerating.
    private int _version;


    public Table(int id, World archetypes, SortedSet<TypeExpression> types)
    {
        _archetypes = archetypes;

        Id = id;
        Types = types;

        _identities = new Identity[StartCapacity];

        var i = 0;
        foreach (var type in types)
        {
            _indices.Add(type, i++);
        }

        _storages = new Array[_indices.Count];

        foreach (var (type, index) in _indices)
        {
            _storages[index] = Array.CreateInstance(type.Type, StartCapacity);
        }
    }

    internal bool Matches(TypeExpression type)
    {
        return type.Matches(Types);
    }


    internal bool Matches(Mask mask)
    {
        //Not overrides both Any and Has.
        var matchesNot = !mask.NotTypes.Any(t => t.Matches(Types));
        if (!matchesNot) return false;
        
        //If already matching, no need to check any further. 
        var matchesHas = mask.HasTypes.All(t => t.Matches(Types));
        if (!matchesHas) return false;
        
        //Short circuit to avoid enumerating all AnyTypes if already matching; or if none present.
        var matchesAny = mask.AnyTypes.Count == 0;
        matchesAny |= mask.AnyTypes.Any(t => t.Matches(Types));

        return matchesHas && matchesNot && matchesAny;
    }

    
    public int Add(Identity identity)
    {
        Interlocked.Increment(ref _version);
        
        EnsureCapacity(Count + 1);
        _identities[Count] = identity;
        return Count++;
    }

    
    public void Remove(int row)
    {
        Interlocked.Increment(ref _version);
        
        ArgumentOutOfRangeException.ThrowIfGreaterThan(row, Count, nameof(row));
        
        Count--;

        // If removing not the last row, move the last row to the removed row
        if (row < Count)
        {
            _identities[row] = _identities[Count];
            foreach (var storage in _storages)
            {
                Array.Copy(storage, Count, storage, row, 1);
            }
            _archetypes.GetEntityMeta(_identities[row]).Row = row;
        }

        // Free the last row
        _identities[Count] = default;
        
        foreach (var storage in _storages) Array.Clear(storage, Count, 1);
    }

    
    public TableEdge GetTableEdge(TypeExpression typeExpression)
    {
        if (_edges.TryGetValue(typeExpression, out var edge)) return edge;

        edge = new TableEdge();
        _edges[typeExpression] = edge;

        return edge;
    }


    public T[] GetStorage<T>(Identity target)
    {
        var type = TypeExpression.Create<T>(target);
        return (T[]) GetStorage(type);
    }


    public Memory<T> Memory<T>(Identity target)
    {
        var type = TypeExpression.Create<T>(target);
        var storage = (T[]) GetStorage(type);
        return storage.AsMemory(0, Count);
    }


    public Array GetStorage(TypeExpression typeExpression)
    {
        return _storages[_indices[typeExpression]];
    }


    private void EnsureCapacity(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity, nameof(capacity));
        
        if (capacity <= _identities.Length) return;

        Resize(Math.Max(capacity, StartCapacity) * 2);
    }


    internal void Resize(int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length, nameof(length));
        ArgumentOutOfRangeException.ThrowIfLessThan(length, Count, nameof(length));

        Array.Resize(ref _identities, length);

        for (var i = 0; i < _storages.Length; i++)
        {
            var elementType = _storages[i].GetType().GetElementType()!;
            var newStorage = Array.CreateInstance(elementType, length);
            Array.Copy(_storages[i], newStorage, Math.Min(_storages[i].Length, length));
            _storages[i] = newStorage;
        }
    }

    
    public static int MoveEntry(Identity identity, int oldRow, Table oldTable, Table newTable)
    {
        var newRow = newTable.Add(identity);

        foreach (var (type, oldIndex) in oldTable._indices)
        {
            if (!newTable._indices.TryGetValue(type, out var newIndex) || newIndex < 0) continue;

            var oldStorage = oldTable._storages[oldIndex];
            var newStorage = newTable._storages[newIndex];

            Array.Copy(oldStorage, oldRow, newStorage, newRow, 1);
        }

        oldTable.Remove(oldRow);

        return newRow;
    }

    public override string ToString()
    {
        var sb = new StringBuilder($"Table {Id} ");
        sb.AppendJoin(" ", Types);
        return sb.ToString();
    }
}