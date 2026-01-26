using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace HexEditor.WinUI.ContentView;

internal static class SquiggleUnderline
{
	public static PathGeometry BuildGeometry(double width, double height, double strokeThickness, double wavelength)
	{
		var half = wavelength / 2.0d;

		var fig = new PathFigure
		{
			StartPoint = new Point(0, height),
			IsClosed = false,
			IsFilled = false,
		};

		bool up = true;
		double x = 0;

		do
		{
			var y = up ? height : 0;

			fig.Segments.Add(new LineSegment
			{
				Point = new Point(x, y)
			});

			up = !up;
			x += half;
		} while (x < width);

		var geo = new PathGeometry();
		geo.Figures.Add(fig);
		return geo;
	}
}

