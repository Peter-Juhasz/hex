using HexEditor.Model;

namespace HexEditor.Core.Tagging;

public readonly record struct TagSpan<TTag>(SnapshotSpan Span, TTag Tag) where TTag : ITag;