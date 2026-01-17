using HexEditor.Core.Tagging;

namespace HexEditor.Structure;

public record class StructureTag(string? Label = null) : ITag;