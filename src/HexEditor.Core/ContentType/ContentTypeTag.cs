using HexEditor.Core.Tagging;

namespace HexEditor.Core.ContentType;

public record class ContentTypeTag(ContentTypeDefinition ContentType) : ITag;
