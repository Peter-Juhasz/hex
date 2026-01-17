using HexEditor.Model;

namespace HexEditor.Structure;

public record class StructureSpan(SnapshotSpan FullExtent, string? Label = null);
