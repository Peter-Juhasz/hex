using HexEditor.Model;

namespace HexEditor.Classification;

public readonly record struct ClassificationSpan(LongSpan Span, string Type);
