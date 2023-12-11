﻿using HSMDataCollector.Alerts;
using HSMDataCollector.DefaultSensors.Windows;
using HSMSensorDataObjects.SensorRequests;
using System;
using System.Collections.Generic;

namespace HSMDataCollector.Prototypes.Collections.Disks
{
    internal sealed class WindowsDiskQueueLengthPrototype : BarDisksMonitoringPrototype
    {
        protected override string SensorNameTemplate => "Disk queue length on {0} disk";

        protected override string DescriptionPath => WindowsDiskQueueLength.Counter;


        public WindowsDiskQueueLengthPrototype() : base()
        {
            SensorUnit = Unit.Seconds;

            Alerts = new List<BarAlertTemplate>()
            {
                AlertsFactory.IfEmaMean(AlertOperation.GreaterThanOrEqual, 100)
                             .AndConfirmationPeriod(TimeSpan.FromMinutes(5))
                             .ThenSendNotification($"[$product]$path $property $operation $target {SensorUnit}")
                             .AndSetIcon(AlertIcon.Warning).Build()
            };
        }
    }
}