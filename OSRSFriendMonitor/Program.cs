using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Logging;
using OSRSFriendMonitor;
using OSRSFriendMonitor.Activity.Models;
using OSRSFriendMonitor.Configuration;
using OSRSFriendMonitor.Shared.Services.Database;
using System.Diagnostics;
using System.Text.Json;

//IdentityModelEventSource.ShowPII = true;
var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
};
var builder = WebApplication.CreateBuilder(args);
//string temp = "{ \"x\":3162,\"y\":3488,\"plane\":0,\"accountHash\":-8143573453725794545,\"timestamp\":1663995925,\"type\":\"LOCATION\"}";
//ActivityUpdate lu = JsonSerializer.Deserialize<ActivityUpdate>(temp, options);
//String json = JsonSerializer.Serialize(lu, options);
//Debug.WriteLine(json);
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxConcurrentConnections = 20000;
    serverOptions.Limits.MaxConcurrentUpgradedConnections = 20000;
});

builder.Services.AddSingleton<LiveConnectionManager>();
builder.Services.AddSingleton<LocationUpdateNotifier>();

CosmosClient client = new CosmosClientBuilder(builder.Configuration["DatabaseConnectionString"])
    .WithCustomSerializer(new SystemTextJsonSerializer())
    .Build();

Database db = client.GetDatabase("FriendMonitorDatabase");

Container accountsContainer = db.GetContainer("Accounts");

builder.Services.AddSingleton<IDatabaseService>(new DatabaseService(accountsContainer));

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

var webSocketOptions = new WebSocketOptions()
{
    KeepAliveInterval = TimeSpan.FromSeconds(120)
};

app.UseWebSockets(webSocketOptions);

app.UseMiddleware<WebSocketMiddleware>();


app.UseEndpoints(endpoints =>
{
    endpoints.MapRazorPages();
    endpoints.MapControllers();
});

app.Run();