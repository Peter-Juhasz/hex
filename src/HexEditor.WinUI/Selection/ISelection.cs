using HexEditor.Model;
using System;

namespace HexEditor.WinUI.Selection;

public interface ISelection
{
	SelectionSpan? Span { get; }

	event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

	void Select(SnapshotPoint anchorPoint, SnapshotPoint activePoint);
	void Select(SnapshotSpan span, bool isReversed = false);
	void MoveActivePointLeft();
	void MoveActivePointRight();
	void MoveActivePointUpByRow();
	void MoveActivePointDownByRow();
	void MoveActivePointToHome();
	void MoveActivePointToEnd();
	void MoveActivePointToRowStart();
	void MoveActivePointToRowEnd();
	void SelectAll();
	void Clear();
	void SelectTo(SnapshotPoint anchorPoint);
}