using HexEditor.Model;
using HexEditor.ViewModel;
using System.Collections.Immutable;

namespace HexEditor.Classification;

public interface IClassifier
{
    ValueTask<ImmutableArray<ClassificationSpan>> GetClassificationsAsync(IViewBuffer buffer, BinarySpan span, CancellationToken cancellationToken);
}

public sealed class Utf8BomClassifier : IClassifier
{
    private static readonly ImmutableArray<ClassificationSpan> _utf8BomClassification = [new ClassificationSpan(new BinarySpan(0, 3), "utf8.bom")];

    public ValueTask<ImmutableArray<ClassificationSpan>> GetClassificationsAsync(IViewBuffer buffer, BinarySpan span, CancellationToken cancellationToken)
    {
        if (span.StartOffset == 0 && span.Length >= 3)
        {
            Span<byte> data = stackalloc byte[3];
            if (buffer.TryCopyTo(0, data))
            {
                if (data[..3] is [0xEF, 0xBB, 0xBF])
                {
                    return ValueTask.FromResult(_utf8BomClassification);
                }
            }
        }

        return ValueTask.FromResult(ImmutableArray<ClassificationSpan>.Empty);
    }
}