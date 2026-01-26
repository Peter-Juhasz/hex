using HexEditor.Core.QuickInfo;
using HexEditor.Core.Tagging;
using HexEditor.Model;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace HexEditor.Core.Hyperlinks;

public sealed class UrlToQuickInfoAdapter(ITagger<UrlTag> inner) : ITagger<QuickInfoTag>
{
	public async Task<ImmutableArray<TagSpan<QuickInfoTag>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken)
	{
		var tags = await inner.GetTagsAsync(span, cancellationToken).ConfigureAwait(false);
		var newTags = new TagSpan<QuickInfoTag>[tags.Length];
		for (int i = 0; i < tags.Length; i++)
		{
			var urlTag = tags[i].Tag;
			var quickInfoTag = new TextQuickInfoTag(String.Concat(
				urlTag.Url, Environment.NewLine,
				"Ctrl + Click to follow link"
			));
			newTags[i] = new(tags[i].Span, quickInfoTag);
		}
		return ImmutableCollectionsMarshal.AsImmutableArray(newTags);
	}
}
