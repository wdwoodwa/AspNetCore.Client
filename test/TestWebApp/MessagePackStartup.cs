﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MessagePack.AspNetCoreMvcFormatter;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TestWebApp.FakeServices;
using TestWebApp.GoodServices;
using TestWebApp.Hubs;
using WebApiContrib.Core.Formatter.Protobuf;

namespace TestWebApp
{
	public class MessagePackStartup
	{
		public MessagePackStartup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddMvc()
					.AddMvcOptions(option =>
					{
						option.OutputFormatters.Clear();
						option.OutputFormatters.Add(new MessagePackOutputFormatter(ContractlessStandardResolver.Instance));
						option.InputFormatters.Clear();
						option.InputFormatters.Add(new MessagePackInputFormatter(ContractlessStandardResolver.Instance));
					})
					.SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

			services.AddApiVersioning(options =>
			{
				options.AssumeDefaultVersionWhenUnspecified = true;
			});


			services.AddSignalR()
				.AddMessagePackProtocol();

			services.AddTransient<IFakeService, FakeService>();
			services.AddTransient<IGoodService, GoodService>();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}


			app.UseSignalR(routes =>
			{
				routes.MapHub<ChatHub>("/Chat");
			});

			app.UseMvc();
		}
	}
}
