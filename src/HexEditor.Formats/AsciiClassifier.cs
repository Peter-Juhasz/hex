using HexEditor.Classification;
using HexEditor.Core.ContentType;
using HexEditor.Core.Model;
using HexEditor.Core.Tagging;
using HexEditor.Formats.Text;
using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Formats;

[ContentType(BinaryContentTypeDefinition.Id)]
public sealed class AsciiClassifier : ITagger<ClassificationTag>
{
	public static readonly ClassificationTag NonPrintableTag = new("encoding.ascii.non-printable");
	public static readonly ClassificationTag PrintableTag = new("encoding.ascii.printable");

	private const byte PrintableMin = 0x20; // space
	private const byte PrintableMax = 0x7E; // ~

	public async Task<ImmutableArray<TagSpan<ClassificationTag>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken)
	{
		var tags = ImmutableArray.CreateBuilder<TagSpan<ClassificationTag>>();

		var reader = span.CreateChunkReader(4096);
		while (reader.MoveNext(out var memory))
		{
			cancellationToken.ThrowIfCancellationRequested();

			await span.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);

			var data = memory.Span;
			int i = 0;

			while (i < data.Length)
			{
				// Find the next printable byte.
				int printableStartRelative = data.Slice(i).IndexOfAnyInRange(PrintableMin, PrintableMax);
				if (printableStartRelative < 0)
				{
					// No printable bytes in the remainder -> all non-printable.
					tags.Add(new(span.Slice(reader.Position + i, data.Length - i), NonPrintableTag));
					break;
				}

				int printableStart = i + printableStartRelative;

				// Anything before the first printable is non-printable.
				if (printableStart > i)
				{
					tags.Add(new(span.Slice(reader.Position + i, printableStart - i), NonPrintableTag));
				}

				// Now find where the printable run ends (first byte outside printable range).
				var printableSlice = data.Slice(printableStart);
				int printableEndRel = printableSlice.IndexOfAnyExceptInRange(PrintableMin, PrintableMax);

				int printableLen = (printableEndRel < 0) ? printableSlice.Length : printableEndRel;

				tags.Add(new(span.Slice(reader.Position + printableStart, printableLen), PrintableTag));

				// Continue after the printable run.
				i = printableStart + printableLen;
			}
		}

		return tags.ToImmutableArray();
	}
}
