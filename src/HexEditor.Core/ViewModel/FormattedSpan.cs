using HexEditor.Model;

namespace HexEditor.ViewModel;

public readonly record struct FormattedSpan(SnapshotSpan Span, ReadOnlyMemory<byte> Data, object? Style);
