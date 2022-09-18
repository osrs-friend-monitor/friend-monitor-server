using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Logging;
using OSRSFriendMonitor;
using OSRSFriendMonitor.Configuration;
using OSRSFriendMonitor.Services.Database;

//IdentityModelEventSource.ShowPII = true;

var builder = WebApplication.CreateBuilder(args);
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

//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddMicrosoftIdentityWebApi(options =>
//    {
//        builder.Configuration.Bind("AzureAdB2C", options);
//    },
//    options => { 
//        builder.Configuration.Bind("AzureAdB2C", options); 
//    });

//builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();



//app.UseRouting();

//app.UseAuthentication();
//app.UseAuthorization();

var webSocketOptions = new WebSocketOptions()
{
    KeepAliveInterval = TimeSpan.FromSeconds(120)
};

app.UseWebSockets(webSocketOptions);

app.UseMiddleware<WebSocketMiddleware>();


//app.UseEndpoints(endpoints =>
//{
//    endpoints.MapRazorPages();
//    endpoints.MapControllers();
//});

app.Run();