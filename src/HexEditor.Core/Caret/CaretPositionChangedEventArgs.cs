using System;

namespace HexEditor.Core.Caret;

public class CaretPositionChangedEventArgs(CaretPosition caretPosition) : EventArgs
{
	public CaretPosition CaretPosition { get; } = caretPosition;
}
