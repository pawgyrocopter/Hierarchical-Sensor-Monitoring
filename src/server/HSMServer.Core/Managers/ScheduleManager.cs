﻿using HSMCommon.Collections;
using HSMCommon.Extensions;
using System;

namespace HSMServer.Core.Managers
{
    internal sealed class ScheduleManager : BaseTimeManager
    {
        private readonly CTimeDict<CTimeDict<ScheduleAlertMessage>> _storage = new();
        private readonly TimeSpan _grouppingPeriod = TimeSpan.FromHours(1);


        internal void ProcessMessage(AlertMessage message)
        {
            var sensorId = message.SensorId;
            var utcTime = DateTime.UtcNow;

            var (notApplyAlerts, applyAlerts) = message.Alerts.SplitByCondition(u => u.SendTime <= utcTime);

            SendAlertMessage(sensorId, notApplyAlerts);

            foreach (var alert in applyAlerts)
            {
                var grouppingDate = alert.BuildDate.Floor(_grouppingPeriod);
                var grouppedAlerts = _storage[alert.SendTime];

                if (!grouppedAlerts.ContainsKey(grouppingDate))
                    grouppedAlerts.TryAdd(grouppingDate, new ScheduleAlertMessage(sensorId, alert.BuildDate));

                grouppedAlerts[grouppingDate].Alerts.Add(alert);
            }
        }


        internal override void FlushMessages()
        {
            foreach (var (sendTime, branch) in _storage)
                if (sendTime < DateTime.UtcNow && _storage.TryRemove(sendTime, out _))
                {
                    foreach (var (_, message) in branch)
                        SendAlertMessage(message);

                    branch.Clear();
                }
        }
    }
}