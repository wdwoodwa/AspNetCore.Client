﻿using AspNetCore.Client;
using Flurl.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using TestWebApp.Clients;

namespace TestWebApp.Tests
{
	public abstract class ServerInfo<T> : IDisposable where T : class
	{
		public IServiceProvider Provider { get; }
		public TestServer Server { get; }
		public HttpClient Client { get; }

		public ServerInfo()
		{
			Server = new TestServer(new WebHostBuilder()
					.UseStartup<T>());

			Client = Server.CreateClient();

			var services = new ServiceCollection();
			services.AddTestWebClients(ConfigureClient);

			Provider = services.BuildServiceProvider();
		}

		protected abstract void ConfigureClient(ClientConfiguration configure);

		public void Dispose()
		{
			Client.Dispose();
			Server.Dispose();
		}
	}


	public class ProtobufServerInfo : ServerInfo<ProtobufStartup>
	{
		protected override void ConfigureClient(ClientConfiguration configure)
		{
			configure.UseTestServerClient<ITestWebAppClientWrapper>(Client)
				.WithProtobufBody()
				.UseProtobufDeserializer()
				.UseProtobufSerializer();
		}
	}


	public class MessagePackServerInfo : ServerInfo<MessagePackStartup>
	{
		protected override void ConfigureClient(ClientConfiguration configure)
		{
			configure.UseTestServerClient<ITestWebAppClientWrapper>(Client)
				.WithMessagePackBody()
				.UseMessagePackDeserializer()
				.UseMessagePackSerializer();
		}
	}

	public class JsonServerInfo : ServerInfo<JsonStartup>
	{
		protected override void ConfigureClient(ClientConfiguration configure)
		{
			configure.UseTestServerClient<ITestWebAppClientWrapper>(Client)
				.WithJsonBody()
				.WithRequestModifier(request =>
				{
					return request.WithHeader("TestPre", "YES");
				})
				.UseJsonClientDeserializer()
				.UseJsonClientSerializer();
		}
	}

}
