using HexEditor.Composition;
using HexEditor.Core.Actions;
using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Formats.Text;

//[ContentType(TextContentTypeDefinition.Id)]
public sealed class InsertUtf8BomBinaryActionProvider : IBinaryActionProvider
{
	private static readonly byte[] bom = [0xEF, 0xBB, 0xBF];

	public ValueTask<ImmutableArray<BinaryAction>> GetActionsAsync(SnapshotSpan span, CancellationToken cancellationToken)
	{
		if (span.Span.EndOffset != 0)
		{
			return new([]);
		}

		if (span.Snapshot.Length < bom.Length)
		{
			return new([new(
				Title: "Insert UTF-8 BOM", 
				Edit: new([BinaryChange.Insert(0, bom)])
			)]);
		}

		return GetCoreAsync(span, cancellationToken);
	}

	private static async ValueTask<ImmutableArray<BinaryAction>> GetCoreAsync(SnapshotSpan span, CancellationToken cancellationToken)
	{
		byte[] bytes = new byte[bom.Length];
		await span.Snapshot.CopyToAsync(0, bytes, cancellationToken).ConfigureAwait(false);
		if (bom.AsSpan().SequenceEqual(bytes))
		{
			return [];
		}

		return [new(
			Title: "Insert UTF-8 BOM",
			Edit: new([BinaryChange.Insert(0, bom)])
		)];
	}
}