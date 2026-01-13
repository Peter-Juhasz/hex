using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Structure;

public interface IStructureProvider
{
    ValueTask<ImmutableArray<StructureSpan>> GetStructureSpansAsync(SnapshotSpan span, CancellationToken cancellationToken);
}