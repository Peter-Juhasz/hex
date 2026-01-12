using HexEditor.Model;
using HexEditor.ViewModel;
using System.Collections.Immutable;

namespace HexEditor.Structure;

public interface IStructureProvider
{
    ValueTask<ImmutableArray<StructureSpan>> GetStructureSpansAsync(IViewBuffer buffer, MemorySpan span, CancellationToken cancellationToken);
}