using HexEditor.Model;
using System;

namespace HexEditor.WinUI.Caret;

public interface ICaret
{
	ActiveView ActiveView { get; }
	CaretPosition Position { get; }

	event EventHandler<ActiveViewChangedEventArgs>? ActiveViewChanged;
	event EventHandler<CaretPositionChangedEventArgs>? CaretPositionChanged;

	void ChangeView(ActiveView activeView);
	void MoveDown();
	void MoveLeft();
	void MoveRight();
	void MoveTo(SnapshotPoint point);
	void MoveTo(SnapshotPoint point, ActiveView activeView);
	void MoveToEnd();
	void MoveToHome();
	void MoveToRowEnd();
	void MoveToRowStart();
	void MoveUp();
}