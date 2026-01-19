using System.Collections.Immutable;

namespace HexEditor.Model;

public interface ISnapshotManager
{
    IBinarySnapshot CurrentSnapshot { get; }

    void ApplyChange(BinaryChange change);

    void Undo();

    void Save();
}

public class SnapshotManager : ISnapshotManager
{
    private IBinaryDataSource _dataSource;

    private ImmutableList<BinaryChange> binaryChanges = ImmutableList<BinaryChange>.Empty;

    public SnapshotManager(IBinaryDataSource dataSource)
    {
        this._dataSource = dataSource;
    }

    public IBinarySnapshot CurrentSnapshot => throw new NotImplementedException();

    public void ApplyChange(BinaryChange change)
    {
        throw new NotImplementedException();
    }

    public void Save()
    {
        throw new NotImplementedException();
    }

    public void Undo()
    {
        throw new NotImplementedException();
    }
}