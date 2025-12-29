namespace HexEditor.ViewModel;

public readonly record struct FormattedSpan(ReadOnlyMemory<byte> Span, object? Style);
