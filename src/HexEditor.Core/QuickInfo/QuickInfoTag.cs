using HexEditor.Core.Tagging;

namespace HexEditor.Core.QuickInfo;

public record class QuickInfoTag() : ITag;

public record class TextQuickInfoTag(string Text) : QuickInfoTag;
