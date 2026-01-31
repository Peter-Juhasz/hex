using HexEditor.Core.Diagnostics;

namespace HexEditor.Formats.Binary;

public static class BinaryDiagnostics
{
	public static readonly DiagnosticDescriptor UnknownChunkHeader = new(
		Id: "BIN0001",
		Title: "Unknown chunk header",
		MessageFormat: "The chunk type '{0}' is not recognized.",
		Severity: DiagnosticSeverity.Error
	);

	public static readonly DiagnosticDescriptor InvalidChunkLength = new(
		Id: "BIN0002",
		Title: "Invalid chunk length",
		MessageFormat: "The chunk length specified is invalid.",
		Severity: DiagnosticSeverity.Warning
	);
}
