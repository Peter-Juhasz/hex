using HexEditor.Classification;
using HexEditor.Core.Tagging;
using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Formats;

public sealed class Utf8BomClassifier : ITagger<ClassificationTag>
{
    private static readonly ClassificationTag Tag = new("encoding.utf8.bom");

	public async Task<ImmutableArray<TagSpan<ClassificationTag>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken)
    {
        if (span.Span.StartOffset < 3 && span.Snapshot.Length >= 3)
        {
            var buffer = new byte[3];
            await span.Snapshot.CopyToAsync(0, buffer, cancellationToken);
            if (buffer is [0xEF, 0xBB, 0xBF])
            {
                return [new(span.Snapshot.Slice(0, 3), Tag)];
            }
        }

		return [];
    }
}
