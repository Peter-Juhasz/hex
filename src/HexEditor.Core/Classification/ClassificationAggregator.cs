using HexEditor.Classification;
using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Core.Classification;

public class ClassificationAggregator(
	IReadOnlyCollection<IClassifier> classifiers
)
{
	public async Task<ImmutableArray<ClassificationSpan>> ClassifyAsync(SnapshotSpan span, CancellationToken cancellationToken)
	{
		List<ClassificationSpan>? classifications = null;
		foreach (var classifier in classifiers)
		{
			var spans = await classifier.GetClassificationsAsync(span, cancellationToken);
			if (spans.IsEmpty)
			{
				continue;
			}

			classifications ??= new List<ClassificationSpan>(spans.Length);
			classifications.AddRange(spans);
		}

		if (classifications == null)
		{
			return [];
		}

		return classifications.ToImmutableArray();
	}
}
