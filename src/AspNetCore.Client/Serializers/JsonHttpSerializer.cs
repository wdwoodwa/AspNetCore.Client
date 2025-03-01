﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AspNetCore.Client.Serializers
{
	/// <summary>
	/// Uses Newtonsoft.Json for serializing and deserializing the http content
	/// </summary>
	internal class JsonHttpSerializer : IHttpContentSerializer
	{
		internal static readonly string CONTENT_TYPE = "application/json";
		internal static readonly string PROBLEM_TYPE = "application/problem+json";
		public string[] ContentTypes => new string[] { CONTENT_TYPE, PROBLEM_TYPE };


		private static readonly IDictionary<Type, Func<string, object>> _knownJsonPrimitives = new Dictionary<Type, Func<string, object>>
		{
			{ typeof(char), (_)=> char.Parse(_) },
			{ typeof(byte), (_)=> byte.Parse(_) },
			{ typeof(sbyte), (_)=> sbyte.Parse(_) },
			{ typeof(ushort), (_)=> ushort.Parse(_) },
			{ typeof(int), (_)=> int.Parse(_) },
			{ typeof(uint), (_)=> uint.Parse(_) },
			{ typeof(long), (_)=> long.Parse(_) },
			{ typeof(ulong), (_)=> ulong.Parse(_) },
			{ typeof(float), (_)=> float.Parse(_) },
			{ typeof(double), (_)=> double.Parse(_) },
			{ typeof(string), (_)=> _.TrimStart('"').TrimEnd('"') },
			{ typeof(bool), (_)=> bool.Parse(_) },
			{ typeof(DateTime), (_)=> DateTime.Parse(_.TrimStart('"').TrimEnd('"')) },
			{ typeof(Guid), (_)=> Guid.Parse(_.TrimStart('"').TrimEnd('"')) },
		};


		/// <summary>
		/// Deserializes the request content which is assumed to be json into a object of <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="content"></param>
		/// <returns></returns>
		public async Task<T> Deserialize<T>(HttpContent content)
		{
			if (_knownJsonPrimitives.ContainsKey(typeof(T)))
			{
				return (T)_knownJsonPrimitives[typeof(T)](await content.ReadAsStringAsync().ConfigureAwait(false));
			}
			else
			{
				using (var reader = new StreamReader(await content.ReadAsStreamAsync().ConfigureAwait(false), Encoding.UTF8, true, 1024, true))
				using (JsonReader jsonReader = new JsonTextReader(reader))
				{
					var serializer = new JsonSerializer();
					return serializer.Deserialize<T>(jsonReader);
				}
			}
		}

		/// <summary>
		/// Serializes the request into a StringContent with a json media type
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="request"></param>
		/// <returns></returns>
		public HttpContent Serialize<T>(T request)
		{
			var json = JsonConvert.SerializeObject(request);
			return new StringContent(json, Encoding.UTF8, CONTENT_TYPE);
		}
	}
}
