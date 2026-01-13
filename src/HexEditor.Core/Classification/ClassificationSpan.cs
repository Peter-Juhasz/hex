using HexEditor.Model;

namespace HexEditor.Classification;

public readonly record struct ClassificationSpan(SnapshotSpan Span, string Type);
