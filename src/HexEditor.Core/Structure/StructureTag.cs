using HexEditor.Core.Tagging;

namespace HexEditor.Core.Structure;

public record class StructureTag(string? Label = null) : ITag;