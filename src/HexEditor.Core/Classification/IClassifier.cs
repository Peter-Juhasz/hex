using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Classification;

public interface IClassifier
{
    ValueTask<ImmutableArray<ClassificationSpan>> GetClassificationsAsync(SnapshotSpan span, CancellationToken cancellationToken);
}
