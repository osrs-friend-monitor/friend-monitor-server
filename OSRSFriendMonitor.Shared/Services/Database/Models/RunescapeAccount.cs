﻿using System;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OSRSFriendMonitor.Shared.Services.Database.Models;

public class RunescapeAccountIdentifierConverter : JsonConverter<RunescapeAccountIdentifier>
{
    public override RunescapeAccountIdentifier Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.GetString();

        if (text is null)
        {
            throw new JsonException("unable to get string value for RunescapeAccountIdentifier");
        }


        return RunescapeAccountIdentifier.FromString(text);
    }

    public override void Write(Utf8JsonWriter writer, RunescapeAccountIdentifier value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.CombinedIdentifier());
    }
}

[JsonConverter(typeof(RunescapeAccountIdentifierConverter))]
public record struct RunescapeAccountIdentifier(
    string UserId,
    string AccountHash
)
{
    public static RunescapeAccountIdentifier FromString(string value)
    {
        var userId = value[..36];
        var runescapeAccountHash = value[36..];

        return new(userId, runescapeAccountHash);
    }
    public string CombinedIdentifier()
    {
        return $"{UserId}{AccountHash}";
    }
}

public sealed record RunescapeAccount(
    [property: JsonPropertyName("id")] RunescapeAccountIdentifier AccountIdentifier,
    string DisplayName,
    IImmutableList<RunescapeAccountIdentifier> Friends,
    IImmutableList<RunescapeAccountIdentifier> FriendRequests
)
{
    public string PartitionKey => AccountIdentifier.UserId;

    public static RunescapeAccount Create(RunescapeAccountIdentifier id, string displayName) 
    {
        return new(
            id,
            displayName,
            new List<RunescapeAccountIdentifier>().ToImmutableList(),
            new List<RunescapeAccountIdentifier>().ToImmutableList()
        );
    }

    public static string DisplayNamePath() => "/DisplayName";
}
