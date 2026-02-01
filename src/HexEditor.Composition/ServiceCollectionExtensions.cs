using HexEditor.Core.Actions;
using HexEditor.Core.ContentType;
using HexEditor.Core.Syntax;
using HexEditor.Core.Tagging;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace HexEditor.Composition;

public static partial class ServiceCollectionExtensions
{
	extension(IServiceCollection services)
	{
		public void AddHexEditor()
		{
			services.AddSingleton<ITaggerProvider, ServicesTaggerProvider>();
			services.AddSingleton<IContentTypeRegistry, ContentTypeRegistry>();
		}

		public void AddContent(Assembly assembly)
		{
			services.AddContentTypes(assembly);
			services.AddSyntax(assembly);
			services.AddTaggers(assembly);
			services.AddActions(assembly);
		}

		public void AddContentTypes(Assembly assembly)
		{
			var contentTypeDefinitionType = typeof(ContentTypeDefinition);
			foreach (var type in assembly.GetExportedTypes()
				.Where(t => contentTypeDefinitionType.IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
			)
			{
				var instance = (ContentTypeDefinition)Activator.CreateInstance(type)!;
				services.AddSingleton<ContentTypeDefinition>(instance);
				services.AddKeyedSingleton<ContentTypeDefinition>(instance.Type, instance);
			}
		}

		public void AddSyntax(Assembly assembly)
		{
			var factoryServiceType = typeof(IPartialSyntaxTreeFactory);
			var providerServiceType = typeof(IPartialSyntaxTreeProvider);
			foreach (var type in assembly.GetExportedTypes()
				.Where(t => factoryServiceType.IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
			)
			{
				var contentType = type.GetCustomAttribute<ContentTypeAttribute>()!.Type;
				services.AddKeyedSingleton(factoryServiceType, contentType, type);
				services.AddKeyedSingleton(providerServiceType, contentType, (sp, _) => new PartialSyntaxTreeProvider(
					sp.GetRequiredKeyedService<IPartialSyntaxTreeFactory>(contentType)
				));
			}
		}

		public void AddTaggers(Assembly assembly)
		{
			var taggerType = typeof(ITagger<>);
			foreach (var type in assembly.GetExportedTypes()
				.Where(t =>
					!t.IsAbstract && !t.IsInterface &&
					t.GetInterface("ITagger`1")?.GetGenericTypeDefinition().Equals(taggerType) == true
				)
			)
			{
				foreach (var taggerInterface in type.GetInterfaces()
					.Where(i => i.IsGenericType && i.GetGenericTypeDefinition().Equals(taggerType))
				)
				{
					var tagType = taggerInterface.GetGenericArguments()[0];

					foreach (var contentType in type.GetCustomAttributes<ContentTypeAttribute>().Select(a => a.Type).DefaultIfEmpty(null))
					{
						if (contentType == null)
						{
							services.AddSingleton(taggerInterface, type);
						}
						else
						{
							services.AddKeyedSingleton(taggerInterface, contentType, type);
						}
					}
				}
			}
		}

		public void AddActions(Assembly assembly)
		{
			var factoryServiceType = typeof(IBinaryActionProvider);
			foreach (var type in assembly.GetExportedTypes()
				.Where(t => factoryServiceType.IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
			)
			{
				foreach (var contentType in type.GetCustomAttributes<ContentTypeAttribute>().Select(a => a.Type).DefaultIfEmpty(null))
				{
					if (contentType == null)
					{
						services.AddSingleton(factoryServiceType, type);
					}
					else
					{
						services.AddKeyedSingleton(factoryServiceType, contentType, type);
					}
				}
			}
		}

	}
}
