using System;
using System.Text.Json.Serialization;

namespace OSRSFriendMonitor.Shared.Services.Database.Models;

[JsonSerializable(typeof(UserAccount))]
[JsonSerializable(typeof(LocationUpdate))]
[JsonSerializable(typeof(RunescapeAccount))]
[JsonSerializable(typeof(ValidatedFriendsList))]
public partial class DatabaseModelJsonContext: JsonSerializerContext
{

}

