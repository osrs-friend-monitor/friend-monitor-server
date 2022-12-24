using OSRSFriendMonitor.Services.SocketConnection.Messages;

namespace OSRSFriendMonitor.Services.SocketConnection;

public static class RunescapeAccountContextProcessor
{
    private const ulong MAX_FAST_LOCATION_UPDATE_SPEED_TICKS = 100;
    private const ulong SLOW_LOCATION_UPDATE_SPEED_TICK_THRESHOLD = 50;
    private const ulong FAST_LOCATION_UPDATE_SPEED_TICK_THRESHOLD = 3;
    public static RunescapeAccountContext ProcessContext(ulong tick, RunescapeAccountContext context)
    {
        LocationUpdateSpeed speed = context.LocationUpdateSpeed;

        if (speed == LocationUpdateSpeed.Fast && tick - context.LastLocationUpdateSpeedChangeTick > MAX_FAST_LOCATION_UPDATE_SPEED_TICKS)
        {
            speed = LocationUpdateSpeed.Slow;
        }


        return context with
        {
            LocationUpdateSpeed = speed
        };
    }

    public static bool ShouldSendLocationUpdateToClient(ulong tick, RunescapeAccountContext context)
    {
        bool shouldSend = false;
        ulong ticksSinceLocationPush = tick - context.LastLocationPushToClientTick;

        switch (context.LocationUpdateSpeed) {
            case LocationUpdateSpeed.Slow when ticksSinceLocationPush >= SLOW_LOCATION_UPDATE_SPEED_TICK_THRESHOLD:
                shouldSend = true;
                break;
            case LocationUpdateSpeed.Fast when ticksSinceLocationPush >= FAST_LOCATION_UPDATE_SPEED_TICK_THRESHOLD:
                shouldSend = true;
                break;
        }

        return shouldSend;
    }
}
