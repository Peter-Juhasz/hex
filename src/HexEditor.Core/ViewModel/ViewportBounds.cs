using System.Numerics;

namespace HexEditor.Core.ViewModel;

public readonly record struct ViewportBounds(
	double Left,
	double Top,
	double Width,
	double Height
);

public static partial class Extensions
{
	extension(ViewportBounds bounds)
	{
		public double Right => bounds.Left + bounds.Width;

		public double Bottom => bounds.Top + bounds.Height;

		public double X => bounds.Left;

		public double Y => bounds.Top;

		public Vector2 TopLeft => new((float)bounds.Left, (float)bounds.Top);

		public Vector2 TopRight => new((float)bounds.Right, (float)bounds.Top);

		public Vector2 BottomLeft => new((float)bounds.Left, (float)bounds.Bottom);

		public Vector2 BottomRight => new((float)bounds.Right, (float)bounds.Bottom);
	}
}