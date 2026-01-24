namespace HexEditor.Core.Diagnostics;

public record class DiagnosticDescriptor(
	string Id,
	DiagnosticSeverity Severity,
	string? Message = null
);
