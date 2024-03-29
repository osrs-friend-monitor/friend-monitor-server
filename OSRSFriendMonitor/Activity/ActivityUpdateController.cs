﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OSRSFriendMonitor.Activity.Models;
using OSRSFriendMonitor.Services;
using DatabaseModels = OSRSFriendMonitor.Shared.Services.Database.Models;
using System.Security.Claims;

namespace OSRSFriendMonitor.Activity;


[Route("api/activity")]
[ApiController]
[Authorize]
public class ActivityUpdateController : ControllerBase
{
    private readonly IActivityProcessor _activityProcessor;
    public ActivityUpdateController(IActivityProcessor activityProcessor)
    {
        _activityProcessor = activityProcessor;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ActivityUpdate update)
    {
        string userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        DatabaseModels.ActivityUpdate convertedUpdate = update.ConvertToDatabaseModel(update.AccountHash);
        await _activityProcessor.ProcessActivityAsync(convertedUpdate, userId);

        return Ok();
    }
}

public static class ActivityUpdateExtensions
{
    public static DatabaseModels.ActivityUpdate ConvertToDatabaseModel(this ActivityUpdate activityUpdate, long accountHash)
    {
        DateTime activityDateTime = DateTimeOffset.FromUnixTimeMilliseconds(activityUpdate.Timestamp).DateTime;

        if (activityUpdate is LocationUpdate locationUpdate)
        {
            return new DatabaseModels.LocationUpdate(
                X: locationUpdate.X,
                Y: locationUpdate.Y,
                Plane: locationUpdate.Plane,
                Id: activityUpdate.Id,
                World: locationUpdate.World,
                AccountHash: accountHash,
                Time: activityDateTime
            );
        }
        else if (activityUpdate is PlayerDeath playerDeath)
        {
            return new DatabaseModels.PlayerDeath(
                X: playerDeath.X,
                Y: playerDeath.Y,
                Plane: playerDeath.Plane,
                Id: activityUpdate.Id,
                World: playerDeath.World,
                AccountHash: accountHash,
                Time: activityDateTime
            );
        }
        else if (activityUpdate is LevelUp levelUp)
        {
            return new DatabaseModels.LevelUp(
                levelUp.Skill,
                levelUp.Level,
                activityUpdate.Id,
                AccountHash: accountHash,
                activityDateTime
            );
        }
        else if (activityUpdate is QuestComplete questComplete)
        {
            throw new NotImplementedException("quest complete not implemented yet");
        }
        else if (activityUpdate is BossKillCount bossKillCount)
        {
            throw new NotImplementedException("boss kill count not implemented yet");
        }
        else if (activityUpdate is ItemDrop itemDrop)
        {
            throw new NotImplementedException("item drop not implemented yet");
        }
        else
        {
            throw new NotImplementedException($"unknown activity update {activityUpdate.GetType()}");
        }
    }
}