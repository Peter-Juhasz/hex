using HexEditor.Core.Actions;
using HexEditor.Core.Diagnostics;
using HexEditor.Core.Tagging;

namespace HexEditor.Core.Fixes;

public record class BinaryFix(
	BinaryAction Action,
	TagSpan<DiagnosticTag> DiagnosticSpan
);