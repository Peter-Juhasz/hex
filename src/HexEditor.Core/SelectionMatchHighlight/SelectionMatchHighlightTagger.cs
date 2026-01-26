using HexEditor.Core.Tagging;
using HexEditor.Core.ViewModel;
using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Core.SelectionMatchHighlight;

public class SelectionMatchHighlightTagger(IViewAccessor viewAccessor) : ITagger<SelectionMatchHighlightTag>
{
	public async Task<ImmutableArray<TagSpan<SelectionMatchHighlightTag>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken)
	{
		// Get the current selection
		var selectionSpan = viewAccessor.View.Selection.Span;
		if (selectionSpan == null || selectionSpan.Span.IsEmpty)
		{
			return [];
		}

		var selection = selectionSpan.Span;
		
		// Don't try to match if selection is too large (performance consideration)
		if (selection.Length > 1024)
		{
			return [];
		}

		// Read the selected bytes
		var selectionLength = (int)selection.Length;
		var selectionBytes = new byte[selectionLength];
		await selection.CopyToAsync(selectionBytes, cancellationToken).ConfigureAwait(false);

		// Find all matching spans within the requested span
		var matches = new List<TagSpan<SelectionMatchHighlightTag>>();
		
		// Search for matches in the requested span
		var searchStart = span.Span.StartOffset;
		var searchEnd = span.Span.EndOffset;
		
		for (long offset = searchStart; offset <= searchEnd - selectionLength; offset++)
		{
			// Check if we should cancel
			if (cancellationToken.IsCancellationRequested)
			{
				break;
			}

			// Read bytes at current offset
			var currentBytes = new byte[selectionLength];
			var currentSpan = new SnapshotSpan(span.Snapshot, new LongSpan(offset, selectionLength));
			await currentSpan.CopyToAsync(currentBytes, cancellationToken).ConfigureAwait(false);

			// Check if bytes match
			if (currentBytes.AsSpan().SequenceEqual(selectionBytes))
			{
				matches.Add(new TagSpan<SelectionMatchHighlightTag>(currentSpan, SelectionMatchHighlightTag.Instance));
			}
		}

		return [.. matches];
	}
}
