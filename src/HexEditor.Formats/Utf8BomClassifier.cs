using HexEditor.Classification;
using HexEditor.Model;
using HexEditor.ViewModel;
using System.Collections.Immutable;

namespace HexEditor.Formats;

public sealed class Utf8BomClassifier : IClassifier
{
    private static readonly ImmutableArray<ClassificationSpan> _utf8BomClassification = [new ClassificationSpan(new LongSpan(0, 3), "encoding.utf8.bom")];

    public ValueTask<ImmutableArray<ClassificationSpan>> GetClassificationsAsync(IViewBuffer buffer, MemorySpan span, CancellationToken cancellationToken)
    {
        if (span.StartOffset < 3 && buffer.DataBuffer.Length >= 3)
        {
            if (buffer.TryRead(new(0, 3), out var data))
            {
                if (data.Span[..3] is [0xEF, 0xBB, 0xBF])
                {
                    return ValueTask.FromResult(_utf8BomClassification);
                }
            }
        }

        return ValueTask.FromResult(ImmutableArray<ClassificationSpan>.Empty);
    }
}
