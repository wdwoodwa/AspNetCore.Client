﻿using AspNetCore.Client.Generator;
using Microsoft.Build.Framework;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace AspNetCore.Client.Test.Generator
{
	/// <summary>
	/// This project is used to run the generator locally on all the test projects
	/// </summary>
	public static class Program
	{
		const string WEBAPP = "TestWebApp.Clients";
		const string BLAZOR = "TestBlazorApp.Clients";
		const string FUNCTIONS = "TestAzureFunction.Clients";
		const string FUNCTIONS2 = "FunctionApp2.Clients";
		const string FAILURE_DIR = "AspNetCore.Client";

		static void Main()
		{
			var webApp = GoUpUntilDirectory(WEBAPP, FAILURE_DIR);
			var blazor = GoUpUntilDirectory(BLAZOR, FAILURE_DIR);
			var functions = GoUpUntilDirectory(FUNCTIONS, FAILURE_DIR);
			var functions2 = GoUpUntilDirectory(FUNCTIONS2, FAILURE_DIR);

			if (!(Generate(webApp)
				&& Generate(blazor)
				&& Generate(functions)
				&& Generate(functions2)))
			{
				Console.ReadKey();
			}

		}

		private static string GoUpUntilDirectory(string targetDirectoryName, string failDirectory)
		{
			string currentPath = System.Environment.CurrentDirectory;

			while (Path.GetFileName(currentPath) != failDirectory)
			{
				var childDirectories = Directory.GetDirectories(currentPath).ToList();
				var dirs = childDirectories.Select(Path.GetFileName).ToList();
				if (!dirs.Contains(targetDirectoryName))
				{
					currentPath = Path.GetFullPath($"{currentPath}/..");
				}
				else
				{
					return childDirectories.SingleOrDefault(x => Path.GetFileName(x) == targetDirectoryName);
				}
			}

			throw new DirectoryNotFoundException($"Directory {targetDirectoryName} was not found.");
		}


		private static bool Generate(string path)
		{
			var projectName = Path.GetFileName(path);
			var projFile = $"{path}/{projectName}.csproj";
			var data = XElement.Load(projFile);

			IDictionary<string, string> properties = data.Elements("PropertyGroup")
				.Descendants()
				.ToDictionary(x => x.Name.ToString(), y => y.Value);

			var previousWorkDir = Environment.CurrentDirectory;
			var task = new GeneratorTask();
			task.Fill(properties);
			task.CurrentDirectory = path;


			var mockedBuildEngine = new Mock<IBuildEngine>();
			mockedBuildEngine.Setup(x => x.LogErrorEvent(It.IsAny<BuildErrorEventArgs>())).Callback((BuildErrorEventArgs args) =>
			{
				Console.Error.WriteLine(args.Message);
				throw new Exception(args.Message);
			});
			mockedBuildEngine.Setup(x => x.LogMessageEvent(It.IsAny<BuildMessageEventArgs>())).Callback((BuildMessageEventArgs args) =>
			{
				Console.WriteLine(args.Message);
			});
			mockedBuildEngine.Setup(x => x.LogWarningEvent(It.IsAny<BuildWarningEventArgs>())).Callback((BuildWarningEventArgs args) =>
			{
				Console.WriteLine(args.Message);
			});

			task.BuildEngine = mockedBuildEngine.Object;

			var success = task.ByPassExecute();
			Environment.CurrentDirectory = previousWorkDir;
			return success;
		}

	}
}
