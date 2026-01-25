using HexEditor.Core.ViewModel;
using HexEditor.WinUI.Theming;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace HexEditor.WinUI.Selection;

internal sealed class HexSelectionLayer : Canvas
{
	public HexSelectionLayer(IGraphicalHexView view, VisualTheme theme) : base()
	{
		_view = view;
		_theme = theme;

		this.HorizontalAlignment = HorizontalAlignment.Stretch;
		this.VerticalAlignment = VerticalAlignment.Stretch;
		this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.IBeam);
		this.MinWidth = (theme.Columns * 2) * theme.FontWidth;

		_canvas = this;

		_view.Selection.SelectionChanged += OnSelectionChanged;
	}

	private readonly Canvas _canvas;
	private readonly IGraphicalHexView _view;
	private readonly VisualTheme _theme;

	private Path? _selectionPath;
	private readonly Brush _selectionBackground = new SolidColorBrush(Color.FromArgb(255, 153, 201, 239));

	private void OnSelectionChanged(object? sender, Core.Selection.SelectionChangedEventArgs e)
	{
		if (e.Selection == null)
		{
			if (_selectionPath != null)
			{
				_selectionPath.Visibility = Visibility.Collapsed;
			}
			return;
		}

		if (_selectionPath == null)
		{
			_selectionPath = new Path()
			{
				Data = new PathGeometry()
				{
					Figures = [new PathFigure()
					{
						IsFilled = true,
						IsClosed = true,
					}],
				},
				Fill = _selectionBackground,
				IsHitTestVisible = false,
			};
			Canvas.SetZIndex(_selectionPath, -1);
			_canvas.Children.Add(_selectionPath);
		}

		var points = _view.MapToVisualHex(e.Selection.Span);
		var figure = ((PathGeometry)_selectionPath.Data).Figures[0];
		figure.Fill(points);
		_selectionPath.Visibility = Visibility.Visible;
	}
}
