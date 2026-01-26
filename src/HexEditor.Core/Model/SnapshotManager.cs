using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Core.Model;

public class SnapshotManager : ISnapshotManager
{
    private BinarySnapshot _currentSnapshot;

    public SnapshotManager(IBinaryDataSource dataSource)
    {
        this.DataSource = dataSource;
        this._currentSnapshot = new BinarySnapshot(this, null, null);
    }

    public IBinarySnapshot CurrentSnapshot => _currentSnapshot;

    public IBinaryDataSource DataSource { get; }

    public void ApplyChange(BinaryChange change)
    {
        this._currentSnapshot = new BinarySnapshot(this, this._currentSnapshot, change);
    }

    public void Save()
    {
        throw new NotImplementedException();
    }

    public void Undo()
    {
        throw new NotImplementedException();
    }

    public ValueTask CopyToAsync(BinarySnapshot binarySnapshot, long offset, Memory<byte> destination, CancellationToken cancellationToken)
    {
        if (binarySnapshot == this._currentSnapshot)
        {
            return this.DataSource.CopyToAsync(offset, destination, cancellationToken);
        }
        throw new NotImplementedException();
    }
}

public class BinarySnapshot(SnapshotManager snapshotManager, BinarySnapshot? previous, BinaryChange? Change) : IBinarySnapshot
{
    public IBinaryDataSource Source => snapshotManager.DataSource;

    public long Length { get; } = snapshotManager.DataSource.Length + (Change.HasValue ? Change.Value.LengthIncrease : 0);

    public IBinarySnapshot? Previous => previous;

    public ValueTask CopyToAsync(long offset, Memory<byte> destination, CancellationToken cancellationToken)
    {
        return snapshotManager.CopyToAsync(this, offset, destination, cancellationToken);
    }
}