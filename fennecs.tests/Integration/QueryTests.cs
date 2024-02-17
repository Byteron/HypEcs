﻿using System.Collections;
using System.Numerics;

namespace fennecs.tests.Integration;

public static class QueryTests
{
    [Fact]
    private static void Can_Enumerate_PlainEnumerator()
    {
        using var world = new World();

        var entities = new List<Entity>();
        for (var i = 0; i < 234; i++)
        {
            var entity = world.Spawn().Add(new object()).Id();
            entities.Add(entity);
        }
        
        var query = world.Query<object>().Build();
        var plain = query as IEnumerable;
        
        var enumerator = plain.GetEnumerator();
        using var disposable = enumerator as IDisposable;
        while (enumerator.MoveNext())
        {
            Assert.IsType<Entity>(enumerator.Current);
            
            var entity = (Entity) enumerator.Current;
            Assert.Contains(entity, entities);
            entities.Remove(entity);            
        }
        Assert.Empty(entities);
    }

    [Fact]
    private static void Contains_Finds_Entity()
    {
        using var world = new World();

        var random = new Random(1234);
        var entities = new List<Entity>();
        for (var i = 0; i < 2345; i++)
        {
            var entity = world.Spawn().Add(i).Id();
            entities.Add(entity);
        }

        var query = world.Query<int>().Build();

        Assert.True(entities.All(e => query.Contains(e)));

        var former = entities.ToArray(); 
        while (entities.Count > 0)
        {
            var index = random.Next(entities.Count);
            var entity = entities[index];
            world.Despawn(entity);
            Assert.False(query.Contains(entity));
            entities.RemoveAt(index);
        }
        
        Assert.True(!former.Any(e => query.Contains(e))); 
    }
    
    [Fact]
    private static void Has_Matches()
    {
        var p1 = new Vector3(6, 6, 6);
        var p2 = new Vector3(1, 2, 3);

        using var world = new World();
        world.Spawn().Add(p1).Id();
        world.Spawn().Add(p2).Add<int>().Id();
        world.Spawn().Add(p2).Add<int>().Id();

        var query = world.Query<Vector3>()
            .Has<int>()
            .Build();

        query.Raw(memory =>
        {
            Assert.True(memory.Length == 2);
            foreach (var pos in memory.Span) Assert.Equal(p2, pos);
        });
    }

    [Fact]
    private static void Not_prevents_Match()
    {
        var p1 = new Vector3(6, 6, 6);
        var p2 = new Vector3(1, 2, 3);

        using var world = new World();
        world.Spawn().Add(p1).Id();
        world.Spawn().Add(p2).Add<int>().Id();
        world.Spawn().Add(p2).Add<int>().Id();

        var query = world.Query<Vector3>()
            .Not<int>()
            .Build();

        query.Raw(memory =>
        {
            Assert.True(memory.Length == 1);
            foreach (var pos in memory.Span) Assert.Equal(p1, pos);
        });
    }

    [Fact]
    private static void Any_Target_None_Matches_Only_None()
    {
        var p1 = new Vector3(6, 6, 6);
        var p2 = new Vector3(1, 2, 3);
        var p3 = new Vector3(4, 4, 4);

        using var world = new World();
        var alice = world.Spawn().Add(p1).Add(0).Id();
        var bob = world.Spawn().Add(p2).Add(111, alice).Id();
        /*var charlie = */world.Spawn().Add(p3).Add(222, bob).Id();

        var query = world.Query<Entity, Vector3>()
            .Any<int>(Identity.None)
            .Build();

        var count = 0;
        query.Raw((me, mp) =>
        {
            count++;
            Assert.Equal(1, mp.Length);
            var entity = me.Span[0];
            Assert.Equal(alice, entity);
        });
        Assert.Equal(1, count);
    }

    [Fact]
    private static void Any_Target_Single_Matches()
    {
        var p1 = new Vector3(6, 6, 6);
        var p2 = new Vector3(1, 2, 3);
        var p3 = new Vector3(4, 4, 4);

        using var world = new World();
        var alice = world.Spawn().Add(p1).Add(0).Id();
        var eve = world.Spawn().Add(p2).Add(111, alice).Id();
        var charlie = world.Spawn().Add(p3).Add(222, eve).Id();

        var query = world.Query<Entity, Vector3>().Any<int>(eve).Build();

        var count = 0;
        query.Raw((me, mp) =>
        {
            count++;
            Assert.Equal(1, mp.Length);
            var entity = me.Span[0];
            Assert.Equal(charlie, entity);
            var pos = mp.Span[0];
            Assert.Equal(pos, p3);
        });
        Assert.Equal(1, count);
    }

    [Fact]
    private static void Any_Target_Multiple_Matches()
    {
        var p1 = new Vector3(6, 6, 6);
        var p2 = new Vector3(1, 2, 3);
        var p3 = new Vector3(4, 4, 4);

        using var world = new World();
        var alice = world.Spawn().Add(p1).Add(0).Id();
        var eve = world.Spawn().Add(p2).Add(111, alice).Id();
        var charlie = world.Spawn().Add(p3).Add(222, eve).Id();

        var query = world.Query<Entity, Vector3>()
            .Any<int>(eve)
            .Any<int>(alice)
            .Build();

        var count = 0;
        query.Raw((me, mp) =>
        {
            Assert.Equal(1, mp.Length);
            for (var index = 0; index < me.Length; index++)
            {
                var entity = me.Span[index];
                count++;
                if (entity == charlie)
                {
                    var pos = mp.Span[index];
                    Assert.Equal(pos, p3);
                }
                else if (entity == eve)
                {
                    var pos = mp.Span[index];
                    Assert.Equal(pos, p2);
                }
                else
                {
                    Assert.Fail("Unexpected entity");
                }
            }
        });
        Assert.Equal(2, count);
    }

    [Fact]
    private static void Any_Not_does_not_Match_Specific()
    {
        var p1 = new Vector3(6, 6, 6);
        var p2 = new Vector3(1, 2, 3);
        var p3 = new Vector3(4, 4, 4);

        using var world = new World();
        var alice = world.Spawn().Add(p1).Add(0).Id();
        var bob = world.Spawn().Add(p2).Add(111, alice).Id();
        var eve = world.Spawn().Add(p1).Add(888).Id();

        /*var charlie = */
        world.Spawn().Add(p3).Add(222, bob).Id();
        /*var charlie = */
        world.Spawn().Add(p3).Add(222, eve).Id();

        var query = world.Query<Entity, Vector3>()
            .Not<int>(bob)
            .Any<int>(alice)
            .Build();

        var count = 0;
        query.Raw((me, mp) =>
        {
            Assert.Equal(1, mp.Length);
            for (var index = 0; index < me.Length; index++)
            {
                var entity = me.Span[index];
                count++;
                if (entity == bob)
                {
                    var pos = mp.Span[index];
                    Assert.Equal(pos, p2);
                }
                else
                {
                    Assert.Fail("Unexpected entity");
                }
            }
        });
        Assert.Equal(1, count);
    }

    [Fact]
    private static void Query_provided_Has_works_with_Target()
    {
        var p1 = new Vector3(6, 6, 6);
        var p2 = new Vector3(1, 2, 3);
        var p3 = new Vector3(4, 4, 4);

        using var world = new World();

        var alice = world.Spawn().Add(p1).Add(0).Id();
        var eve = world.Spawn().Add(p1).Add(888).Id();

        var bob = world.Spawn().Add(p2).Add(111, alice).Id();

        world.Spawn().Add(p3).Add(555, bob).Id();
        world.Spawn().Add(p3).Add(666, eve).Id();

        var query = world.Query<Entity, Vector3, int>()
            .Not<int>(bob)
            .Build();

        var count = 0;
        query.Raw((me, mp, mi) =>
        {
            Assert.Equal(2, mp.Length);
            for (var index = 0; index < me.Length; index++)
            {
                var entity = me.Span[index];
                count++;
                
                if (entity == alice)
                {
                    var pos = mp.Span[0];
                    Assert.Equal(pos, p1);
                    var integer = mi.Span[index];
                    Assert.Equal(0, integer);
                }
                else if (entity == eve)
                {
                    var pos = mp.Span[index];
                    Assert.Equal(pos, p1);
                    var i = mi.Span[index];
                    Assert.Equal(888, i);
                }
                else
                {
                    Assert.Fail($"Unexpected entity {entity}");
                }
            }
        });
        Assert.Equal(2, count);
    }

    [Fact]
    private static void Queries_are_Cached()
    {
        using var world = new World();

        world.Spawn().Add(123);

        var query1A = world.Query().Build();
        var query1B = world.Query().Build();

        var query2A = world.Query<Entity>().Build();
        var query2B = world.Query<Entity>().Build();

        var query3A = world.Query().Has<int>().Build();
        var query3B = world.Query().Has<int>().Build();

        var query4A = world.Query<Entity>().Not<int>().Build();
        var query4B = world.Query<Entity>().Not<int>().Build();

        var query5A = world.Query<Entity>().Any<int>().Any<float>().Build();
        var query5B = world.Query<Entity>().Any<int>().Any<float>().Build();

        Assert.True(ReferenceEquals(query1A, query1B));
        Assert.True(ReferenceEquals(query2A, query2B));
        Assert.True(ReferenceEquals(query3A, query3B));
        Assert.True(ReferenceEquals(query4A, query4B));
        Assert.True(ReferenceEquals(query5A, query5B));
    }
}