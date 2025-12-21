using HexEditor.Model;
using HexEditor.ViewModel;
using System.CommandLine;

#if DEBUG
if (args is [])
{
	args = [@"E:\roslyn.txt", "--interactive"];
}
#endif

using var cts = new CancellationTokenSource();
var cancellationToken = cts.Token;

Console.CancelKeyPress += (sender, eventArgs) =>
{
	eventArgs.Cancel = true;
	cts.Cancel();
};

var root = new RootCommand();
var filePathArgument = new Argument<string>("path");
root.Add(filePathArgument);
var interactiveOption = new Option<bool>("--interactive");
root.Add(interactiveOption);
root.SetAction(async (context, ct) =>
{
	var path = context.GetRequiredValue(filePathArgument);
	var interactive = context.GetValue(interactiveOption);

	using var handle = File.OpenHandle(path);
	await using var binaryBuffer = new SafeFileHandleBinaryBuffer(handle);
	var viewBuffer = new LazyViewBuffer(binaryBuffer);
	var hexView = new ConsoleHexView(viewBuffer);
	await hexView.ResizeWindowAsync(Console.WindowWidth, Console.WindowHeight - 1, ct);
	hexView.Render();

	if (!interactive)
	{
		return;
	}

	while (true)
	{
		var key = Console.ReadKey(intercept: true);
		switch (key.Key)
		{
			case ConsoleKey.PageUp:
				await hexView.PageUpAsync(ct);
				hexView.Render();
				break;

			case ConsoleKey.PageDown:
				await hexView.PageDownAsync(ct);
				hexView.Render();
				break;

			case ConsoleKey.DownArrow:
				await hexView.ScrollDownAsync(ct);
				hexView.Render();
				break;

			case ConsoleKey.UpArrow:
				await hexView.ScrollUpAsync(ct);
				hexView.Render();
				break;

			case ConsoleKey.Escape:
				cts.Cancel();
				return;
		}
	}
});

await root.Parse(args).InvokeAsync(cancellationToken: cancellationToken);
