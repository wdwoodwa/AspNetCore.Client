﻿using MessagePack.Resolvers;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace AspNetCore.Client.Serializers
{
	/// <summary>
	/// Uses MessagePack for serializing and deserializing the http content
	/// </summary>
	internal class MessagePackSerializer : IHttpContentSerializer
	{
		internal static readonly string CONTENT_TYPE = "application/x-msgpack";
		public string[] ContentTypes => new string[] { CONTENT_TYPE };

		/// <summary>
		/// Deserializes the request content which is assumed to be MessagePack into a object of <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="content"></param>
		/// <returns></returns>
		public async Task<T> Deserialize<T>(HttpContent content)
		{
			return MessagePack.MessagePackSerializer.Deserialize<T>(await content.ReadAsStreamAsync().ConfigureAwait(false), ContractlessStandardResolver.Instance);
		}

		/// <summary>
		/// Serializes the request into a StringContent with a MessagePack media type
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="request"></param>
		/// <returns></returns>
		public HttpContent Serialize<T>(T request)
		{
			var data = MessagePack.MessagePackSerializer.Serialize(request, ContractlessStandardResolver.Instance);
			var content = new ByteArrayContent(data);
			content.Headers.ContentType = new MediaTypeHeaderValue(CONTENT_TYPE);
			content.Headers.ContentLength = data.Length;

			return content;
		}
	}
}
