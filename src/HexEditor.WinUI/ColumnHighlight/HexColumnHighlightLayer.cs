using HexEditor.Core.Caret;
using HexEditor.Core.ViewModel;
using HexEditor.WinUI.Theming;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;

namespace HexEditor.WinUI.ColumnHighlight;

internal sealed class HexColumnHighlightLayer : Canvas
{
	public HexColumnHighlightLayer(IGraphicalHexView view, VisualTheme theme) : base()
	{
		_view = view;
		_theme = theme;

		this.HorizontalAlignment = HorizontalAlignment.Stretch;
		this.VerticalAlignment = VerticalAlignment.Stretch;
		this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.IBeam);
		this.MinWidth = (theme.Columns * 2) * theme.FontWidth;

		_canvas = this;

		_view.Caret.CaretPositionChanged += OnCaretPositionChanged;
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
		var columnIndex = position.Point.Position % _theme.Columns;
		var primaryGrouping = _theme.HexViewStyle?.PrimaryGrouping ?? 0;
		var secondaryGrouping = _theme.HexViewStyle?.SecondaryGrouping ?? 0;

		var columnLeft = IHexViewRow.GetVisualLeftOfHexColumn((int)columnIndex, _theme.FontWidth, primaryGrouping, secondaryGrouping);
		var columnWidth = _theme.FontWidth * 2;

		if (_columnHighlight == null)
		{
			var style = _theme.HexViewStyle?.ColumnHighlight!;

			_columnHighlight = new Rectangle()
			{
				Fill = style.Background,
				IsHitTestVisible = false,
				Opacity = style.Opacity ?? 1.0,
			};

			if (style.BorderBrush != null && (style.BorderThickness ?? 0) > 0)
			{
				_columnHighlight.Stroke = style.BorderBrush;
				_columnHighlight.StrokeThickness = style.BorderThickness ?? 0;
			}

			_canvas.Children.Add(_columnHighlight);
		}

		Canvas.SetLeft(_columnHighlight, columnLeft);
		Canvas.SetTop(_columnHighlight, 0);
		_columnHighlight.Width = columnWidth;
		_columnHighlight.Height = _view.Viewport.Height;
	}
}
