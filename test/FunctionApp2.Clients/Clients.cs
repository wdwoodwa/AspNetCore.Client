//------------------------------------------------------------------------------
// <auto-generated>
//		This code was generated from a template.
//		Manual changes to this file may cause unexpected behavior in your application.
//		Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
using AspNetCore.Client.Authorization;
using AspNetCore.Client.Exceptions;
using AspNetCore.Client.GeneratorExtensions;
using AspNetCore.Client.Http;
using AspNetCore.Client.RequestModifiers;
using AspNetCore.Client.Serializers;
using AspNetCore.Client;
using Flurl.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;
using System;
using TestAzureFunction.Contracts;

namespace FunctionApp2Client.Clients
{
	public static class FunctionApp2ClientInstaller
	{
		/// <summary>
		/// Register the autogenerated clients into the container with a lifecycle of scoped.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="configure">Overrides for client configuration</param>
		/// <returns></returns>
		public static IServiceCollection AddFunctionAppClients(this IServiceCollection services, Action<ClientConfiguration> configure)
		{
			var configuration = new ClientConfiguration();
			configuration.RegisterClientWrapperCreator<IFunctionApp2Client>(FunctionApp2ClientWrapper.Create);
			configuration.UseClientWrapper<IFunctionApp2ClientWrapper, FunctionApp2ClientWrapper>((provider) => new FunctionApp2ClientWrapper(provider.GetService<Func<IFunctionApp2Client, IFlurlClient>>(), configuration.GetSettings(), provider));
			configure?.Invoke(configuration);
			services.AddScoped<IFunction1Client, Function1Client>();
			return configuration.ApplyConfiguration<IFunctionApp2Client>(services);
		}
	}

	public interface IFunctionApp2ClientWrapper : IClientWrapper
	{
	}

	public class FunctionApp2ClientWrapper : IFunctionApp2ClientWrapper
	{
		public TimeSpan Timeout
		{
			get;
			internal set;
		}

		public IFlurlClient ClientWrapper
		{
			get;
			internal set;
		}

		public FunctionApp2ClientWrapper(Func<IFunctionApp2Client, IFlurlClient> client, ClientSettings settings, IServiceProvider provider)
		{
			ClientWrapper = client(null);
			if (settings.BaseAddress != null)
			{
				ClientWrapper.BaseUrl = settings.BaseAddress(provider);
			}

			Timeout = settings.Timeout;
		}

		public static IFunctionApp2ClientWrapper Create(Func<IFunctionApp2Client, IFlurlClient> client, ClientSettings settings, IServiceProvider provider)
		{
			return new FunctionApp2ClientWrapper(client, settings, provider);
		}
	}

	public interface IFunctionApp2Client : IClient
	{
	}
}

namespace FunctionApp2Client.Clients
{
	public interface IFunction1Client : IFunctionApp2Client
	{
		void Function1(User command, String AuthKey, Action<HttpResponseMessage> ResponseCallback = null, Action<FlurlHttpException> ExceptionCallback = null, Action<HttpResponseMessage> UnauthorizedCallback = null, IDictionary<String, Object> headers = null, IEnumerable<Cookie> cookies = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default);
		HttpResponseMessage Function1Raw(User command, String AuthKey, Action<FlurlHttpException> ExceptionCallback = null, IDictionary<String, Object> headers = null, IEnumerable<Cookie> cookies = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default);
		Task Function1Async(User command, String AuthKey, Action<HttpResponseMessage> ResponseCallback = null, Action<FlurlHttpException> ExceptionCallback = null, Action<HttpResponseMessage> UnauthorizedCallback = null, IDictionary<String, Object> headers = null, IEnumerable<Cookie> cookies = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default);
		ValueTask<HttpResponseMessage> Function1RawAsync(User command, String AuthKey, Action<FlurlHttpException> ExceptionCallback = null, IDictionary<String, Object> headers = null, IEnumerable<Cookie> cookies = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default);
	}

	internal class Function1Client : IFunction1Client
	{
		protected readonly IFunctionApp2ClientWrapper Client;
		protected readonly IHttpOverride HttpOverride;
		protected readonly IHttpSerializer Serializer;
		protected readonly IHttpRequestModifier Modifier;
		public Function1Client(IFunctionApp2ClientWrapper param_client, Func<IFunctionApp2Client, IHttpOverride> param_httpoverride, Func<IFunctionApp2Client, IHttpSerializer> param_serializer, Func<IFunctionApp2Client, IHttpRequestModifier> param_modifier)
		{
			Client = param_client;
			HttpOverride = param_httpoverride(this);
			Serializer = param_serializer(this);
			Modifier = param_modifier(this);
		}

		public void Function1(User command, String AuthKey, Action<HttpResponseMessage> ResponseCallback = null, Action<FlurlHttpException> ExceptionCallback = null, Action<HttpResponseMessage> UnauthorizedCallback = null, IDictionary<String, Object> headers = null, IEnumerable<Cookie> cookies = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
		{
			string url = $@"api/Function1?{command.GetQueryObjectString(nameof(command)).ConfigureAwait(false).GetAwaiter().GetResult()}";
			HttpResponseMessage response = null;
			response = HttpOverride.GetResponseAsync(HttpMethod.Post, url, null, cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();
			bool responseHandled = response != null;
			if (response == null)
			{
				try
				{
					response = Client.ClientWrapper.Request(url).WithCookies(cookies).WithHeaders(headers).WithTimeout(timeout ?? Client.Timeout).WithFunctionAuthorizationKey(AuthKey).AllowAnyHttpStatus().PostAsync(null, cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();
				}
				catch (FlurlHttpException fhex)
				{
					if (ExceptionCallback != null && ExceptionCallback.Method.IsDefined(typeof(AsyncStateMachineAttribute), true))
					{
						throw new NotSupportedException("Async void action delegates for ExceptionCallback are not supported.As they will run out of the scope of this call.");
					}

					if (ExceptionCallback != null)
					{
						responseHandled = true;
						ExceptionCallback?.Invoke(fhex);
					}
					else
					{
						throw fhex;
					}

					return;
				}

				HttpOverride.OnNonOverridedResponseAsync(HttpMethod.Post, url, null, response, cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();
			}

			if (ResponseCallback != null && ResponseCallback.Method.IsDefined(typeof(AsyncStateMachineAttribute), true))
			{
				throw new NotSupportedException("Async void action delegates for ResponseCallback are not supported.As they will run out of the scope of this call.");
			}

			if (ResponseCallback != null)
			{
				responseHandled = true;
				ResponseCallback.Invoke(response);
			}

			if (UnauthorizedCallback != null && UnauthorizedCallback.Method.IsDefined(typeof(AsyncStateMachineAttribute), true))
			{
				throw new NotSupportedException("Async void action delegates for UnauthorizedCallback are not supported.As they will run out of the scope of this call.");
			}

			if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
			{
				if (UnauthorizedCallback != null)
				{
					responseHandled = true;
					UnauthorizedCallback.Invoke(Serializer.Deserialize<HttpResponseMessage>(response.Content).ConfigureAwait(false).GetAwaiter().GetResult());
				}
			}

			if (!responseHandled)
			{
				throw new System.InvalidOperationException($"Response Status of {response.StatusCode} was not handled properly.");
			}

			return;
		}

		public HttpResponseMessage Function1Raw(User command, String AuthKey, Action<FlurlHttpException> ExceptionCallback = null, IDictionary<String, Object> headers = null, IEnumerable<Cookie> cookies = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
		{
			string url = $@"api/Function1?{command.GetQueryObjectString(nameof(command)).ConfigureAwait(false).GetAwaiter().GetResult()}";
			HttpResponseMessage response = null;
			response = HttpOverride.GetResponseAsync(HttpMethod.Post, url, null, cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();
			bool responseHandled = response != null;
			if (response == null)
			{
				try
				{
					response = Client.ClientWrapper.Request(url).WithCookies(cookies).WithHeaders(headers).WithTimeout(timeout ?? Client.Timeout).WithFunctionAuthorizationKey(AuthKey).AllowAnyHttpStatus().PostAsync(null, cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();
				}
				catch (FlurlHttpException fhex)
				{
					if (ExceptionCallback != null && ExceptionCallback.Method.IsDefined(typeof(AsyncStateMachineAttribute), true))
					{
						throw new NotSupportedException("Async void action delegates for ExceptionCallback are not supported.As they will run out of the scope of this call.");
					}

					if (ExceptionCallback != null)
					{
						responseHandled = true;
						ExceptionCallback?.Invoke(fhex);
					}
					else
					{
						throw fhex;
					}

					return null;
				}

				HttpOverride.OnNonOverridedResponseAsync(HttpMethod.Post, url, null, response, cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();
			}

			return response;
		}

		public async Task Function1Async(User command, String AuthKey, Action<HttpResponseMessage> ResponseCallback = null, Action<FlurlHttpException> ExceptionCallback = null, Action<HttpResponseMessage> UnauthorizedCallback = null, IDictionary<String, Object> headers = null, IEnumerable<Cookie> cookies = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
		{
			string url = $@"api/Function1?{await command.GetQueryObjectString(nameof(command)).ConfigureAwait(false)}";
			HttpResponseMessage response = null;
			response = await HttpOverride.GetResponseAsync(HttpMethod.Post, url, null, cancellationToken).ConfigureAwait(false);
			bool responseHandled = response != null;
			if (response == null)
			{
				try
				{
					response = await Client.ClientWrapper.Request(url).WithCookies(cookies).WithHeaders(headers).WithTimeout(timeout ?? Client.Timeout).WithFunctionAuthorizationKey(AuthKey).AllowAnyHttpStatus().PostAsync(null, cancellationToken).ConfigureAwait(false);
				}
				catch (FlurlHttpException fhex)
				{
					if (ExceptionCallback != null && ExceptionCallback.Method.IsDefined(typeof(AsyncStateMachineAttribute), true))
					{
						throw new NotSupportedException("Async void action delegates for ExceptionCallback are not supported.As they will run out of the scope of this call.");
					}

					if (ExceptionCallback != null)
					{
						responseHandled = true;
						ExceptionCallback?.Invoke(fhex);
					}
					else
					{
						throw fhex;
					}

					return;
				}

				await HttpOverride.OnNonOverridedResponseAsync(HttpMethod.Post, url, null, response, cancellationToken).ConfigureAwait(false);
			}

			if (ResponseCallback != null && ResponseCallback.Method.IsDefined(typeof(AsyncStateMachineAttribute), true))
			{
				throw new NotSupportedException("Async void action delegates for ResponseCallback are not supported.As they will run out of the scope of this call.");
			}

			if (ResponseCallback != null)
			{
				responseHandled = true;
				ResponseCallback.Invoke(response);
			}

			if (UnauthorizedCallback != null && UnauthorizedCallback.Method.IsDefined(typeof(AsyncStateMachineAttribute), true))
			{
				throw new NotSupportedException("Async void action delegates for UnauthorizedCallback are not supported.As they will run out of the scope of this call.");
			}

			if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
			{
				if (UnauthorizedCallback != null)
				{
					responseHandled = true;
					UnauthorizedCallback.Invoke(await Serializer.Deserialize<HttpResponseMessage>(response.Content).ConfigureAwait(false));
				}
			}

			if (!responseHandled)
			{
				throw new System.InvalidOperationException($"Response Status of {response.StatusCode} was not handled properly.");
			}

			return;
		}

		public async ValueTask<HttpResponseMessage> Function1RawAsync(User command, String AuthKey, Action<FlurlHttpException> ExceptionCallback = null, IDictionary<String, Object> headers = null, IEnumerable<Cookie> cookies = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
		{
			string url = $@"api/Function1?{await command.GetQueryObjectString(nameof(command)).ConfigureAwait(false)}";
			HttpResponseMessage response = null;
			response = await HttpOverride.GetResponseAsync(HttpMethod.Post, url, null, cancellationToken).ConfigureAwait(false);
			bool responseHandled = response != null;
			if (response == null)
			{
				try
				{
					response = await Client.ClientWrapper.Request(url).WithCookies(cookies).WithHeaders(headers).WithTimeout(timeout ?? Client.Timeout).WithFunctionAuthorizationKey(AuthKey).AllowAnyHttpStatus().PostAsync(null, cancellationToken).ConfigureAwait(false);
				}
				catch (FlurlHttpException fhex)
				{
					if (ExceptionCallback != null && ExceptionCallback.Method.IsDefined(typeof(AsyncStateMachineAttribute), true))
					{
						throw new NotSupportedException("Async void action delegates for ExceptionCallback are not supported.As they will run out of the scope of this call.");
					}

					if (ExceptionCallback != null)
					{
						responseHandled = true;
						ExceptionCallback?.Invoke(fhex);
					}
					else
					{
						throw fhex;
					}

					return null;
				}

				await HttpOverride.OnNonOverridedResponseAsync(HttpMethod.Post, url, null, response, cancellationToken).ConfigureAwait(false);
			}

			return response;
		}
	}
}