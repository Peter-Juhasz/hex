using HexEditor.Model;
using HexEditor.ViewModel;

#if DEBUG
if (args is [])
{
	args = [@"E:\roslyn.txt"];
}
#endif

var path = args[0];
var cancelationToken = CancellationToken.None;

using var handle = File.OpenHandle(path);
await using var binaryBuffer = new SafeFileHandleBinaryBuffer(handle);
var viewBuffer = new LazyViewBuffer(binaryBuffer);
var hexView = new ConsoleHexView(viewBuffer);
await hexView.ResizeWindowAsync(Console.WindowWidth, Console.WindowHeight - 1, cancelationToken);
hexView.Render();

while (true)
{
	var key = Console.ReadKey();
	switch (key.Key)
	{
		case ConsoleKey.PageUp:
			await hexView.PageUpAsync(cancelationToken);
			hexView.Render();
			break;

		case ConsoleKey.PageDown:
			await hexView.PageDownAsync(cancelationToken);
			hexView.Render();
			break;

		case ConsoleKey.DownArrow:
			await hexView.ScrollDownAsync(cancelationToken);
			hexView.Render();
			break;

		case ConsoleKey.UpArrow:
			await hexView.ScrollUpAsync(cancelationToken);
			hexView.Render();
			break;

		case ConsoleKey.Escape:
			return;
	}
}