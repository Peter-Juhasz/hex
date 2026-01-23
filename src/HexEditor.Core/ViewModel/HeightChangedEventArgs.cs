namespace HexEditor.Core.ViewModel;

public class HeightChangedEventArgs(double oldHeight, double newHeight) : EventArgs
{
	public double OldHeight { get; } = oldHeight;
	public double NewHeight { get; } = newHeight;
}