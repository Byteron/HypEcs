﻿// SPDX-License-Identifier: MIT

namespace fennecs;

public class Query<C1, C2, C3>(World world, Mask mask, List<Table> tables) : Query(world, mask, tables)
{
    public RefValueTuple<C1, C2, C3> Get(Entity entity)
    {
        var meta = world.GetEntityMeta(entity.Identity);
        var table = world.GetTable(meta.TableId);
        var storage1 = table.GetStorage<C1>(Identity.None);
        var storage2 = table.GetStorage<C2>(Identity.None);
        var storage3 = table.GetStorage<C3>(Identity.None);
        return new RefValueTuple<C1, C2, C3>(ref storage1[meta.Row], ref storage2[meta.Row],
            ref storage3[meta.Row]);
    }

    #region Runners

    public void Run(RefAction_CCC<C1, C2, C3> action)
    {
        world.Lock();

        foreach (var table in Tables)
        {
            if (table.IsEmpty) continue;
            var s1 = table.GetStorage<C1>(Identity.None).AsSpan(0, table.Count);
            var s2 = table.GetStorage<C2>(Identity.None).AsSpan(0, table.Count);
            var s3 = table.GetStorage<C3>(Identity.None).AsSpan(0, table.Count);

            for (var i = 0; i < table.Count; i++) action(ref s1[i], ref s2[i], ref s3[i]);
        }

        world.Unlock();
    }

    public void RunParallel(RefAction_CCC<C1, C2, C3> action, int chunkSize = int.MaxValue)
    {
        world.Lock();

        using var countdown = new CountdownEvent(1);

        foreach (var table in Tables)
        {
            if (table.IsEmpty) continue;
            var storage1 = table.GetStorage<C1>(Identity.None);
            var storage2 = table.GetStorage<C2>(Identity.None);
            var storage3 = table.GetStorage<C3>(Identity.None);
            var length = table.Count;

            var partitions = Math.Max(length / chunkSize, 1);
            var partitionSize = length / partitions;

            for (var partition = 0; partition < partitions; partition++)
            {
                countdown.AddCount();

                ThreadPool.QueueUserWorkItem(delegate(int part)
                {
                    var s1 = storage1.AsSpan(part * partitionSize, partitionSize);
                    var s2 = storage2.AsSpan(part * partitionSize, partitionSize);
                    var s3 = storage3.AsSpan(part * partitionSize, partitionSize);

                    for (var i = 0; i < s1.Length; i++)
                    {
                        action(ref s1[i], ref s2[i], ref s3[i]);
                    }

                    // ReSharper disable once AccessToDisposedClosure
                    countdown.Signal();
                }, partition, preferLocal: true);
            }

            /*
            //Optimization: Also process one partition right here on the calling thread.
            var s1 = storage1.AsSpan(0, partitionSize);
            var s2 = storage2.AsSpan(0, partitionSize);
            var s3 = storage3.AsSpan(0, partitionSize);
            for (var i = 0; i < partitionSize; i++)
            {
                action(ref s1[i], ref s2[i], ref s3[i]);
            }
            */
        }

        countdown.Signal();
        countdown.Wait();
        world.Unlock();
    }

    public void Run<U>(RefAction_CCCU<C1, C2, C3, U> action, U uniform)
    {
        world.Lock();

        foreach (var table in Tables)
        {
            if (table.IsEmpty) continue;
            var s1 = table.GetStorage<C1>(Identity.None).AsSpan(0, table.Count);
            var s2 = table.GetStorage<C2>(Identity.None).AsSpan(0, table.Count);
            var s3 = table.GetStorage<C3>(Identity.None).AsSpan(0, table.Count);
            for (var i = 0; i < table.Count; i++) action(ref s1[i], ref s2[i], ref s3[i], uniform);
        }

        world.Unlock();
    }


    public void RunParallel<U>(RefAction_CCCU<C1, C2, C3, U> action, U uniform, int chunkSize = int.MaxValue)
    {
        world.Lock();
        using var countdown = new CountdownEvent(1);

        foreach (var table in Tables)
        {
            if (table.IsEmpty) continue;
            var storage1 = table.GetStorage<C1>(Identity.None);
            var storage2 = table.GetStorage<C2>(Identity.None);
            var storage3 = table.GetStorage<C3>(Identity.None);
            var length = table.Count;

            var partitions = Math.Max(length / chunkSize, 1);
            var partitionSize = length / partitions;

            for (var partition = 0; partition < partitions; partition++)
            {
                countdown.AddCount();

                ThreadPool.QueueUserWorkItem(delegate(int part)
                {
                    var s1 = storage1.AsSpan(part * partitionSize, partitionSize);
                    var s2 = storage2.AsSpan(part * partitionSize, partitionSize);
                    var s3 = storage3.AsSpan(part * partitionSize, partitionSize);

                    for (var i = 0; i < s1.Length; i++)
                    {
                        action(ref s1[i], ref s2[i], ref s3[i], uniform);
                    }

                    // ReSharper disable once AccessToDisposedClosure
                    countdown.Signal();
                }, partition, preferLocal: true);
            }

            /*
            //Optimization: Also process one partition right here on the calling thread.
            var s1 = storage1.AsSpan(0, partitionSize);
            var s2 = storage2.AsSpan(0, partitionSize);
            var s3 = storage3.AsSpan(0, partitionSize);
            for (var i = 0; i < partitionSize; i++)
            {
                action(ref s1[i], ref s2[i], ref s3[i], uniform);
            }
            */
        }

        countdown.Signal();
        world.Unlock();

    }


    public void Run(SpanAction_CCC<C1, C2, C3> action)
    {
        world.Lock();
        foreach (var table in Tables)
        {
            if (table.IsEmpty) continue;
            var s1 = table.GetStorage<C1>(Identity.None).AsSpan(0, table.Count);
            var s2 = table.GetStorage<C2>(Identity.None).AsSpan(0, table.Count);
            var s3 = table.GetStorage<C3>(Identity.None).AsSpan(0, table.Count);
            action(s1, s2, s3);
        }

        world.Unlock();
    }

    public void Raw(Action<Memory<C1>, Memory<C2>, Memory<C3>> action)
    {
        world.Lock();
        foreach (var table in Tables)
        {
            if (table.IsEmpty) continue;
            var m1 = table.GetStorage<C1>(Identity.None).AsMemory(0, table.Count);
            var m2 = table.GetStorage<C2>(Identity.None).AsMemory(0, table.Count);
            var m3 = table.GetStorage<C3>(Identity.None).AsMemory(0, table.Count);
            action(m1, m2, m3);
        }

        world.Unlock();
    }

    public void RawParallel(Action<Memory<C1>, Memory<C2>, Memory<C3>> action)
    {
        world.Lock();

        Parallel.ForEach(Tables, Options,
            table =>
            {
                if (table.IsEmpty) return; //TODO: This wastes a scheduled thread. 
                var m1 = table.GetStorage<C1>(Identity.None).AsMemory(0, table.Count);
                var m2 = table.GetStorage<C2>(Identity.None).AsMemory(0, table.Count);
                var m3 = table.GetStorage<C3>(Identity.None).AsMemory(0, table.Count);
                action(m1, m2, m3);
            });

        world.Unlock();
    }

    #endregion
}