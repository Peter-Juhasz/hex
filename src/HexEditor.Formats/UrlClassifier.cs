using HexEditor.Classification;
using HexEditor.Model;
using HexEditor.ViewModel;
using System.Buffers;
using System.Collections.Immutable;

namespace HexEditor.Formats;

public sealed class UrlClassifier : IClassifier
{
	private static readonly SearchValues<byte> UriCharacters = SearchValues.Create("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-.+_?#%=&/"u8);
	private static readonly SearchValues<byte> ProtocolLetters = SearchValues.Create("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"u8);

	public ValueTask<ImmutableArray<ClassificationSpan>> GetClassificationsAsync(IViewBuffer buffer, MemorySpan span, CancellationToken cancellationToken)
	{
		if (!buffer.TryRead(span, out var data))
		{
			return ValueTask.FromResult(ImmutableArray<ClassificationSpan>.Empty);
		}

		using var builder = new PooledArrayBuilder<ClassificationSpan>();
		var dataSpan = data.Span;
		foreach (var index in dataSpan.IndexesOf("://"u8))
		{
			if (index == 0)
			{
				continue;
			}

			var before = dataSpan[0..index];
			var lastNonLetterIndex = before.LastIndexOfAnyExcept(ProtocolLetters);
			if (lastNonLetterIndex == index)
			{
				continue; 
			}

			var protocolStart = lastNonLetterIndex == -1 ? 0 : lastNonLetterIndex + 1;

			var after = dataSpan[(index + 3)..];
			var firstInvalidIndex = after.IndexOfAnyExcept(UriCharacters);
			if (firstInvalidIndex == 0)
			{
				continue;
			}

			var uriLength = firstInvalidIndex == -1 ? after.Length : firstInvalidIndex;

			var uriSpan = new MemorySpan(StartOffset: span.StartOffset + protocolStart, Length: index + 3 + uriLength - protocolStart);
			builder.Add(new(uriSpan, "text.url"));
		}

		return new(builder.ToImmutableArray());
	}
}
