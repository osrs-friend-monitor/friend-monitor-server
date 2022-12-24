﻿using OSRSFriendMonitor.Shared.Services.Database.Models;

namespace OSRSFriendMonitor.Activity.Models;

public sealed record LevelUp(
    Skill Skill, 
    int Level, 
    string AccountHash, 
    string Id, 
    long Timestamp
) : ActivityUpdate(AccountHash, Id, Timestamp);
