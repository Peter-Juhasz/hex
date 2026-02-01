using HexEditor.Core.Model;
using System;

namespace HexEditor.Core.Caret;

public interface ICaret
{
	ActiveView ActiveView { get; }
	CaretPosition Position { get; }

	event EventHandler<ActiveViewChangedEventArgs>? ActiveViewChanged;
	event EventHandler<CaretPositionChangedEventArgs>? PositionChanged;

	void ChangeView(ActiveView activeView);

	void MoveTo(SnapshotPoint point);
	void MoveTo(SnapshotPoint point, ActiveView activeView);

	void MoveToPreviousByte();
	void MoveToNextByte();

	void MoveUpByRow();
	void MoveDownByRow();

	void MoveToPreviousColumnGroup();
	void MoveToNextColumnGroup();

	void MoveToHome();
	void MoveToEnd();

	void MoveToRowStart();
	void MoveToRowEnd();

	void MoveUpByPage();
	void MoveDownByPage();

	void MoveToPageTop();
	void MoveToPageBottom();
}