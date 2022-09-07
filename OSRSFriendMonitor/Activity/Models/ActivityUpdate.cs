using PolyJson;

namespace OSRSFriendMonitor.Activity.Models;

[PolyJsonConverter("type")]
[PolyJsonConverter.SubType(typeof(BossKillCount), "BOSS_KILL_COUNT")]
[PolyJsonConverter.SubType(typeof(ItemDrop), "ITEM_DROP")]
[PolyJsonConverter.SubType(typeof(LevelUp), "LEVEL_UP")]
[PolyJsonConverter.SubType(typeof(LocationUpdate), "LOCATION")]
[PolyJsonConverter.SubType(typeof(QuestComplete), "QUEST_COMPLETE")]
public abstract record ActivityUpdate(
    int AccountHash,
    long Timestamp
) {
    public string? Discriminator => DiscriminatorValue.Get(GetType());
};
