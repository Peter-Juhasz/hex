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
		
		// Check if selection is larger than the search span
		var searchStart = span.Span.StartOffset;
		var searchEnd = span.Span.EndOffset;
		if (selectionLength > searchEnd - searchStart)
		{
			return [];
		}

		// Use a buffer-based approach for efficiency
		const int bufferSize = 64 * 1024; // 64KB buffer
		var buffer = new byte[bufferSize + selectionLength - 1];
		
		for (long bufferStart = searchStart; bufferStart < searchEnd; )
		{
			// Check if we should cancel
			if (cancellationToken.IsCancellationRequested)
			{
				break;
			}

			// Calculate how much to read
			var remainingBytes = searchEnd - bufferStart;
			var bytesToRead = (int)Math.Min(buffer.Length, remainingBytes);
			
			// Read a chunk of data
			var bufferSpan = new SnapshotSpan(span.Snapshot, new LongSpan(bufferStart, bytesToRead));
			await bufferSpan.CopyToAsync(buffer.AsMemory(0, bytesToRead), cancellationToken).ConfigureAwait(false);

			// Search for matches in the buffer
			var searchLimit = bytesToRead - selectionLength + 1;
			for (int i = 0; i < searchLimit; i++)
			{
				// Check if bytes match
				if (buffer.AsSpan(i, selectionLength).SequenceEqual(selectionBytes))
				{
					var matchOffset = bufferStart + i;
					var matchSpan = new SnapshotSpan(span.Snapshot, new LongSpan(matchOffset, selectionLength));
					matches.Add(new TagSpan<SelectionMatchHighlightTag>(matchSpan, SelectionMatchHighlightTag.Instance));
				}
			}

			// Move to the next buffer, overlapping by selectionLength-1 to catch matches at boundaries
			bufferStart += bufferSize;
		}

		return [.. matches];
	}
}
