namespace HexEditor.Core.Diagnostics;

public record class DiagnosticDescriptor(
	string Id,
	string Title,
	DiagnosticSeverity Severity,
	string? MessageFormat = null
);
