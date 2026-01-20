
namespace HexEditor.Model.Tests;

[TestClass]
public sealed class SnapshotManagerTest
{
    private class ByteArrayDataSource(byte[] data) : IBinaryDataSource
    {
        public long Length => data.Length;

        public ValueTask CopyToAsync(long offset, Memory<byte> destination, CancellationToken cancellationToken)
        {
            data.AsMemory((int)offset, destination.Length).CopyTo(destination);
            return ValueTask.CompletedTask;
        }
    }

    [TestMethod]
    public async Task TestFirstVersion()
    {
        var dataSource = new ByteArrayDataSource([1, 2, 3]);
        var manager = new SnapshotManager(dataSource);

        Assert.AreEqual(3, manager.CurrentSnapshot.Length);

        Assert.IsNotNull(manager.CurrentSnapshot);
        Assert.AreEqual(3, manager.CurrentSnapshot.Length);
        Assert.IsNull(manager.CurrentSnapshot.Previous);

        byte[] buffer = new byte[3];
        await manager.CurrentSnapshot.CopyToAsync(0, buffer, CancellationToken.None);

        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, buffer);
    }
}
