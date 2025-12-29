using HexEditor.Model;

namespace HexEditor.Structure;

public record class StructureSpan(LongSpan Span, string? Label = null);
