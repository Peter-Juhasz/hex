using HexEditor.Core.Hyperlinks;
using HexEditor.Core.Tagging;
using HexEditor.Model;
using System.Buffers;
using System.Collections.Immutable;
using System.Text;

namespace HexEditor.Formats;

public sealed class UrlTagger : ITagger<UrlTag>
{
	private static readonly SearchValues<byte> UriCharacters = SearchValues.Create("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-.+_?#%=&/"u8);
	private static readonly SearchValues<byte> ProtocolLetters = SearchValues.Create("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"u8);

	public async Task<ImmutableArray<TagSpan<UrlTag>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken)
	{
		var data = new byte[span.Span.Length];
		await span.CopyToAsync(data, cancellationToken);

		var builder = new List<TagSpan<UrlTag>>(); // TODO: use array builder
		var dataSpan = data;
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

			var uriSpan = span.Slice(protocolStart, index + 3 + uriLength - protocolStart);
			builder.Add(new(uriSpan, new UrlTag(Encoding.UTF8.GetString(dataSpan.AsSpan(protocolStart, uriLength))))); // TODO: cache strings
		}

		return builder.ToImmutableArray();
	}
}
