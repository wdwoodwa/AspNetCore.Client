﻿using System.Collections.Generic;

namespace AspNetCore.Client.Generator.Framework.AspNetCoreHttp.Headers
{
	/// <summary>
	/// Detailing that this component contains headers that can be added
	/// </summary>
	public interface IHeaders
	{
		/// <summary>
		/// List of headers that can be added to the context that never change in value
		/// </summary>
		IList<ConstantHeader> ConstantHeader { get; set; }

		/// <summary>
		/// List of headers that can be added to the context that are used as a parameter
		/// </summary>
		IList<ParameterHeader> ParameterHeader { get; set; }
	}
}
