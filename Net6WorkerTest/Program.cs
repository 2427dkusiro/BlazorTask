using BlazorTask;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using Net6WorkerTest;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddWorkerService(config => config
                    .ResolveResourcesFromBootJson(config.HttpClient)
#if !DEBUG
                    .FetchBrotliResources("workerDecode.min.js")
#endif
                    );

WebAssemblyHost? host = builder.Build();
await Task.WhenAll(host.InitializeWorkerService(), host.RunAsync());