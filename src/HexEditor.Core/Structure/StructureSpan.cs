using HexEditor.Model;

namespace HexEditor.Structure;

public record class StructureSpan(BinarySpan Span, string? Label = null);
