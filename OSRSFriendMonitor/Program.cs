using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Identity.Web;
using OSRSFriendMonitor.Services;
using OSRSFriendMonitor.Services.SocketConnection;
using OSRSFriendMonitor.Shared.Services.Account;
using OSRSFriendMonitor.Shared.Services.Activity;
using OSRSFriendMonitor.Shared.Services.Cache;
using OSRSFriendMonitor.Shared.Services.Database;
using StackExchange.Redis;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

CosmosClient client = new CosmosClientBuilder(builder.Configuration["DatabaseConnectionString"])
    .WithCustomSerializer(new SystemTextJsonSerializer())
    .Build();

Database db = client.GetDatabase("FriendMonitorDatabase");

Container accountsContainer = db.GetContainer("Accounts");
Container activityContainer = db.GetContainer("Activity");

builder.Services.AddSingleton<IDatabaseService>(new DatabaseService(accountsContainer, activityContainer));

builder.Services.AddSingleton<IRemoteCache, RedisCache>(factory =>
{
    ConnectionMultiplexer connection = ConnectionMultiplexer.Connect(new ConfigurationOptions()
    {
        EndPoints =
        {
            { builder.Configuration["RedisAddress"]!, int.Parse(builder.Configuration["RedisPort"]!) }
        },
        User = builder.Configuration["RedisUser"],
        Password = builder.Configuration["RedisPassword"],
        ConnectRetry = 4
    });

    return new RedisCache(connection);
});

TickService tickService = new TickService();

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<SocketConnectionManager>();
builder.Services.AddSingleton<ILocalCache, LocalCache>();
builder.Services.AddSingleton<ITickAccessor>(tickService);
builder.Services.AddSingleton<ITickIncrementer>(tickService);
builder.Services.AddSingleton<ILocationCache, ActivityCache>();
builder.Services.AddSingleton<IAccountCache, AccountCache>();
builder.Services.AddSingleton<ILocalActivityBroadcaster, LocalActivityBroadcaster>();
builder.Services.AddSingleton<IRemoteActivityBroadcaster, RemoteActivityBroadcaster>();
builder.Services.AddSingleton<IActivityProcessor, ActivityProcessor>();
builder.Services.AddSingleton<IActivityStorageService, ActivityStorageService>();
builder.Services.AddSingleton<IActivityStorageService, ActivityStorageService>();
builder.Services.AddSingleton<IAccountStorageService, AccountStorageService>();
builder.Services.AddSingleton<IRunescapeAccountConnectionService, RunescapeAccountConnectionService>();
builder.Services.AddSingleton(new SocketMessageJsonContext(new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
builder.Services.AddSingleton<IRunescapeAccountContextStorage>(context =>
{
    SocketConnectionManager socketManager = context.GetRequiredService<SocketConnectionManager>();

    IRunescapeAccountContextStorage accountContextStorage = new RunescapeAccountContextStorage();

    socketManager.accountConnected = identifier => accountContextStorage.AddNewContext(identifier);
    socketManager.accountDisconnected = identifier => accountContextStorage.RemoveContext(identifier);

    return accountContextStorage;
});

builder.Services.AddHostedService<LocationTickService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(options =>
    {
        builder.Configuration.Bind("AzureAdB2C", options);
    },
    options =>
    {
        builder.Configuration.Bind("AzureAdB2C", options);
    });

builder.Services.AddRazorPages();
builder.Services.AddMvc().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseWebSockets(new() { KeepAliveInterval = TimeSpan.FromSeconds(60) });

app.MapRazorPages();
app.MapControllers();

app.Run();