using HexEditor.Core.Tagging;

namespace HexEditor.Core.ReferenceHighlight;

public record class ReferenceTag() : ITag;

public record class ReferenceDefinitionTag() : ReferenceTag();

public record class ReferenceUsageTag() : ReferenceTag();
