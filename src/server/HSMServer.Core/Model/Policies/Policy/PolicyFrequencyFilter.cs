using System;
using HSMDatabase.AccessManager.DatabaseEntities;
using HSMServer.Core.Cache.UpdateEntities;

namespace HSMServer.Core.Model.Policies
{
    public sealed class PolicyFrequencyFilter
    {
        public AlertRepeatMode ActivationPeriod { get; private set; }


        internal PolicyFrequencyFilter() { }

        internal PolicyFrequencyFilter(PolicyFrequencyFilterEntity entity)
        {
            if (entity is null)
                return;

            // ActivationPeriod = (AlertRepeatMode)entity.ActivationPeriod;
            ActivationPeriod = AlertRepeatMode.Test;
        }

        internal DateTime GetSendTime()
        {
            var shiftTime = ActivationPeriod switch
            {
                AlertRepeatMode.Hourly => TimeSpan.FromHours(1),
                AlertRepeatMode.Daily => TimeSpan.FromDays(1),
                AlertRepeatMode.Weekly => TimeSpan.FromDays(7),
                AlertRepeatMode.Test => TimeSpan.FromMinutes(1),
                _ => TimeSpan.FromMinutes(1)
            };

            var curTime = DateTime.UtcNow;

            return curTime.AddTicks(shiftTime.Ticks);
        }

        internal void Update(PolicyFrequencyUpdate update)
        {
            if (update is null)
                return;

            ActivationPeriod = update.ActivationPeriod ?? ActivationPeriod;
        }
        

        internal PolicyFrequencyFilterEntity ToEntity() =>
            new()
            {
                // TimeTicks = Time.Ticks,
                ActivationPeriod = (byte)ActivationPeriod,
            };

        public override string ToString() => ActivationPeriod is AlertRepeatMode.Immediately
            ? string.Empty
            : $"scheduled {ActivationPeriod}";
    }
}