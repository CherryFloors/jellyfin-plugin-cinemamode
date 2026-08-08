using System;

namespace Jellyfin.Plugin.CinemaMode.Tests.Fixtures
{
    public class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTime localDateTime)
        {
            _utcNow = new DateTimeOffset(localDateTime, TimeZoneInfo.Local.GetUtcOffset(localDateTime));
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}
