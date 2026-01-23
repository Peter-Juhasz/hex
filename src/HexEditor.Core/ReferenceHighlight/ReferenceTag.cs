namespace HexEditor.Core.ReferenceHighlight;

public record class ReferenceTag();

public record class ReferenceDefinitionTag() : ReferenceTag();

public record class ReferenceUsageTag() : ReferenceTag();
