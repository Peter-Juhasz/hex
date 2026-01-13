using HexEditor.Classification;
using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Formats;

public sealed class Utf8BomClassifier : IClassifier
{
    public async ValueTask<ImmutableArray<ClassificationSpan>> GetClassificationsAsync(SnapshotSpan span, CancellationToken cancellationToken)
    {
        if (span.Span.StartOffset < 3 && span.Snapshot.Length >= 3)
        {
            var buffer = new byte[3];
            await span.Snapshot.CopyToAsync(0, buffer, cancellationToken);
            if (buffer is [0xEF, 0xBB, 0xBF])
            {
                return [new(span.Snapshot.Slice(0, 3), "encoding.utf8.bom")];
            }
        }

		return [];
    }
}
