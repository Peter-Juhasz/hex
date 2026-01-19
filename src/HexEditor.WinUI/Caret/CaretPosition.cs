using HexEditor.Model;

namespace HexEditor.WinUI.Caret;

public readonly record struct CaretPosition(SnapshotPoint Point, bool IsHalfByte = false);
