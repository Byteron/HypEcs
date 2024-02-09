namespace fennecs.tests.Integration;
using Position = System.Numerics.Vector3;

public class DocumentationExampleTests
{
    [Fact]
    public void QuickStart_Example_Works()
    {
        using var world = new World();
        var entity1 = world.Spawn().Add<Position>().Id();
        var entity2 = world.Spawn().Add(new Position(1, 2, 3)).Add<int>().Id();

        var query = world.Query<Position>().Build();

        const float MULTIPLIER = 10f;

        query.RunParallel((ref Position pos, float uniform) => { pos *= uniform; }, MULTIPLIER, chunkSize: 2048);

        var pos1 = world.GetComponent<Position>(entity1);
        var expected = new Position() * MULTIPLIER;
        Assert.Equal(expected, pos1);

        var pos2 = world.GetComponent<Position>(entity2);
        expected = new Position(1, 2, 3) * MULTIPLIER;
        Assert.Equal(expected, pos2);
    }

    [Theory]
    [InlineData(20_000, 2_000)]
    [InlineData(20_000, 1_999)]
    [InlineData(20_000, 37)]
    public void Can_Iterate_Multiple_Chunks(int count, int chunkSize)
    {
        using var world = new World();
        for (int i = 0; i < 20_000; i++)
        {
            world.Spawn().Add(new Position(1,2,3)).Id();
        }

        var query = world.Query<Position>().Build();

        query.RunParallel((ref Position pos) => { pos = new Position(2,3,4); }, chunkSize: chunkSize);

        Assert.Equal(count, query.Count);
    }
}