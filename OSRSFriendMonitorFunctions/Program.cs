using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = new HostBuilder();

builder.ConfigureFunctionsWorkerDefaults();
builder.ConfigureServices(collection =>
{
    collection.AddSingleton
});

var host = builder.Build();


host.Run();

