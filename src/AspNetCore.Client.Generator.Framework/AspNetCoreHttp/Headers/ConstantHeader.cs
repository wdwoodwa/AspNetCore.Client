﻿using System.Collections.Generic;
using AspNetCore.Client.Generator.Framework.Navigation;
using AspNetCore.Server.Attributes.Http;

namespace AspNetCore.Client.Generator.Framework.AspNetCoreHttp.Headers
{
	/// <summary>
	/// Handles <see cref="IncludeHeaderAttribute"/>
	/// </summary>
	public class ConstantHeader : Header
	{
		/// <summary>
		/// Whether a header is a constant or not
		/// </summary>
		public string Value { get; set; }

		/// <summary>
		/// Default constructor
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public ConstantHeader(string key, string value) : base(key)
		{
			Value = value;
		}

		/// <summary>
		/// Retrieve all the <see cref="INavNode"/> implemented children of this node
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<INavNode> GetChildren()
		{
			return null;
		}

		/// <summary>
		/// Returns a string that represents the current object
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return $"{Key} : {Value}";
		}
	}
}
