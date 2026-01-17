using HexEditor.Core.Tagging;

namespace HexEditor.Core.Hyperlinks;

public record class UrlTag(string Url) : ITag;