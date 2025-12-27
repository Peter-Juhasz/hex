using HexEditor.Model;

namespace HexEditor.Classification;

public readonly record struct ClassificationSpan(BinarySpan Span, string Type);
