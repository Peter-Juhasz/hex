using HexEditor.Classification;
using HexEditor.Model;
using HexEditor.ViewModel;
using System.Collections.Immutable;

namespace HexEditor.Core.Classification;

public class ClassificationAggregator(
	IReadOnlyCollection<IClassifier> classifiers,
	IViewBuffer viewBuffer
)
{
	// TODO: we need an indexed storage here to avoid scanning the entire set for every request
	private ClassificationSpan[] _computedClassifications = [];

	public bool TryGetClassifications(MemorySpan span, out ImmutableArray<ClassificationSpan> classifications)
	{
		List<ClassificationSpan>? matchedClassifications = null;

		foreach (var classification in _computedClassifications)
		{
			if (span.OverlapsWith(classification.Span))
			{
				matchedClassifications ??= new List<ClassificationSpan>();
				matchedClassifications.Add(classification);
			}
		}

		if (matchedClassifications is null)
		{
			classifications = [];
			return false;
		}

		classifications = matchedClassifications.ToImmutableArray();
		return true;
	}

	public async Task<bool> ClassifyAsync(MemorySpan span, CancellationToken cancellationToken)
	{
		List<ClassificationSpan>? classifications = null;
		foreach (var classifier in classifiers)
		{
			var spans = await classifier.GetClassificationsAsync(viewBuffer, span, cancellationToken);
			if (spans.IsEmpty)
			{
				continue;
			}

			classifications ??= new List<ClassificationSpan>(spans.Length);
			classifications.AddRange(spans);
		}

		if (classifications == null)
		{
			return false;
		}

		var oldArray = _computedClassifications;
		var newArray = new ClassificationSpan[oldArray.Length + classifications.Count];
		oldArray.CopyTo(newArray, 0);
		classifications.CopyTo(newArray, oldArray.Length);
		_computedClassifications = newArray;
		return true;
	}
}
