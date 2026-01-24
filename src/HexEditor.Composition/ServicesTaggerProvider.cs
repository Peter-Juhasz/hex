using HexEditor.Core.Tagging;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;

namespace HexEditor.Composition;

public class ServicesTaggerProvider(
	IServiceProvider provider
)
	: ITaggerProvider
{
	public ImmutableArray<ITagger<TTag>> CreateTaggers<TTag>(ImmutableArray<string> contentTypes) where TTag : ITag
	{
		using var builder = new PooledArrayBuilder<ITagger<TTag>>();

		foreach (var taggers in provider.GetServices<ITagger<TTag>>())
		{
			builder.Add(taggers);
		}

		foreach (var contentType in contentTypes)
		{
			foreach (var taggers in provider.GetKeyedServices<ITagger<TTag>>(contentType))
			{
				builder.Add(taggers);
			}
		}

		return builder.ToImmutableArray();
	}
}