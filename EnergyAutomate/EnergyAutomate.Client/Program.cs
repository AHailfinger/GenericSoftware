using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace EnergyAutomate.Client;

class Program
{
    static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        builder.Services.AddAuthorizationCore();
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddAuthenticationStateDeserialization();

        builder.Services.AddBlazorBootstrap();

        await builder.Build().RunAsync();
    }
}
