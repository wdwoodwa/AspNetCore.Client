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
using TestBlazorApp.Shared;

namespace TestBlazorApp.Clients.Routes
{
	public static class SampleDataClientRoutes
	{
		public static string WeatherForecasts()
		{
			var controller = "SampleData";
			var action = "WeatherForecasts";
			string url = $@"api/{controller}/{action}";
			return url;
		}
	}
}