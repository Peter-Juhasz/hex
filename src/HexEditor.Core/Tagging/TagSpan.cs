using HexEditor.Model;

namespace HexEditor.Core.Tagging;

public readonly record struct TagSpan<TTag>(SnapshotSpan Span, TTag Tag) where TTag : ITag
{
	public static implicit operator TagSpan(TagSpan<TTag> tagSpan) => new(tagSpan.Span, tagSpan.Tag);
}

public readonly record struct TagSpan(SnapshotSpan Span, ITag Tag);