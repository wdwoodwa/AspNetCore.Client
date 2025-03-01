﻿using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestWebApp.Contracts;
using TestWebApp.Hubs;

namespace TestWebApp.Tests
{
	public static class CancellationTokenHelper
	{
		public static Task WhenCanceled(this CancellationToken cancellationToken)
		{
			var tcs = new TaskCompletionSource<bool>();
			cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
			return tcs.Task;
		}
	}

	[TestFixture]
	public class SignalRTests
	{

		[Test]
		public async Task SendReceiveMessageAsync()
		{
			using (var endpoint = new JsonServerInfo())
			{
				var hub = new ChatHubConnectionBuilder(endpoint.Server.BaseAddress, null,
					config =>
					{
						config.HttpMessageHandlerFactory = _ => endpoint.Server.CreateHandler();
					})
					.Build();

				string user = null;
				string message = null;

				CancellationTokenSource tokenSource = new CancellationTokenSource(2000);
				var token = tokenSource.Token;

				hub.OnReceiveMessage((usr, msg) =>
				{
					user = usr;
					message = msg;
					tokenSource.Cancel();
				});

				await hub.StartAsync();

				await hub.SendMessageAsync("Test", "Hello World");

				await token.WhenCanceled();

				await hub.StopAsync();

				Assert.AreEqual("Test", user);
				Assert.AreEqual("Hello World", message);
			}
		}


		[Test]
		public async Task CounterChannelTest()
		{
			using (var endpoint = new JsonServerInfo())
			{
				var hub = new ChatHubConnectionBuilder(endpoint.Server.BaseAddress, null,
					config =>
					{
						config.HttpMessageHandlerFactory = _ => endpoint.Server.CreateHandler();
					})
					.Build();

				int count = 100;
				int delay = 20;

				IList<int> results = new List<int>();

				await hub.StartAsync();

				var channel = await hub.StreamCounterAsync(count, delay);

				while (await channel.WaitToReadAsync())
				{
					while (channel.TryRead(out int item))
					{
						results.Add(item);
					}
				}

				await hub.StopAsync();

				Assert.AreEqual(count, results.Count());
			}
		}


		[Test]
		public async Task CounterBlockingTest()
		{
			using (var endpoint = new JsonServerInfo())
			{
				var hub = new ChatHubConnectionBuilder(endpoint.Server.BaseAddress, null,
					config =>
					{
						config.HttpMessageHandlerFactory = _ => endpoint.Server.CreateHandler();
					})
					.Build();

				int count = 100;
				int delay = 20;

				await hub.StartAsync();

				IEnumerable<int> results = await hub.ReadCounterBlockingAsync(count, delay);

				await hub.StopAsync();

				Assert.AreEqual(count, results.Count());
			}
		}


		[Test]
		public async Task MessagePackTest()
		{
			using (var endpoint = new MessagePackServerInfo())
			{
				var hub = new ChatHubConnectionBuilder(endpoint.Server.BaseAddress, null,
					config =>
					{
						config.HttpMessageHandlerFactory = _ => endpoint.Server.CreateHandler();
					})
					.AddMessagePackProtocol()
					.Build();

				MyFancyDto actual = null;
				MyFancyDto expected = new MyFancyDto
				{
					Collision = Guid.NewGuid(),
					Description = "I am a test",
					Id = 15,
					When = DateTime.Now
				};

				CancellationTokenSource tokenSource = new CancellationTokenSource(2000);
				var token = tokenSource.Token;

				hub.OnReceiveMessage2((dto) =>
				{
					actual = dto;
					tokenSource.Cancel();
				});

				await hub.StartAsync();

				await hub.DtoMessageAsync(expected);

				await token.WhenCanceled();

				await hub.StopAsync();

				Assert.AreEqual(expected.Collision, actual.Collision);
				Assert.AreEqual(expected.Description, actual.Description);
				Assert.AreEqual(expected.Id, actual.Id);
				Assert.AreEqual(expected.When.ToLocalTime(), actual.When.ToLocalTime());
			}
		}

	}
}
