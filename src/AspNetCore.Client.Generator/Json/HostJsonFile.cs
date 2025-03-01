﻿using Newtonsoft.Json;
using AspNetCore.Client.Generator.Framework.AspNetCoreHttp.Functions;

namespace AspNetCore.Client.Generator.Json
{
	public class HostJsonFile
	{
		public HostJson Data { get; }

		private readonly static JsonSerializerSettings _settings = new JsonSerializerSettings
		{
			MissingMemberHandling = MissingMemberHandling.Ignore
		};

		public HostJsonFile(string filePath)
		{
			var fileText = Helpers.SafelyReadFromFile(filePath);


			Data = JsonConvert.DeserializeObject<HostJson>(fileText, _settings);
		}
	}



}
