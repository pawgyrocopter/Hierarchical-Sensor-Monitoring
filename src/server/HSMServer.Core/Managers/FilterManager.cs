using System;
using HSMCommon.Collections;
using HSMCommon.Extensions;

namespace HSMServer.Core.Managers;

internal class FilterManager : BaseTimeManager
{
    private readonly CTimeDict<CDict<ScheduleAlertMessage>> _storage = new();


    internal void ProcessMessage(AlertMessage message)
    {
        var sensorId = message.SensorId;

        var (notApplyAlerts, applyAlerts) = message.SplitByCondition(u => u.IsFilterAlert);

        SendAlertMessage(sensorId, notApplyAlerts);
    
        foreach (var alert in applyAlerts)
        {
            var grouppedAlerts = _storage[alert.FrequenceTime];

            if (!grouppedAlerts.TryGetValue(sensorId, out var sensorGroup))
            {
                sensorGroup = new ScheduleAlertMessage(sensorId);
                grouppedAlerts.TryAdd(sensorId, sensorGroup);
            }

            if (alert.IsReplaceAlert)
                sensorGroup.RemovePolicyAlerts(alert.PolicyId);

            sensorGroup.AddAlert(alert);
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