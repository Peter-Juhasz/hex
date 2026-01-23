using HexEditor.Core.Model;

namespace HexEditor.Core.Caret;

public readonly record struct CaretPosition(SnapshotPoint Point, bool IsHalfByte = false);
