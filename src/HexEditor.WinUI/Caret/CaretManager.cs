using HexEditor.Model;
using HexEditor.WinUI.Selection;
using System;

namespace HexEditor.WinUI.Caret;

public class CaretManager : ICaret
{
	public CaretManager(WinUIHexView view)
	{
		_view = view;

		_view.Selection.SelectionChanged += OnSelectionChanged;
	}

	private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		if (e.Selection != null)
		{
			_caretPosition = new CaretPosition(e.Selection.ActivePoint);
			CaretPositionChanged?.Invoke(this, new CaretPositionChangedEventArgs(_caretPosition));
		}
	}

	private readonly WinUIHexView _view;
	private CaretPosition _caretPosition;
	private ActiveView _activeView = ActiveView.Hex;

	public CaretPosition Position => _caretPosition;

	public ActiveView ActiveView => _activeView;

	public event EventHandler<CaretPositionChangedEventArgs>? CaretPositionChanged;
	public event EventHandler<ActiveViewChangedEventArgs>? ActiveViewChanged;

	public void MoveTo(SnapshotPoint point)
	{
		Set(new CaretPosition(point));
	}

	public void MoveTo(SnapshotPoint point, ActiveView activeView)
	{
		Set(new CaretPosition(point));
		ChangeView(activeView);
	}

	public void ChangeView(ActiveView activeView)
	{
		if (_activeView != activeView)
		{
			_activeView = activeView;
			ActiveViewChanged?.Invoke(this, new ActiveViewChangedEventArgs(activeView));
		}
	}

	public void MoveToHome()
	{
		Set(new(_view.Snapshot.Start));
	}

	public void MoveToEnd()
	{
		Set(new(_view.Snapshot.End));
	}

	public void MoveToRowStart()
	{
		var row = _view.GetContainingRow(_caretPosition.Point);
		Set(new CaretPosition(row.Start));
	}

	public void MoveToRowEnd()
	{
		var row = _view.GetContainingRow(_caretPosition.Point);
		Set(new CaretPosition(row.End));
	}

	public void MoveLeft()
	{
		if (_caretPosition.Point.Position > 0)
		{
			Set(new CaretPosition(_caretPosition.Point - 1));
		}
	}

	public void MoveRight()
	{
		if (_caretPosition.Point.Position < _view.Snapshot.Length)
		{
			Set(new CaretPosition(_caretPosition.Point + 1));
		}
	}

	public void MoveUpByRow()
	{
		var currentRow = _view.GetContainingRow(_caretPosition.Point);
		if (currentRow.Start.Position > 0)
		{
			var previousRow = _view.GetContainingRow(currentRow.Start - 1);
			var currentRelativeOffset = _caretPosition.Point.Position - currentRow.Start.Position;
			var newPosition = Math.Min(previousRow.Start.Position + currentRelativeOffset, previousRow.End.Position);
			Set(new CaretPosition(new SnapshotPoint(_view.Snapshot, newPosition)));
		}
	}

	public void MoveDownByRow()
	{
		var currentRow = _view.GetContainingRow(_caretPosition.Point);
		if (currentRow.End.Position < _view.Snapshot.Length)
		{
			var nextRow = _view.GetContainingRow(currentRow.End);
			var currentRelativeOffset = _caretPosition.Point.Position - currentRow.Start.Position;
			var newPosition = Math.Min(nextRow.Start.Position + currentRelativeOffset, nextRow.End.Position);
			Set(new CaretPosition(new SnapshotPoint(_view.Snapshot, newPosition)));
		}
	}

	private void Set(CaretPosition caretPosition)
	{
		_caretPosition = caretPosition;
		CaretPositionChanged?.Invoke(this, new CaretPositionChangedEventArgs(caretPosition));

		_view.Selection.Clear();

		_view.Viewport.BringIntoView(caretPosition.Point);
	}
}
