using HexEditor.Model;
using System;

namespace HexEditor.WinUI.Selection;

public class SelectionManager : ISelection
{
	public SelectionManager(WinUIHexView view)
	{
		_view = view;
	}

	private readonly WinUIHexView _view;
	private SelectionSpan? _selection;

	public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

	public SelectionSpan? Span => _selection;

	public void Select(SnapshotPoint anchorPoint, SnapshotPoint activePoint)
	{
		if (anchorPoint == activePoint)
		{
			Clear();
			return;
		}

		if (_selection is SelectionSpan existing)
		{
			if (existing.AnchorPoint == anchorPoint && existing.ActivePoint == activePoint)
			{
				return;
			}
		}

		_selection = new SelectionSpan(this, anchorPoint, activePoint);
		SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(_selection));
	}

	public void Select(SnapshotSpan span, bool isReversed = false)
	{
		if (span.Span.IsEmpty)
		{
			Clear();
			return;
		}

		_selection = isReversed ? new SelectionSpan(this, span.End, span.Start) : new SelectionSpan(this, span.Start, span.End);
		SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(_selection));
	}

	private SnapshotPoint GetAnchorPointOrCaret() => _selection?.AnchorPoint ?? _view.Caret.Position.Point;
	private SnapshotPoint GetActivePointOrCaret() => _selection?.ActivePoint ?? _view.Caret.Position.Point;

	public void SelectTo(SnapshotPoint activePoint)
	{
		Select(GetAnchorPointOrCaret(), activePoint);
	}

	public void MoveActivePointLeft()
	{
		SelectTo(GetActivePointOrCaret() - 1);
	}

	public void MoveActivePointRight()
	{
		SelectTo(GetActivePointOrCaret() + 1);
	}

	public void MoveActivePointToHome()
	{
		SelectTo(_view.Snapshot.Start);
	}

	public void MoveActivePointToEnd()
	{
		SelectTo(_view.Snapshot.End);
	}

	public void MoveActivePointToRowStart()
	{
		var activePoint = GetActivePointOrCaret();
		var currentRow = _view.GetContainingRow(activePoint);
		SelectTo(currentRow.Start);
	}

	public void MoveActivePointToRowEnd()
	{
		var activePoint = GetActivePointOrCaret();
		var currentRow = _view.GetContainingRow(activePoint);
		SelectTo(currentRow.End);
	}

	public void MoveActivePointUpByRow()
	{
		var activePoint = GetActivePointOrCaret();
		var currentRow = _view.GetContainingRow(activePoint);
		if (currentRow.Start.Position > 0)
		{
			var previousRow = _view.GetContainingRow(currentRow.Start - 1);
			var currentRelativeOffset = activePoint.Position - currentRow.Start.Position;
			var newPosition = Math.Min(previousRow.Start.Position + currentRelativeOffset, previousRow.End.Position);
			SelectTo(new SnapshotPoint(_view.Snapshot, newPosition));
		}
	}

	public void MoveActivePointDownByRow()
	{
		var activePoint = GetActivePointOrCaret();
		var currentRow = _view.GetContainingRow(activePoint);
		if (currentRow.End.Position < _view.Snapshot.Length)
		{
			var nextRow = _view.GetContainingRow(currentRow.End);
			var currentRelativeOffset = activePoint.Position - currentRow.Start.Position;
			var newPosition = Math.Min(nextRow.Start.Position + currentRelativeOffset, nextRow.End.Position);
			SelectTo(new SnapshotPoint(_view.Snapshot, newPosition));
		}
	}

	public void SelectAll()
	{
		Select(_view.Snapshot.Span);
	}

	public void Clear()
	{
		if (_selection == null)
		{
			return;
		}

		var oldSelection = _selection;

		_selection = null;
		SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(null));
	}
}
