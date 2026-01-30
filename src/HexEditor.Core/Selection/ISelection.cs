using HexEditor.Core.Model;
using HexEditor.Model;
using System;

namespace HexEditor.Core.Selection;

public interface ISelection
{
	SelectionSpan? Span { get; }

	bool IsEmpty => Span is null or { Span.IsEmpty: true };

	event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

	void Select(SnapshotPoint anchorPoint, SnapshotPoint activePoint);
	void Select(SnapshotSpan span, bool isReversed = false);

	void MoveActivePointToPreviousByte();
	void MoveActivePointToNextByte();

	void MoveActivePointUpByRow();
	void MoveActivePointDownByRow();

	void MoveActivePointToHome();
	void MoveActivePointToEnd();

	void MoveActivePointToRowStart();
	void MoveActivePointToRowEnd();

	void MoveActivePointToPreviousColumnGroup();
	void MoveActivePointToNextColumnGroup();

	void SelectTo(SnapshotPoint anchorPoint);

	void SelectAll();

	void Replace(ReadOnlySpan<byte> data);

	void Clear();
}