using HexEditor.Core.Caret;
using HexEditor.Core.ViewModel;
using HexEditor.WinUI.Theming;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;

namespace HexEditor.WinUI.ColumnHighlight;

internal sealed class AsciiColumnHighlightLayer : Canvas
{
	public AsciiColumnHighlightLayer(IGraphicalHexView view, VisualTheme theme) : base()
	{
		_view = view;
		_theme = theme;

		this.HorizontalAlignment = HorizontalAlignment.Stretch;
		this.VerticalAlignment = VerticalAlignment.Stretch;
		this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.IBeam);
		this.MinWidth = view.Columns * theme.FontWidth;
		this.IsHitTestVisible = false;

		_canvas = this;

		_view.Caret.PositionChanged += OnCaretPositionChanged;
	}

	private readonly Canvas _canvas;
	private readonly IGraphicalHexView _view;
	private readonly VisualTheme _theme;

	private Rectangle? _columnHighlight;

	private void OnCaretPositionChanged(object? sender, CaretPositionChangedEventArgs e)
	{
		Invalidate(e.CaretPosition);
	}

	private void Invalidate(CaretPosition position)
	{
		var columnIndex = position.Point.Position % _view.Columns;
		var primaryGrouping = _theme.AsciiView?.PrimaryGrouping ?? 0;
		var secondaryGrouping = _theme.AsciiView?.SecondaryGrouping ?? 0;

		var columnLeft = IHexViewRow.GetVisualLeftOfAsciiColumn((int)columnIndex, _theme.FontWidth, primaryGrouping, secondaryGrouping);
		var columnWidth = _theme.FontWidth;

		if (_columnHighlight == null)
		{
			_columnHighlight = new Rectangle()
			{
				IsHitTestVisible = false,
			};

			if ((_theme.AsciiView?.ColumnHighlight ?? _theme.ColumnHighlight) is { } style)
			{
				_columnHighlight.Apply(style);
			}

			_canvas.Children.Add(_columnHighlight);
		}

		Canvas.SetLeft(_columnHighlight, columnLeft);
		Canvas.SetTop(_columnHighlight, 0);
		_columnHighlight.Width = columnWidth;
		_columnHighlight.Height = _view.Viewport.Height;
	}
}
