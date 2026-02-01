using HexEditor.Core.Actions;
using HexEditor.Core.Diagnostics;
using HexEditor.Core.Tagging;
using System.Collections.Immutable;

namespace HexEditor.Core.Fixes;

public interface IBinaryFixProvider
{
	ImmutableArray<string> FixableDiagnosticIds { get; }

	ValueTask<ImmutableArray<BinaryAction>> GetFixesAsync(TagSpan<DiagnosticTag> span, CancellationToken cancellationToken);
}