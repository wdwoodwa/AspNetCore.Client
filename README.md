# AspNetCore.Client
[![AppVeyor](https://ci.appveyor.com/api/projects/status/984mqqfnwytd3oga?svg=true)](https://ci.appveyor.com/project/Beffyman/aspnetcore-client)
---

If you are anything like me, you look at

```c#
using(var client  = new HttpClient())
{
	var request = await client.client.GetAsync($"api/mysuperspecialroute/{id}");
	//Now to make sure what came back is what is expected, in every case...
}
```
and think the following
- Why not just inject clients?
  - services.InstallClients(baseAddress = null);!
- How can I pool the HttpClient usage? 
  - HttpClient is injected!
- Yuck, hard coded routes, these can lead to issues if my endpoint is still under development. 
  - Generated On Build!
- How do I unit test this without spinning up a full web app? 
  - Works with Microsoft.AspNetCore.TestHost!
    - CancellationTokens are not respected inside the TestServer without some hacks though.
- How do I tell my teammates that an endpoint has headers it requires? 
  - RequiredHttpHeaderParameters! 
  - IncludeHeaderAttribute!
- How do I tell my teammates that an endpoint has known response types for status codes? 
  - KnownStatusesAndResponseTypes!
  - ProducesResponseType!
- What if sometimes I want to intercept requests before they go out? 
  - IncludeHttpOverride!
- If I am using Blazor and don't want to hard code routes?
  - BlazorClients!
- If I own the endpoint's code, why can't I just generate clients from it?
  - Introducing AspNetCore.Client.Generator!


## AspNetCore.Client.Core
[![NuGet](https://img.shields.io/nuget/v/AspNetCore.Client.Core.svg)](https://www.nuget.org/packages/AspNetCore.Client.Core/)

Package that contains required classes/attributes used by the generator package.

Can be included inside the web app to configure behavior.

## AspNetCore.Client.Generator
[![NuGet](https://img.shields.io/nuget/v/AspNetCore.Client.Generator.svg)](https://www.nuget.org/packages/AspNetCore.Client.Generator/)

On Build generator that will generate a Clients.cs file based on the ClientGeneratorSettings.json file the generator creates.


#### ClientGeneratorSettings.json Reference

- Locked
  - Whether or not the generator should run on build.
  - Keeps the Clients.cs file from changing
- RelativeRouteToServiceProjectFolder
  - Path to the Asp.Net.Core website from the folder that contains this file
- ClientInterfaceName
  - Name used for the base interface of all clients generated
  - Name used for the wrapper class of the HttpClient
- [ValueTask](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/async/async-return-types#generalized-async-return-types-and-valuetaskt)
  - Use ValueTask`<T>` instead of Task`<T>`
- Namespace
  - Namespace to be used for the Clients.cs
- BlazorClients
  - Whether or not the generate the clients so they are compatible with Blazor views
  - Differences include
    - Newtonsoft.Json.JsonConvert.DeserializeObject => JsonUtil.Deserialize
    - Default namespaces are different.
    - Requires a reference to [Microsoft.AspNetCore.Blazor](https://www.nuget.org/packages/Microsoft.AspNetCore.Blazor/) inside the client project
- IncludeHttpOverride
  - You will be required to provide the service registration for `IHttpOverride`.
  - If the `IHttpOverride` implementation returns a HttpResonseMessage, the actual http call will be ignored and the returned value will be used.
  - Allows for overrides to a separate service that will check to see if a HttpResponseMessage can be provided by it instead so it doesn't need to make the http call.
  - Useful if you want to intercept a request before it goes out depending on internal flags, etc.
  - Can be used with mocking
- AllowedNamespaces
  - Namespaces allowed to be pulled from Controllers that the generator is pulling the data from.
    - ex) You have `using MyApp.Contracts;` inside the Controller, if MyApp.Contracts is inside the allowed namespaces, it would be copied into the Clients.cs
- ExcludedNamespaces
  - Namespaces that will NOT be pulled from Controllers
    - ex) You have `using MyApp.Internals;` inside the controller, if MyApp.Internals is inside the excluded namespaces, it will not be copied into the Clients.cs
- KnownStatusesAndResponseTypes
  - Response types implied to be on every client call
    - `"BadRequest": "string"` implies that every client could return a 400 with a content of string, so the Action`<string>` BadRequest will be a nullable parameter on every client call
- DefaultHttpHeaders
  - Headers that will be included based on the HTTPType of the endpoint, based on the `[HttpGet]` attributes
- RequiredHttpHeaderParameters
  - Headers that will be required on the HttpTypes provided
  - Format is `"NAME": "TYPE"`, so it would show up as TYPE NAME in the parameters of the client