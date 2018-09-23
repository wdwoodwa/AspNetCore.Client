﻿using AspNetCore.Client.Generator.CSharp.Http;
using AspNetCore.Client.Generator.Framework;
using AspNetCore.Client.Generator.Framework.AttributeInterfaces;
using AspNetCore.Client.Generator.Framework.Http.Dependencies;
using AspNetCore.Client.Generator.Framework.Http.Headers;
using AspNetCore.Client.Generator.Framework.Http.Parameters;
using AspNetCore.Client.Generator.Framework.Http.RequestModifiers;
using AspNetCore.Client.Generator.Framework.Http.ResponseTypes;
using AspNetCore.Client.Generator.Framework.Http.Routes.Constraints;
using Flurl.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using AspNetCore.Client.Generator.Framework.Http;
using AspNetCore.Client.Generator.Framework.RequestModifiers;
using AspNetCore.Client.Generator.CSharp.SignalR;
using AspNetCore.Client.Generator.Framework.SignalR;

namespace AspNetCore.Client.Generator.Output
{
	public static class ClassWriter
	{
		public static void WriteClientsFile(GenerationContext context)
		{
			var str = WriteFile(context);

			var syntaxTree = CSharpSyntaxTree.ParseText(str, new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.None, SourceCodeKind.Regular));

			str = syntaxTree.GetRoot().NormalizeWhitespace("	", false).ToFullString();

			Helpers.SafelyWriteToFile($"{Environment.CurrentDirectory}/Clients.cs", str);
		}


		public static string WriteFile(GenerationContext context)
		{
			var usings = new List<string>
			{
				@"using AspNetCore.Client;",
				"using AspNetCore.Client.Authorization;",
				"using AspNetCore.Client.Exceptions;",
				"using AspNetCore.Client.Http;",
				"using AspNetCore.Client.RequestModifiers;",
				"using AspNetCore.Client.Serializers;",
				"using Flurl.Http;",
				"using Microsoft.Extensions.DependencyInjection;",
				"using System;",
				"using System.Linq;",
				"using System.Collections.Generic;",
				"using System.Net;",
				"using System.Net.Http;",
				"using System.Runtime.CompilerServices;",
				"using System.Threading;",
				"using System.Threading.Tasks;",
				"using Microsoft.AspNetCore.SignalR.Client;",
				"using Microsoft.AspNetCore.Http.Connections;",
				"using Microsoft.AspNetCore.Http.Connections.Client;",
				"using Microsoft.AspNetCore.SignalR.Protocol;",
				"using Microsoft.Extensions.Logging;",
				"using System.IO;",
				"using System.Threading.Channels;"
			}.Union(context.UsingStatements)
			.Distinct()
			.OrderBy(x => x)
			.ToList();

			return
$@"//------------------------------------------------------------------------------
// <auto-generated>
//		This code was generated from a template.
//		Manual changes to this file may cause unexpected behavior in your application.
//		Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

{string.Join(Environment.NewLine, usings)}

namespace {Settings.ClientNamespace}
{{

{string.Join(Environment.NewLine, context.HttpClients.Select(HttpClassWriter.WriteErrorMessage).NotNull())}
{string.Join(Environment.NewLine, context.HubClients.Select(SignalRClassWriter.WriteErrorMessage).NotNull())}

{HttpClassWriter.WriteInstaller(context)}

{HttpClassWriter.WriteBaseClients()}

{HttpClassWriter.WriteRepositories(context)}

}}


{HttpClassWriter.WriteVersionBlocks(context)}


{SignalRClassWriter.WriteVersionBlocks(context)}
";
		}

		#region SignalR

		public static class SignalRClassWriter
		{
			public static string WriteErrorMessage(HubController controller)
			{
				if (controller.Failed)
				{
					return $@"#warning {(controller.UnexpectedFailure ? "PLEASE MAKE A GITHUB REPO ISSUE" : "")} {controller.Name}Hub {(controller.UnexpectedFailure ? "has failed generation with unexpected error" : "is misconfigured for generation")} :: {controller.Error.Replace('\r', ' ').Replace('\n', ' ')}";
				}
				else
				{
					return null;
				}
			}



			public static string WriteVersionBlocks(GenerationContext context)
			{
				var versions = context.HubClients.Where(x => x.Generated)
					.GroupBy(x => x.NamespaceVersion)
					.ToList();

				return
	$@"

{string.Join(Environment.NewLine, versions.Select(WriteVersionGroup))}

";

			}

			public static string WriteVersionGroup(IGrouping<string, HubController> version)
			{
				return
	$@"
namespace { Settings.HubNamespace }{(version.Key != null ? "." : "")}{version.Key}
{{
{string.Join(Environment.NewLine, version.Select(WriteHub))}
}}
";
			}



			public static string WriteHub(HubController controller)
			{
				return
	$@"
{(controller.NamespaceSuffix != null ? $@"namespace {controller.NamespaceSuffix}
{{" : string.Empty)}

	{WriteConnectionBuilder(controller)}

	{WriteConnection(controller)}

{(controller.NamespaceSuffix != null ? $@"}}" : string.Empty)}
";
			}

			private static string WriteConnectionBuilder(HubController controller)
			{
				return
	$@"
{SharedWriter.GetObsolete(controller)}
public class {controller.Name}HubConnectionBuilder : HubConnectionBuilder
{{
	private bool _hubConnectionBuilt;

	public {controller.Name}HubConnectionBuilder(Uri host, HttpTransportType? transports = null, Action<HttpConnectionOptions> configureHttpConnection = null) : base()
	{{
		//Remove default HubConnection to use custom one
		Services.Remove(Services.Where(x => x.ServiceType == typeof(HubConnection)).Single());
		Services.AddSingleton<{controller.Name}HubConnection>();

		Services.Configure<HttpConnectionOptions>(o =>
		{{
			o.Url = new Uri(host,""{controller.Route}"");
			if (transports != null)
			{{
				o.Transports = transports.Value;
			}}
		}});

		if (configureHttpConnection != null)
		{{
			Services.Configure(configureHttpConnection);
		}}

		Services.AddSingleton<IConnectionFactory, HttpConnectionFactory>();
	}}


	public new {controller.Name}HubConnection Build()
	{{
		// Build can only be used once
		if (_hubConnectionBuilt)
		{{
			throw new InvalidOperationException(""HubConnectionBuilder allows creation only of a single instance of HubConnection."");
		}}

		_hubConnectionBuilt = true;

		// The service provider is disposed by the HubConnection
		var serviceProvider = Services.BuildServiceProvider();

		var connectionFactory = serviceProvider.GetService<IConnectionFactory>();
		if (connectionFactory == null)
		{{
			throw new InvalidOperationException($""Cannot create {{nameof(HubConnection)}} instance.An {{nameof(IConnectionFactory)}} was not configured."");
		}}

		return serviceProvider.GetService<{controller.Name}HubConnection>();
	}}
}}
";
			}

			private static string WriteConnection(HubController controller)
			{
				return $@"
{SharedWriter.GetObsolete(controller)}
public class {controller.Name}HubConnection : HubConnection
{{

	public {controller.Name}HubConnection(IConnectionFactory connectionFactory,
		IHubProtocol protocol,
		IServiceProvider serviceProvider,
		ILoggerFactory loggerFactory)
		: base(connectionFactory, protocol, serviceProvider, loggerFactory) {{ }}


	public {controller.Name}HubConnection(IConnectionFactory connectionFactory,
		IHubProtocol protocol,
		ILoggerFactory loggerFactory)
		: base(connectionFactory, protocol, loggerFactory) {{ }}


	{string.Join(Environment.NewLine, controller.GetEndpoints().Select(WriteEndpoint))}
	{string.Join(Environment.NewLine, controller.GetMessages().Select(WriteMessage))}
}}
";
			}

			private static string WriteEndpoint(HubEndpoint endpoint)
			{
				var parameters = endpoint.GetParameters().NotOfType<IParameter, CancellationTokenModifier>().Select(x => x.Name);
				var cancellationToken = endpoint.GetParameters().OfType<CancellationTokenModifier>().Select(x => x.Name).SingleOrDefault();

				string parameterText = null;
				if (parameters.Any())
				{
					parameterText = $"new object[]{{{string.Join(", ", parameters)}}}, ";
				}
				else
				{

					parameterText = $"null, ";
				}

				if (endpoint.Channel)
				{
					return $@"
{SharedWriter.GetObsolete(endpoint)}
public Task<ChannelReader<{endpoint.ChannelType}>> Stream{endpoint.Name}Async({string.Join(", ", endpoint.GetParameters().Select(SharedWriter.GetParameter))})
{{
	return this.StreamAsChannelCoreAsync<{endpoint.ChannelType}>(""{endpoint.Name}"", {parameterText}{cancellationToken});
}}

{SharedWriter.GetObsolete(endpoint)}
public async Task<IEnumerable<{endpoint.ChannelType}>> Read{endpoint.Name}BlockingAsync({string.Join(", ", endpoint.GetParameters().Select(SharedWriter.GetParameter))})
{{
	var channel = await this.StreamAsChannelCoreAsync<{endpoint.ChannelType}>(""{endpoint.Name}"", {parameterText}{cancellationToken});
	IList<{endpoint.ChannelType}> items = new List<{endpoint.ChannelType}>();
	while(await channel.WaitToReadAsync())
	{{
		while (channel.TryRead(out var item))
		{{
			items.Add(item);
		}}
	}}
	return items;
}}
";
				}
				else
				{
					return $@"
{SharedWriter.GetObsolete(endpoint)}
public Task {endpoint.Name}Async({string.Join(", ", endpoint.GetParameters().Select(SharedWriter.GetParameter))})
{{
	return this.InvokeCoreAsync(""{endpoint.Name}"", {parameterText}{cancellationToken});
}}
";
				}

			}

			private static string WriteMessage(Message message)
			{
				return $@"
public IDisposable On{message.Name}(Action<{string.Join(",", message.Types)}> action)
{{
	return this.On(""{message.Name}"", action);
}}
";
			}
		}




		#endregion SignalR

		#region HTTP
		public static class HttpClassWriter
		{
			public static string WriteErrorMessage(HttpController controller)
			{
				if (controller.Failed)
				{
					return $@"#warning {(controller.UnexpectedFailure ? "PLEASE MAKE A GITHUB REPO ISSUE" : "")} {controller.Name}Controller {(controller.UnexpectedFailure ? "has failed generation with unexpected error" : "is misconfigured for generation")} :: {controller.Error.Replace('\r', ' ').Replace('\n', ' ')}";
				}
				else
				{
					return null;
				}
			}

			public static string GetRelativePath(string file)
			{
				var root = Path.GetFullPath($"{Environment.CurrentDirectory}/{Settings.RouteToServiceProjectFolder}");
				var fullFile = Path.GetFullPath(file);

				return fullFile.Replace(root, "").Trim('\\');
			}

			#region Installer

			public static string WriteInstaller(GenerationContext context)
			{
				var versions = context.HttpClients.Where(x => x.Generated)
					.GroupBy(x => x.NamespaceVersion)
					.Select(x => x.Key)
					.ToList();

				return
	$@"
	public static class {Settings.ClientInterfaceName}Installer
	{{
		/// <summary>
		/// Register the autogenerated clients into the container with a lifecycle of scoped.
		/// </summary>
		/// <param name=""services""></param>
		/// <param name=""configure"">Overrides for client configuration</param>
		/// <returns></returns>
		public static {nameof(IServiceCollection)} Add{Settings.RegisterName}Clients(this {nameof(IServiceCollection)} services, Action<{nameof(ClientConfiguration)}> configure)
		{{
			var configuration = new {nameof(ClientConfiguration)}();

			configuration.{nameof(ClientConfiguration.RegisterClientWrapperCreator)}<I{Settings.ClientInterfaceName}>({Settings.ClientInterfaceName}Wrapper.Create);
			configuration.{nameof(ClientConfiguration.UseClientWrapper)}<I{Settings.ClientInterfaceName}Wrapper, {Settings.ClientInterfaceName}Wrapper>((provider) => new {Settings.ClientInterfaceName}Wrapper(provider.GetService<Func<I{Settings.ClientInterfaceName}, {nameof(IFlurlClient)}>>(), configuration.{nameof(ClientConfiguration.GetSettings)}(), provider));

			configure?.Invoke(configuration);

{string.Join(Environment.NewLine, versions.Select(WriteRepositoryRegistration))}
{string.Join(Environment.NewLine, context.HttpClients.Where(x => x.Generated).Select(WriteClientRegistration))}

			return configuration.{nameof(ClientConfiguration.ApplyConfiguration)}<I{Settings.ClientInterfaceName}>(services);
		}}
	}}
";
			}

			public static string WriteRepositoryRegistration(string version)
			{
				return $@"			services.AddScoped<I{Settings.ClientInterfaceName}{version}Repository,{Settings.ClientInterfaceName}{version}Repository>();";
			}

			public static string WriteClientRegistration(HttpController controller)
			{
				string namespaceVersion = $@"{(controller.NamespaceVersion != null ? $"{controller.NamespaceVersion}." : "")}{(controller.NamespaceSuffix != null ? $"{controller.NamespaceSuffix}." : string.Empty)}";
				string interfaceName = $@"{namespaceVersion}I{controller.ClientName}";
				string implementationName = $@"{namespaceVersion}{controller.ClientName}";
				return $@"			services.AddScoped<{interfaceName}, {implementationName}>();";
			}


			#endregion Installer


			public static string WriteBaseClients()
			{
				return
	$@"
public interface I{Settings.ClientInterfaceName}Wrapper : IClientWrapper {{ }}

public class {Settings.ClientInterfaceName}Wrapper :  I{Settings.ClientInterfaceName}Wrapper
{{
	public TimeSpan Timeout {{ get; internal set; }}
	public {nameof(IFlurlClient)} {Constants.FlurlClientVariable} {{ get; internal set; }}

	public {Settings.ClientInterfaceName}Wrapper(Func<I{Settings.ClientInterfaceName},{nameof(IFlurlClient)}> client, {nameof(ClientSettings)} settings, {nameof(IServiceProvider)} provider)
	{{
		{Constants.FlurlClientVariable} = client(null);
		if (settings.{nameof(ClientSettings.BaseAddress)} != null)
		{{
			{Constants.FlurlClientVariable}.BaseUrl = settings.{nameof(ClientSettings.BaseAddress)}(provider);
		}}

		Timeout = settings.{nameof(ClientSettings.Timeout)};
	}}

	public static I{Settings.ClientInterfaceName}Wrapper Create(Func<I{Settings.ClientInterfaceName},{nameof(IFlurlClient)}> client, {nameof(ClientSettings)} settings, {nameof(IServiceProvider)} provider)
	{{
		return new {Settings.ClientInterfaceName}Wrapper(client, settings, provider);
	}}
}}

public interface I{Settings.ClientInterfaceName} : {nameof(IClient)} {{ }}
";
			}


			#region Repository

			public static string WriteRepositories(GenerationContext context)
			{
				var versions = context.HttpClients.Where(x => x.Generated)
					.GroupBy(x => x.NamespaceVersion)
					.ToList();


				return
	$@"

{string.Join(Environment.NewLine, versions.Select(WriteRepository))}

";
			}

			public static string WriteRepository(IGrouping<string, HttpController> version)
			{


				return
	$@"
public interface I{Settings.ClientInterfaceName}{version.Key}Repository
{{
{string.Join($@"{Environment.NewLine}", version.Select(x => WriteRepositoryInterfaceProperty(version.Key, x)))}
}}

{(Settings.UseInternalClients ? "internal" : "public")} class {Settings.ClientInterfaceName}{version.Key}Repository : I{Settings.ClientInterfaceName}{version.Key}Repository
{{
{string.Join($@"{Environment.NewLine}", version.Select(x => WriteRepositoryProperty(version.Key, x)))}

	public {Settings.ClientInterfaceName}{version.Key}Repository
	(
{string.Join($@",{Environment.NewLine}", version.Select(x => WriteRepositoryParameter(version.Key, x)))}
	)
	{{
{string.Join($@"{Environment.NewLine}", version.Select(x => WriteRepositoryAssignment(version.Key, x)))}
	}}
}}

";
			}


			public static string WriteRepositoryInterfaceProperty(string key, HttpController controller)
			{
				return $@"{key}{(key != null ? "." : "")}{(controller.NamespaceSuffix != null ? $"{controller.NamespaceSuffix}." : string.Empty)}I{controller.ClientName} {controller.Name} {{ get; }}";
			}

			public static string WriteRepositoryProperty(string key, HttpController controller)
			{
				return $@"public {key}{(key != null ? "." : "")}{(controller.NamespaceSuffix != null ? $"{controller.NamespaceSuffix}." : string.Empty)}I{controller.ClientName} {controller.Name} {{ get; }}";
			}

			public static string WriteRepositoryParameter(string key, HttpController controller)
			{
				return $@"{key}{(key != null ? "." : "")}{(controller.NamespaceSuffix != null ? $"{controller.NamespaceSuffix}." : string.Empty)}I{controller.ClientName} param_{controller.Name.ToLower()}";
			}

			public static string WriteRepositoryAssignment(string key, HttpController controller)
			{
				return $@"this.{controller.Name} = param_{controller.Name.ToLower()};";
			}


			#endregion Repository

			#region Version Blocks

			public static string WriteVersionBlocks(GenerationContext context)
			{
				var versions = context.HttpClients.Where(x => x.Generated)
					.GroupBy(x => x.NamespaceVersion)
					.ToList();

				return
	$@"

{string.Join(Environment.NewLine, versions.Select(WriteVersionGroup))}

";

			}

			public static string WriteVersionGroup(IGrouping<string, HttpController> version)
			{
				return
	$@"
namespace { Settings.ClientNamespace }{(version.Key != null ? "." : "")}{version.Key}
{{
{string.Join(Environment.NewLine, version.Select(WriteController))}
}}
";
			}

			#endregion Version Blocks


			#region Class


			public static string WriteController(HttpController controller)
			{
				return
	$@"
{(controller.NamespaceSuffix != null ? $@"namespace {controller.NamespaceSuffix}
{{" : string.Empty)}

{WriteClassInterface(controller)}

{WriteClassImplementation(controller)}

{(controller.NamespaceSuffix != null ? $@"}}" : string.Empty)}
";
			}

			public static string WriteClassInterface(HttpController controller)
			{
				return
	$@"
{SharedWriter.GetObsolete(controller)}
public interface I{controller.ClientName} : I{Settings.ClientInterfaceName}
{{
{string.Join($"{Environment.NewLine}", controller.GetEndpoints().Select(WriteEndpointInterface))}
}}
";
			}

			public static string WriteClassImplementation(HttpController controller)
			{
				var dependencies = controller.GetInjectionDependencies().ToList();
				dependencies.Insert(0, new ClientDependency($"I{Settings.ClientInterfaceName}Wrapper"));

				return
	$@"
{SharedWriter.GetObsolete(controller)}
{(Settings.UseInternalClients ? "internal" : "public")} class {controller.ClientName} : I{controller.ClientName}
{{
{string.Join($"{Environment.NewLine}", dependencies.Select(WriteDependenciesField))}

	public {controller.ClientName}(
{string.Join($",{Environment.NewLine}", dependencies.Select(WriteDependenciesParameter))})
	{{
{string.Join($"{Environment.NewLine}", dependencies.Select(WriteDependenciesAssignment))}
	}}

{string.Join($"{Environment.NewLine}", controller.GetEndpoints().Select(x => WriteEndpointImplementation(controller, x)))}
}}
";
			}

			#endregion Class

			#region Dependencies

			public static string WriteDependenciesField(IDependency dependency)
			{
				return $@"protected readonly {dependency.GetDependencyFieldType($"I{Settings.ClientInterfaceName}")} {dependency.GetDependencyName($"I{Settings.ClientInterfaceName}")};";
			}

			public static string WriteDependenciesParameter(IDependency dependency)
			{
				return $@"{dependency.GetDependencyParameterType($"I{Settings.ClientInterfaceName}")} param_{dependency.GetDependencyName($"I{Settings.ClientInterfaceName}").ToLower()}";
			}

			public static string WriteDependenciesAssignment(IDependency dependency)
			{
				string assignmentValue = $"param_{dependency.GetDependencyName($"I{Settings.ClientInterfaceName}").ToLower()}";

				if (dependency.HasAssignmentOverride)
				{
					return $@"{dependency.GetDependencyName($"I{Settings.ClientInterfaceName}")} = {dependency.GetAssignmentOverride(assignmentValue)};";
				}
				else
				{
					return $@"{dependency.GetDependencyName($"I{Settings.ClientInterfaceName}")} = {assignmentValue};";
				}

			}

			#endregion Dependencies


			#region Endpoint

			public static string WriteEndpointInterface(HttpEndpoint endpoint)
			{
				return
	$@"
{SharedWriter.GetObsolete(endpoint)}
{GetInterfaceReturnType(endpoint.ReturnType, false)} {endpoint.Name}
(
{string.Join($",{Environment.NewLine}", endpoint.GetParameters().Select(SharedWriter.GetParameter))}
);

{SharedWriter.GetObsolete(endpoint)}
{GetInterfaceReturnType(nameof(HttpResponseMessage), false)} {endpoint.Name}Raw
(
{string.Join($",{Environment.NewLine}", endpoint.GetParametersWithoutResponseTypes().Select(SharedWriter.GetParameter))}
);

{SharedWriter.GetObsolete(endpoint)}
{GetInterfaceReturnType(endpoint.ReturnType, true)} {endpoint.Name}Async
(
{string.Join($",{Environment.NewLine}", endpoint.GetParameters().Select(SharedWriter.GetParameter))}
);

{SharedWriter.GetObsolete(endpoint)}
{GetInterfaceReturnType(nameof(HttpResponseMessage), true)} {endpoint.Name}RawAsync
(
{string.Join($",{Environment.NewLine}", endpoint.GetParametersWithoutResponseTypes().Select(SharedWriter.GetParameter))}
);

";
			}

			public static string WriteEndpointImplementation(HttpController controller, HttpEndpoint endpoint)
			{
				return
	$@"

{SharedWriter.GetObsolete(endpoint)}
public {GetImplementationReturnType(endpoint.ReturnType, false)} {endpoint.Name}
(
{string.Join($",{Environment.NewLine}", endpoint.GetParameters().Select(SharedWriter.GetParameter))}
)
{{
{GetMethodDetails(controller, endpoint, false, false)}
}}

{SharedWriter.GetObsolete(endpoint)}
public {GetImplementationReturnType(nameof(HttpResponseMessage), false)} {endpoint.Name}Raw
(
{string.Join($",{Environment.NewLine}", endpoint.GetParametersWithoutResponseTypes().Select(SharedWriter.GetParameter))}
)
{{
{GetMethodDetails(controller, endpoint, false, true)}
}}

{SharedWriter.GetObsolete(endpoint)}
public {GetImplementationReturnType(endpoint.ReturnType, true)} {endpoint.Name}Async
(
{string.Join($",{Environment.NewLine}", endpoint.GetParameters().Select(SharedWriter.GetParameter))}
)
{{
{GetMethodDetails(controller, endpoint, true, false)}
}}

{SharedWriter.GetObsolete(endpoint)}
public {GetImplementationReturnType(nameof(HttpResponseMessage), true)} {endpoint.Name}RawAsync
(
{string.Join($",{Environment.NewLine}", endpoint.GetParametersWithoutResponseTypes().Select(SharedWriter.GetParameter))}
)
{{
{GetMethodDetails(controller, endpoint, true, true)}
}}

";
			}


			public static string GetMethodDetails(HttpController controller, HttpEndpoint endpoint, bool async, bool raw)
			{
				var cancellationToken = endpoint.GetRequestModifiers().OfType<CancellationTokenModifier>().SingleOrDefault();
				var clientDependency = new ClientDependency($"I{Settings.ClientInterfaceName}Wrapper");

				var requestModifiers = endpoint.GetRequestModifiers().ToList();

				var bodyParameter = endpoint.GetBodyParameter();
				string bodyVariable = bodyParameter?.Name ?? "null";

				var responseTypes = endpoint.GetResponseTypes();

				var routeConstraints = endpoint.GetRouteConstraints(controller);

				return
	$@"{string.Join(Environment.NewLine, routeConstraints.Select(WriteRouteConstraint).NotNull())}
{GetEndpointInfoVariables(controller, endpoint)}
string url = $@""{GetRoute(controller, endpoint)}"";
HttpResponseMessage response = null;
response = {GetAwait(async)}HttpOverride.GetResponseAsync({GetHttpMethod(endpoint.HttpType)}, url, null, {cancellationToken.Name}){GetAsyncEnding(async)};

if(response == null)
{{
	try
	{{
		response = {GetAwait(async)}{clientDependency.GetDependencyName($"I{Settings.ClientInterfaceName}")}.{nameof(IClientWrapper.ClientWrapper)}
					.Request(url)
{string.Join($"				{Environment.NewLine}", requestModifiers.Select(WriteRequestModifiers).NotNull())}
					.AllowAnyHttpStatus()
					{GetHttpMethod(endpoint)}
					{GetAsyncEnding(async)};
	}}
	catch({nameof(FlurlHttpException)} fhex)
	{{
{WriteResponseType(responseTypes.OfType<ExceptionResponseType>().Single(), async, false)}
{WriteErrorActionResultReturn(endpoint, async, raw)}
	}}

	{GetAwait(async)}HttpOverride.OnNonOverridedResponseAsync({GetHttpMethod(endpoint.HttpType)}, url, {bodyVariable}, response, {cancellationToken.Name}){GetAsyncEnding(async)};
}}
{string.Join(Environment.NewLine, responseTypes.Where(x => x.GetType() != typeof(ExceptionResponseType)).Select(x => WriteResponseType(x, async, raw)).NotNull())}
{WriteActionResultReturn(endpoint, async, raw)}";
			}

			public static string GetHttpMethod(HttpMethod method)
			{
				if (method.Method == HttpMethod.Delete.Method)
				{
					return $"{nameof(HttpMethod)}.{nameof(HttpMethod.Delete)}";
				}
				else if (method.Method == HttpMethod.Get.Method)
				{
					return $"{nameof(HttpMethod)}.{nameof(HttpMethod.Get)}";
				}
				else if (method.Method == HttpMethod.Put.Method)
				{
					return $"{nameof(HttpMethod)}.{nameof(HttpMethod.Put)}";
				}
				else if (method.Method == new HttpMethod("PATCH").Method)
				{
					return $@"new {nameof(HttpMethod)}(""PATCH"")";
				}
				else if (method.Method == HttpMethod.Post.Method)
				{
					return $"{nameof(HttpMethod)}.{nameof(HttpMethod.Post)}";
				}
				else
				{
					return $"#error Unsupported HttpMethod of {method.Method}";
				}
			}

			public static string WriteRouteConstraint(RouteConstraint constraint)
			{
				if (constraint is AlphaConstraint)
				{
					return $@"
if(string.IsNullOrWhiteSpace({constraint.ParameterName}) || {constraint.ParameterName}.Any(x=>char.IsNumber(x)))
{{
	throw new InvalidRouteException(""Parameter {constraint.ParameterName} must only contain characters that are not numbers."");
}}";
				}
				else if (constraint is BoolConstraint)
				{
					return $@"
if(!bool.TryParse({constraint.ParameterName}.ToString(),out _))
{{
	throw new InvalidRouteException(""Parameter {constraint.ParameterName} does not parse into an bool."");
}}";
				}
				else if (constraint is DateTimeConstraint)
				{
					return $@"
if(!DateTime.TryParse({constraint.ParameterName}.ToString(),out _))
{{
	throw new InvalidRouteException(""Parameter {constraint.ParameterName} does not parse into an DateTime."");
}}";
				}
				else if (constraint is DecimalConstraint)
				{
					return $@"
if(!decimal.TryParse({constraint.ParameterName}.ToString(),out _))
{{
	throw new InvalidRouteException(""Parameter {constraint.ParameterName} does not parse into an decimal."");
}}";
				}
				else if (constraint is FloatConstraint)
				{
					return $@"
if(!float.TryParse({constraint.ParameterName}.ToString(),out _))
{{
	throw new InvalidRouteException(""Parameter {constraint.ParameterName} does not parse into an float."");
}}";
				}
				else if (constraint is GuidConstraint)
				{
					return $@"
if(!Guid.TryParse({constraint.ParameterName}.ToString(),out _))
{{
	throw new InvalidRouteException(""Parameter {constraint.ParameterName} does not parse into an Guid."");
}}";
				}
				else if (constraint is IntConstraint)
				{
					return $@"
if(!int.TryParse({constraint.ParameterName}.ToString(),out _))
{{
	throw new InvalidRouteException(""Parameter {constraint.ParameterName} does not parse into an int."");
}}";
				}
				else if (constraint is LengthConstraint)
				{
					var value = constraint.GetConstraintValue();
					if (value.Contains(','))
					{
						var split = value.Split(',');
						string minL = split[0];
						string maxL = split[1];

						return $@"
if({constraint.ParameterName}.Length <= {minL} || {constraint.ParameterName}.Length >= {maxL})
{{
	throw new InvalidRouteException(""Parameter {constraint.ParameterName} has a length that is not between {minL} and {maxL}."");
}}";
					}
					else
					{
						return $@"
if({constraint.ParameterName}.Length == {value})
{{
	throw new InvalidRouteException(""Parameter {constraint.ParameterName} has a length that is not {value}."");
}}";
					}
				}
				else if (constraint is LongConstraint)
				{
					return $@"
if(!long.TryParse({constraint.ParameterName}.ToString(),out _))
{{
	throw new InvalidRouteException(""Parameter {constraint.ParameterName} does not parse into an long."");
}}";
				}
				else if (constraint is MaxConstraint)
				{
					var value = constraint.GetConstraintValue();

					return $@"
if({constraint.ParameterName} >= {value})
{{
	throw new InvalidRouteException(""Parameter {constraint.ParameterName} has a value more than {value}."");
}}";
				}
				else if (constraint is MaxLengthConstraint)
				{
					var value = constraint.GetConstraintValue();

					return $@"
if({constraint.ParameterName}.Length >= {value})
{{
	throw new InvalidRouteException(""Parameter {constraint.ParameterName} has a length greater than {value}."");
}}";
				}
				else if (constraint is MinConstraint)
				{
					var value = constraint.GetConstraintValue();

					return $@"
if({constraint.ParameterName} <= {value})
{{
	throw new InvalidRouteException(""Parameter {constraint.ParameterName} has a value less than {value}."");
}}";
				}
				else if (constraint is MinLengthConstraint)
				{
					var value = constraint.GetConstraintValue();

					return $@"
if({constraint.ParameterName}.Length <= {value})
{{
	throw new InvalidRouteException(""Parameter {constraint.ParameterName} has a length less than {value}."");
}}";
				}
				else if (constraint is RangeConstraint)
				{
					var value = constraint.GetConstraintValue();

					var split = value.Split(',');
					string minL = split[0];
					string maxL = split[1];

					return $@"
if({constraint.ParameterName} <= {minL} || {constraint.ParameterName} >= {maxL})
{{
	throw new InvalidRouteException(""Parameter {constraint.ParameterName} has a value that is not between {minL} and {maxL}."");
}}";
				}
				else if (constraint is RegexConstraint)
				{
					var value = constraint.GetConstraintValue();

					return $@"
if(!(new Regex(@""{value}"").IsMatch({constraint.ParameterName})))
{{
	throw new InvalidRouteException(""Parameter {constraint.ParameterName} does not follow the regex \""{value}\""."");
}}";
				}
				else if (constraint is RequiredConstraint)
				{
					return null;
				}
				else
				{
					return $@"#error A route constraint of type {constraint.GetType().Name} and text of {constraint.Constraint} is not supported";
				}
			}


			public static string WriteErrorActionResultReturn(HttpEndpoint endpoint, bool async, bool raw)
			{
				if (raw)
				{
					return @"return null;";
				}

				if (endpoint.ReturnType != null)
				{
					return $@"return default({endpoint.ReturnType});";
				}

				return "return;";
			}

			public static string WriteActionResultReturn(HttpEndpoint endpoint, bool async, bool raw)
			{
				if (raw)
				{
					return @"return response;";
				}

				if (endpoint.ReturnsStream)
				{
					return
$@"
if(response.IsSuccessStatusCode)
{{
	return {GetAwait(async)}response.Content.ReadAsStreamAsync(){GetAsyncEnding(async)};
}}
else
{{
	return default({endpoint.ReturnType});
}}
";
				}
				else
				{
					if (endpoint.ReturnType != null)
					{
						return
		$@"
if(response.IsSuccessStatusCode)
{{
	return {GetAwait(async)}Serializer.Deserialize<{endpoint.ReturnType}>(response.Content){GetAsyncEnding(async)};
}}
else
{{
	return default({endpoint.ReturnType});
}}
";
					}
				}



				return "return;";
			}

			public static string WriteResponseType(ResponseType responseType, bool async, bool raw)
			{
				if (raw)
				{
					return null;
				}

				return
	$@"
{GetResponseTypeCheck(responseType)}
{GetResponseTypeInvoke(responseType, async)}
";
			}

			public static string GetResponseTypeCheck(ResponseType responseType)
			{
				return
	$@"
if({responseType.Name} != null && {responseType.Name}.Method.IsDefined(typeof(AsyncStateMachineAttribute), true))
{{
	throw new NotSupportedException(""Async void action delegates for {responseType.Name} are not supported.As they will run out of the scope of this call."");
}}
";
			}

			public static string GetResponseTypeInvoke(ResponseType responseType, bool async)
			{
				if (responseType is ExceptionResponseType)
				{
					return $@"{responseType.Name}?.Invoke(fhex);";
				}

				if (responseType.Status == null)
				{
					return $@"{responseType.Name}?.Invoke(response);";
				}
				else
				{
					string content = null;
					if (responseType.ActionType == nameof(Stream))
					{
						content = $@"{GetAwait(async)}response.Content.ReadAsStreamAsync(){GetAsyncEnding(async)}";
					}
					else if (responseType.ActionType != null)
					{
						content = $@"{GetAwait(async)}Serializer.Deserialize<{responseType.ActionType}>(response.Content){GetAsyncEnding(async)}";
					}

					string statusValue = null;
					if (responseType.Status == HttpStatusCode.RedirectMethod)
					{
						statusValue = nameof(HttpStatusCode.SeeOther);
					}
					else
					{
						statusValue = responseType.Status?.ToString();
					}

					return
	$@"
if(response.StatusCode == System.Net.HttpStatusCode.{statusValue})
{{
	{responseType.Name}?.Invoke({content});
}}
";
				}
			}

			public static string GetHttpMethod(HttpEndpoint endpoint)
			{
				var cancellationToken = endpoint.GetRequestModifiers().OfType<CancellationTokenModifier>().SingleOrDefault();
				var bodyParameter = endpoint.GetBodyParameter();

				string bodyString = null;

				if (bodyParameter?.Name == null)
				{
					bodyString = "null";
				}
				else
				{
					bodyString = $@"Serializer.Serialize({bodyParameter?.Name})";
				}

				if (endpoint.HttpType.Method == HttpMethod.Delete.Method)
				{
					return $".{nameof(GeneratedExtensions.DeleteAsync)}({cancellationToken?.Name})";
				}
				else if (endpoint.HttpType.Method == HttpMethod.Get.Method)
				{
					return $".{nameof(GeneratedExtensions.GetAsync)}({cancellationToken?.Name})";
				}
				else if (endpoint.HttpType.Method == HttpMethod.Put.Method)
				{
					return $".{nameof(GeneratedExtensions.PutAsync)}({bodyString}, {cancellationToken?.Name})";
				}
				else if (endpoint.HttpType.Method == new HttpMethod("PATCH").Method)
				{
					return $".{nameof(GeneratedExtensions.PatchAsync)}({bodyString}, {cancellationToken?.Name})";
				}
				else if (endpoint.HttpType.Method == HttpMethod.Post.Method)
				{
					return $".{nameof(GeneratedExtensions.PostAsync)}({bodyString}, {cancellationToken?.Name})";
				}
				else
				{
					return $"#error Unsupported HttpMethod of {endpoint.HttpType.Method}";
				}
			}

			public static string WriteRequestModifiers(IRequestModifier modifier)
			{
				if (modifier is CookieModifier cm)
				{
					return $@".WithCookies({cm.Name})";
				}
				else if (modifier is HeadersModifier hm)
				{
					return $@".WithHeaders({hm.Name})";
				}
				else if (modifier is RequestModifierDependency rm)
				{
					return $@".WithRequestModifiers({rm.GetDependencyName($"I{Settings.ClientInterfaceName}")})";
				}
				else if (modifier is SecurityModifier sm)
				{
					return $@".WithAuth({sm.Name})";
				}
				else if (modifier is TimeoutModifier tm)
				{
					var clientDependency = new ClientDependency(null);
					return $@".WithTimeout({tm.Name} ?? {clientDependency.GetDependencyName($"I{Settings.ClientInterfaceName}")}.{nameof(IClientWrapper.Timeout)})";
				}
				else if (modifier is ConstantHeader ch)
				{
					return $@".WithHeader(""{ch.Key}"", ""{ch.Value}"")";
				}
				else if (modifier is ParameterHeader ph)
				{
					return $@".WithHeader(""{ph.Name}"", {ph.Name})";
				}
				else if (modifier is CancellationTokenModifier ctm)
				{
					return null;
				}
				else
				{
					return $@"#warning IRequestModifier of type {modifier.GetType().Name} is not supported";
				}
			}

			public static string GetEndpointInfoVariables(HttpController controller, HttpEndpoint endpoint)
			{

				var controllerVar = $@"var {Constants.ControllerRouteReserved} = ""{endpoint.Parent.Name}"";";
				var actionVar = $@"var {Constants.ActionRouteReserved} = ""{endpoint.Name}"";";


				if (!endpoint.GetFullRoute(controller).Contains($"[{Constants.ControllerRouteReserved}]"))
				{
					controllerVar = null;
				}

				if (!endpoint.GetFullRoute(controller).Contains($"[{Constants.ActionRouteReserved}]"))
				{
					actionVar = null;
				}


				return
	$@"{controllerVar}
{actionVar}";
			}

			public static string GetRoute(HttpController controller, HttpEndpoint endpoint)
			{

				const string RouteParseRegex = @"{([^}]+)}";

				string routeUnformatted = endpoint.GetFullRoute(controller);

				var patterns = Regex.Matches(routeUnformatted, RouteParseRegex);

				var routeParameters = endpoint.GetRouteParameters().ToList();
				var queryParameters = endpoint.GetQueryParameters().ToList();

				foreach (var group in patterns)
				{
					Match match = group as Match;
					string filtered = match.Value.Replace("{", "").Replace("}", "");
					string[] split = filtered.Split(new char[] { ':' });

					string variable = split[0];


					if (!routeParameters.Any(x => x.Name.Equals(variable, StringComparison.CurrentCultureIgnoreCase)))
					{
						throw new Exception($"{variable} is missing from passed in parameters. Please check your route.");
					}
					var parameter = routeParameters.SingleOrDefault(x => x.Name.Equals(variable, StringComparison.CurrentCultureIgnoreCase));
					if (Helpers.IsRoutableType(parameter.Type))
					{
						routeUnformatted = routeUnformatted.Replace(match.Value, $"{{{Helpers.GetRouteStringTransform(parameter.Name, parameter.Type)}}}");
					}
				}

				if (queryParameters.Any())
				{
					string queryString = $"?{string.Join("&", queryParameters.Select(WriteQueryParameter))}";

					routeUnformatted += $"{queryString}";
				}



				routeUnformatted = routeUnformatted.Replace($"[{Constants.ControllerRouteReserved}]", $"{{{Constants.ControllerRouteReserved}}}");
				routeUnformatted = routeUnformatted.Replace($"[{Constants.ActionRouteReserved}]", $"{{{Constants.ActionRouteReserved}}}");
				return routeUnformatted;
			}

			public static string WriteQueryParameter(QueryParameter parameter)
			{
				string name = $"{{nameof({parameter.Name})}}";

				if (Helpers.IsEnumerable(parameter.Type))
				{
					return $@"{{string.Join(""&"",{parameter.Name}.Select(x => $""{name}={{{Helpers.GetRouteStringTransform("x", parameter.Type)}}}""))}}";
				}
				else
				{
					return $"{name}={{{Helpers.GetRouteStringTransform(parameter.Name, parameter.Type)}}}";
				}
			}

			public static string GetAsyncEnding(bool async)
			{
				if (async)
				{
					return $@".ConfigureAwait(false)";
				}
				else
				{
					return $@".ConfigureAwait(false).GetAwaiter().GetResult()";
				}
			}

			public static string GetAwait(bool async)
			{
				if (async)
				{
					return "await ";
				}
				return null;
			}

			public static string GetInterfaceReturnType(string returnType, bool async)
			{
				if (async)
				{
					if (returnType == null)
					{
						return $"Task";
					}
					else
					{
						return $"{Helpers.GetTaskType()}<{returnType}>";
					}
				}
				else
				{
					if (returnType == null)
					{
						return $"void";
					}
					else
					{
						return $"{returnType}";
					}
				}
			}

			public static string GetImplementationReturnType(string returnType, bool async)
			{
				if (async)
				{
					if (returnType == null)
					{
						return $"async Task";
					}
					else
					{
						return $"async {Helpers.GetTaskType()}<{returnType}>";
					}
				}
				else
				{
					if (returnType == null)
					{
						return $"void";
					}
					else
					{
						return $"{returnType}";
					}
				}
			}


			#endregion Endpoint
		}
		#endregion HTTP

		public static class SharedWriter
		{

			public static string GetParameter(IParameter parameter)
			{
				return $@"{parameter.Type} {parameter.Name}{(parameter.DefaultValue != null ? $" = {parameter.DefaultValue}" : $"")}";
			}

			public static string GetObsolete(IObsolete ob)
			{
				if (ob.Obsolete)
				{
					return $@"[{nameof(ObsoleteAttribute)}(""{ob.ObsoleteMessage}"")]";
				}
				else
				{
					return string.Empty;
				}

			}
		}
	}
}
