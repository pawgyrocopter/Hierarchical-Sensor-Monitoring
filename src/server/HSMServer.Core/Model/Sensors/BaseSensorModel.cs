﻿using HSMDatabase.AccessManager.DatabaseEntities;
using HSMServer.Core.Cache.UpdateEntities;
using HSMServer.Core.Model.Policies;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HSMServer.Core.Model
{
    public enum SensorState : byte
    {
        Available,
        Muted,
        Blocked = byte.MaxValue,
    }

    [Flags]
    public enum Integration : int
    {
        Grafana = 1,
    }


    public interface IBarSensor
    {
        BarBaseValue LocalLastValue { get; }
    }


    public abstract class BaseSensorModel : BaseNodeModel
    {
        public const string TimeoutComment = "#Timeout";


        private static readonly SensorResult _muteResult = new(SensorStatus.OffTime, "Muted");

        public override SensorPolicyCollection Policies { get; }


        internal abstract ValuesStorage Storage { get; }

        public abstract SensorType Type { get; }


        public Integration Integration { get; private set; }

        public DateTime? EndOfMuting { get; private set; }

        public SensorState State { get; private set; }


        public SensorResult? Status
        {
            get
            {
                if (State == SensorState.Muted)
                    return _muteResult;

                return !Policies.SensorResult.IsOk ? Policies.SensorResult : Storage.Result;
            }
        }

        public PolicyResult PolicyResult => Policies.PolicyResult;

        public bool ShouldDestroy => Settings.SelfDestroy.Value?.TimeIsUp(LastUpdate) ?? false;


        public DateTime LastUpdate => Storage.LastValue?.ReceivingTime ?? DateTime.MinValue;

        public BaseValue LastDbValue => Storage.LastDbValue;

        public BaseValue LastTimeout => Storage.LastTimeout;

        public BaseValue LastValue => Storage.LastValue;


        public bool HasData => Storage.HasData;


        public Action<SensorEntity> UpdateFromParentSettings;
        public Action<BaseValue> ReceivedNewValue;


        public BaseSensorModel(SensorEntity entity) : base(entity)
        {
            State = (SensorState)entity.State;
            Integration = (Integration)entity.Integration;
            EndOfMuting = entity.EndOfMuting > 0L ? new DateTime(entity.EndOfMuting) : null;

            Policies.Attach(this);
        }


        protected override void UpdateTTL(PolicyUpdate update) => Policies.UpdateTTL(update);

        internal abstract bool TryAddValue(BaseValue value);

        internal abstract void AddDbValue(byte[] bytes);

        internal abstract IEnumerable<BaseValue> ConvertValues(List<byte[]> valuesBytes);


        internal void Update(SensorUpdate update)
        {
            base.Update(update);

            State = UpdateProperty(State, update.State ?? State, update.Initiator);
            Integration = UpdateProperty(Integration, update.Integration ?? Integration, update.Initiator);
            EndOfMuting = UpdateProperty(EndOfMuting, update.EndOfMutingPeriod, update.Initiator, "End of muting");

            if (State == SensorState.Available)
                EndOfMuting = null;

            if (update.Policies != null)
                Policies.Update(update.Policies, update.Initiator);
        }

        internal void ResetSensor()
        {
            Policies.Reset();
            Storage.Clear();
        }

        internal SensorEntity ToEntity() => new()
        {
            Id = Id.ToString(),
            AuthorId = AuthorId.ToString(),
            ProductId = Parent.Id.ToString(),
            DisplayName = DisplayName,
            Description = Description,
            CreationDate = CreationDate.Ticks,
            Type = (byte)Type,
            State = (byte)State,
            Integration = (int)Integration,
            Policies = Policies.Ids.Select(u => u.ToString()).ToList(),
            EndOfMuting = EndOfMuting?.Ticks ?? 0L,
            Settings = Settings.ToEntity(),
            TTLPolicy = Policies.TimeToLive?.ToEntity(),
        };
    }
}