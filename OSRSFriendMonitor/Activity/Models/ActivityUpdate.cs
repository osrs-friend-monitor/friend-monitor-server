using PolyJson;
using System.Text.Json.Serialization;

namespace OSRSFriendMonitor.Activity.Models;

[PolyJsonConverter("type")]
[PolyJsonConverter.SubType(typeof(BossKillCount), "BOSS_KILL_COUNT")]
[PolyJsonConverter.SubType(typeof(ItemDrop), "ITEM_DROP")]
[PolyJsonConverter.SubType(typeof(LevelUp), "LEVEL_UP")]
[PolyJsonConverter.SubType(typeof(LocationUpdate), "LOCATION")]
[PolyJsonConverter.SubType(typeof(QuestComplete), "QUEST_COMPLETE")]
[PolyJsonConverter.SubType(typeof(PlayerDeath), "PLAYER_DEATH")]
public abstract record ActivityUpdate(
    string AccountHash,
    string Id,
    long Timestamp
) {
    [JsonPropertyName("type")]
    public string? Discriminator => DiscriminatorValue.Get(GetType());
};
