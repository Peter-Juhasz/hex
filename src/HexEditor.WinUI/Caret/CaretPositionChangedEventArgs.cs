using System;

namespace HexEditor.WinUI.Caret;

public class CaretPositionChangedEventArgs(CaretPosition caretPosition) : EventArgs
{
	public CaretPosition CaretPosition { get; } = caretPosition;
}
