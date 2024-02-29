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

            ActivationPeriod = (AlertRepeatMode)entity.ActivationPeriod;
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