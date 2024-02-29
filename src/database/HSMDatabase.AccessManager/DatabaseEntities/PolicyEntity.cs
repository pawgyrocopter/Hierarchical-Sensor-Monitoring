﻿using System.Collections.Generic;

namespace HSMDatabase.AccessManager.DatabaseEntities
{
    public sealed record PolicyTargetEntity(byte Type, string Value);


    public sealed record PolicyConditionEntity
    {
        public PolicyTargetEntity Target { get; init; }

        public byte Combination { get; init; }

        public byte Operation { get; init; }

        public byte Property { get; init; }
    }


    public sealed record PolicyDestinationEntity
    {
        public Dictionary<string, string> Chats { get; init; }

        public bool AllChats { get; init; }
    }


    public sealed record PolicyScheduleEntity
    {
        public long TimeTicks { get; init; }

        public byte RepeateMode { get; init; }
    }

    public sealed record PolicyFrequencyFilterEntity
    {
        public byte ActivationPeriod { get; init; }
    }


    public sealed record PolicyEntity
    {
        public List<PolicyConditionEntity> Conditions { get; init; }

        public PolicyDestinationEntity Destination { get; init; }

        public PolicyScheduleEntity Schedule { get; init; }
        
        public PolicyFrequencyFilterEntity Frequency { get; set; }


        public byte[] Id { get; init; }

        public byte SensorStatus { get; init; }

        public bool IsDisabled { get; init; }

        public string Template { get; init; }

        public string Icon { get; init; }

        public long? ConfirmationPeriod { get; init; }
    }
}