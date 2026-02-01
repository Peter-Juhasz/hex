using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Core.Actions;

public interface IBinaryActionProvider
{
	ValueTask<ImmutableArray<BinaryAction>>	GetActionsAsync(SnapshotSpan span, CancellationToken cancellationToken);
}
