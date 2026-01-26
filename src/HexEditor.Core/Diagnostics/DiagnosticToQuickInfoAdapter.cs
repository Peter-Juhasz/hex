using HexEditor.Core.QuickInfo;
using HexEditor.Core.Tagging;
using HexEditor.Model;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace HexEditor.Core.Diagnostics;

public sealed class DiagnosticToQuickInfoAdapter(ITagger<DiagnosticTag> inner) : ITagger<QuickInfoTag>
{
	public async Task<ImmutableArray<TagSpan<QuickInfoTag>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken)
	{
		var tags = await inner.GetTagsAsync(span, cancellationToken).ConfigureAwait(false);
		var newTags = new TagSpan<QuickInfoTag>[tags.Length];
		for (int i = 0; i < tags.Length; i++)
		{
			var diagnosticTag = tags[i].Tag;
			var quickInfoTag = new TextQuickInfoTag($"{diagnosticTag.Descriptor.Id}: {diagnosticTag.Descriptor.MessageFormat}");
			newTags[i] = new(tags[i].Span, quickInfoTag);
		}
		return ImmutableCollectionsMarshal.AsImmutableArray(newTags);
	}
}
