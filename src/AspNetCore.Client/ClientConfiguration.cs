﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using AspNetCore.Client.Authorization;
using AspNetCore.Client.Http;
using AspNetCore.Client.RequestModifiers;
using AspNetCore.Client.Serializers;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCore.Client
{

	/// <summary>
	/// Configuration for the clients
	/// </summary>
	public class ClientConfiguration
	{
		/// <summary>
		/// Base address to be used for a HttpClient being injected
		/// </summary>
		private Func<IServiceProvider, string> HttpBaseAddress { get; set; }

		/// <summary>
		/// Does the client have a constant base address? Allow improved pooling
		/// </summary>
		private bool ConstantBaseAddress = false;

		/// <summary>
		/// What serializer to use for these clients
		/// </summary>
		internal Type Serializer { get; set; }

		/// <summary>
		/// What deserializers that are supported
		/// </summary>
		internal ICollection<Type> Deserializers { get; set; } = new HashSet<Type>();

		/// <summary>
		/// What IHttpOverride to use, allows for pre-post request calls
		/// </summary>
		private Type HttpOverrideType { get; set; } = typeof(DefaultHttpOverride);

		/// <summary>
		/// Functions that will always be ran on a request
		/// </summary>
		private ICollection<Func<IHttpSettingsContainer, IHttpSettingsContainer>> PredefinedFunctions { get; } = new List<Func<IHttpSettingsContainer, IHttpSettingsContainer>>();

		/// <summary>
		/// Headers that will always be included with every request
		/// </summary>
		private IDictionary<string, string> PredefinedHeaders { get; } = new Dictionary<string, string>();

		/// <summary>
		/// Headers that will always be included with every request
		/// </summary>
		private List<Cookie> PredefinedCookies { get; } = new List<Cookie>();

		/// <summary>
		/// Override the default timeout, which is 60
		/// </summary>
		private TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);

		/// <summary>
		/// Registration for the clients
		/// </summary>
		private Action<IServiceCollection> _clientRegister = null;

		/// <summary>
		/// Func that creates the client wrapper, comes from generated files
		/// </summary>
		private Func<Func<IClient, IFlurlClient>, ClientSettings, IServiceProvider, IClientWrapper> _clientCreator = null;

		/// <summary>
		/// Whether or not to inject HttpClient
		/// </summary>
		private bool HttpPool = false;

		/// <summary>
		/// Does the container already have an httpclient injected?  AKA blazor?
		/// </summary>
		private bool ExistingHttpClient = false;

		/// <summary>
		/// Applies the configurations to the <see cref="IServiceCollection"/>
		/// </summary>
		/// <param name="services"></param>
		/// <returns></returns>
		public IServiceCollection ApplyConfiguration<T>(IServiceCollection services) where T : IClient
		{
			if (!Deserializers.Any())
			{
				Deserializers.Add(typeof(JsonHttpSerializer));
			}

			if (!Deserializers.Contains(typeof(TextHttpSerializer)))
			{
				Deserializers.Add(typeof(TextHttpSerializer));
			}

			if (Serializer == null)
			{
				Serializer = typeof(JsonHttpSerializer);
			}


			foreach (var deser in Deserializers)
			{
				services.AddTransient(deser);
			}


			services.AddTransient(Serializer);

			if (HttpOverrideType == null)
			{
				HttpOverrideType = typeof(DefaultHttpOverride);
			}

			if (_clientRegister != null)
			{
				_clientRegister(services);
			}
			else
			{
				throw new Exception("Error setting up client dependencies register.");
			}

			services.AddScoped(HttpOverrideType);
			services.AddSingleton<Func<T, IHttpSerializer>>(provider => (_ => new HttpSerializer(provider, this)));
			services.AddScoped<Func<T, IHttpOverride>>(provider => (_ => (IHttpOverride)provider.GetService(HttpOverrideType)));

			if (ExistingHttpClient)
			{
				services.AddTransient<Func<T, IFlurlClient>>(provider =>
				{
					return _ => new FlurlClient(provider.GetService<HttpClient>());
				});
			}
			else
			{
				if (HttpPool)
				{
					if (ConstantBaseAddress)
					{
						services.AddSingleton<IFlurlClientFactory, PerHostFlurlClientFactory>();

						services.AddScoped<Func<T, IFlurlClient>>(provider =>
						{
							var factory = provider.GetService<IFlurlClientFactory>();
							return _ => factory.Get(new Flurl.Url(HttpBaseAddress(provider)));
						});
					}
					else
					{
						services.AddHttpClient(typeof(T).Name);

						services.AddTransient<HttpClient>(provider =>
						{
							return provider.GetService<System.Net.Http.IHttpClientFactory>().CreateClient(typeof(T).Name);
						});


						services.AddTransient<Func<T, IFlurlClient>>(provider =>
						{
							return _ => new FlurlClient(provider.GetService<HttpClient>());
						});
					}

				}
				else
				{
					services.AddTransient<Func<T, IFlurlClient>>(provider =>
					{
						return (_ => new FlurlClient());
					});
				}
			}





			services.AddScoped<Func<T, IHttpRequestModifier>>((provider) =>
			 {
				 return _ => new HttpRequestModifier {
					 PredefinedHeaders = PredefinedHeaders,
					 PredefinedCookies = PredefinedCookies,
					 PredefinedFunctions = PredefinedFunctions
				 };
			 });

			return services;
		}

		/// <summary>
		/// Sets the base address to be used for the injected Clients.
		/// </summary>
		/// <param name="baseAddress"></param>
		/// <returns></returns>
		public ClientConfiguration WithBaseAddress(Func<IServiceProvider, string> baseAddress)
		{
			ConstantBaseAddress = false;
			HttpBaseAddress = baseAddress;

			return this;
		}

		/// <summary>
		/// Sets the base address to be used for the injected Clients. Using this override will enable Flurl http pooling
		/// </summary>
		/// <param name="baseAddress"></param>
		/// <returns></returns>
		public ClientConfiguration WithBaseAddress(string baseAddress)
		{
			ConstantBaseAddress = true;
			HttpBaseAddress = _ => baseAddress;

			return this;
		}

		/// <summary>
		/// Overrides the default timeout of 60 seconds with the one provided
		/// </summary>
		/// <param name="timeout"></param>
		/// <returns></returns>
		public ClientConfiguration WithTimeout(TimeSpan timeout)
		{
			Timeout = timeout;

			return this;
		}

		/// <summary>
		/// Adds a predefined header to the configuration
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public ClientConfiguration WithPredefinedHeader(string name, string value)
		{
			PredefinedHeaders.Add(name, value);
			return this;
		}

		/// <summary>
		/// Adds the security header onto every request unless overwritten by a specific client call via the parameters.
		/// </summary>
		/// <param name="auth"></param>
		/// <returns></returns>
		public ClientConfiguration WithSecurity(SecurityHeader auth)
		{
			Func<IHttpSettingsContainer, IHttpSettingsContainer> func = (IHttpSettingsContainer request) =>
			{
				return auth.AddAuth(request);
			};

			PredefinedFunctions.Add(func);

			return this;
		}

		/// <summary>
		/// Adds the following method call onto every request.
		/// </summary>
		/// <param name="requestModifier"></param>
		/// <returns></returns>
		public ClientConfiguration WithRequestModifier(Func<IHttpSettingsContainer, IHttpSettingsContainer> requestModifier)
		{
			PredefinedFunctions.Add(requestModifier);

			return this;
		}

		/// <summary>
		/// Adds a predefined cookie to the configuration
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public ClientConfiguration WithPredefinedCookie(string name, string value)
		{
			PredefinedCookies.Add(new Cookie(name, value));
			return this;
		}

		/// <summary>
		/// Adds the predefined cookies to the configuration
		/// </summary>
		/// <param name="cookies"></param>
		/// <returns></returns>
		public ClientConfiguration WithPredefinedCookies(IEnumerable<Cookie> cookies)
		{
			PredefinedCookies.AddRange(cookies);
			return this;
		}

		/// <summary>
		/// Adds an Accept of "application/json" to every request
		/// </summary>
		/// <returns></returns>
		public ClientConfiguration WithJsonBody()
		{
			return WithPredefinedHeader("Accept", JsonHttpSerializer.CONTENT_TYPE);
		}

		/// <summary>
		/// Adds an Accept of "text/plain" to every request
		/// </summary>
		/// <returns></returns>
		public ClientConfiguration WithPlainTextBody()
		{
			return WithPredefinedHeader("Accept", TextHttpSerializer.CONTENT_TYPE);
		}

		/// <summary>
		/// Uses <see cref="JsonHttpSerializer"/> to serialize and deserialize requests
		/// </summary>
		public ClientConfiguration UseJsonClientSerializer()
		{
			Serializer = typeof(JsonHttpSerializer);

			return this;
		}

		/// <summary>
		/// Uses <see cref="JsonHttpSerializer"/> to serialize and deserialize requests
		/// </summary>
		public ClientConfiguration UseJsonClientDeserializer()
		{
			Deserializers.Add(typeof(JsonHttpSerializer));

			return this;
		}

		/// <summary>
		/// Overrides the default <see cref="IHttpOverride"/> and uses the one provided
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public ClientConfiguration UseHttpOverride<T>() where T : IHttpOverride
		{
			HttpOverrideType = typeof(T);
			return this;
		}

		/// <summary>
		/// Overrides the default <see cref="JsonHttpSerializer"/> with the one provided
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public ClientConfiguration UseSerializer<T>() where T : IHttpContentSerializer
		{
			Serializer = typeof(T);
			return this;
		}

		/// <summary>
		/// Adds the deserializer to be used when it's content type is detected
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public ClientConfiguration UseDeserializer<T>() where T : IHttpContentSerializer
		{
			Deserializers.Add(typeof(T));
			return this;
		}

		/// <summary>
		/// Enables the use of Microsoft.Extensions.Http for injecting HttpClients that the IFlurlClient will use.
		/// </summary>
		/// <returns></returns>
		public ClientConfiguration UseHttpClientFactory()
		{
			HttpPool = true;
			return this;
		}

		/// <summary>
		/// Uses the existing http client injection inside the container, not compatible with <see cref="UseHttpClientFactory"/> and <see cref="WithBaseAddress(string)"/>
		/// </summary>
		/// <returns></returns>
		public ClientConfiguration UseExistingHttpClient()
		{
			ExistingHttpClient = true;
			return this;
		}


		/// <summary>
		/// Delays registration of the client wrapper into the container, in case we need to override with a test server
		/// </summary>
		/// <typeparam name="TService"></typeparam>
		/// <typeparam name="TImplementation"></typeparam>
		/// <param name="registrationFunc"></param>
		/// <returns></returns>
		public ClientConfiguration UseClientWrapper<TService, TImplementation>(Func<IServiceProvider, TImplementation> registrationFunc)
			where TImplementation : class, TService
			where TService : class, IClientWrapper
		{
			_clientRegister = (IServiceCollection services) =>
			{
				services.AddScoped<TService, TImplementation>(registrationFunc);
			};

			return this;
		}

		/// <summary>
		/// Registers the client creation for the current clients project into this configuration
		/// </summary>
		/// <param name="registrationFunc"></param>
		/// <returns></returns>
		public ClientConfiguration RegisterClientWrapperCreator<TService>(Func<Func<TService, IFlurlClient>, ClientSettings, IServiceProvider, IClientWrapper> registrationFunc)
			where TService : class, IClient
		{
			_clientCreator = registrationFunc;

			return this;
		}

		/// <summary>
		/// Overrides this client's configuration to use a specific <see cref="HttpClient"/> instead.  Useful for test servers
		/// </summary>
		/// <typeparam name="TService">Autogenerated interface that inherits of <see cref="IClientWrapper"/> based on the ClientInterfaceName property</typeparam>
		/// <param name="client"></param>
		/// <returns></returns>
		public ClientConfiguration UseTestServerClient<TService>(HttpClient client)
			where TService : class, IClientWrapper
		{
			_clientRegister = (IServiceCollection services) =>
			{
				services.AddScoped((provider) => (TService)_clientCreator(_ => new FlurlClient(client), this.GetSettings(), provider));
			};

			return this;
		}

		/// <summary>
		/// Gets the global settings to be passed into each client
		/// </summary>
		/// <returns></returns>
		public ClientSettings GetSettings()
		{
			return new ClientSettings {
				BaseAddress = HttpBaseAddress,
				Timeout = Timeout
			};
		}
	}
}
