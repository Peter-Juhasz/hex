using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Core.Model;

public interface ISnapshotManager
{
    IBinarySnapshot? LastSavedSnapshot => throw new NotImplementedException();

	IBinarySnapshot CurrentSnapshot { get; }

    ImmutableArray<BinaryChange> Difference(IBinarySnapshot older, IBinarySnapshot newer) => throw new NotImplementedException();

	IBinarySnapshot Apply(BinaryEdit edit) => throw new NotImplementedException();

	IBinarySnapshot Undo() => throw new NotImplementedException();

	IBinarySnapshot Redo() => throw new NotImplementedException();

	bool TryGetNext(IBinarySnapshot snapshot, out IBinarySnapshot? nextSnapshot) => throw new NotImplementedException();

	Task SaveAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

	Task SaveAsAsync(IBinaryDataSource dataSource, CancellationToken cancellationToken) => throw new NotImplementedException();

    event EventHandler<SnapshotChangedEventArgs>? Changed;
}

public class SnapshotChangedEventArgs(IBinarySnapshot newSnapshot) : EventArgs
{
	public IBinarySnapshot NewSnapshot { get; } = newSnapshot;
}
