using HexEditor.Model;
using HexEditor.ViewModel;
using System.Collections.Immutable;

namespace HexEditor.Classification;

public interface IClassifier
{
    ValueTask<ImmutableArray<ClassificationSpan>> GetClassificationsAsync(IViewBuffer buffer, MemorySpan span, CancellationToken cancellationToken);
}
