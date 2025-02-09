﻿using HSMDataCollector.Core;
using HSMDataCollector.Extensions;
using HSMSensorDataObjects.SensorValueRequests;
using System.Collections.Generic;

namespace HSMDataCollector.SyncQueue
{
    internal class SensorDataQueue : SyncQueue<SensorValueBase>, IValuesQueue
    {
        private readonly HashSet<string> _prioritySensors = new HashSet<string>();


        protected override string QueueName => "Sensor data";


        public SensorDataQueue(CollectorOptions options) : base(options, options.PackageCollectPeriod) { }


        public override void Push(SensorValueBase value) => Enqueue(_valuesQueue, value.TrimLongComment());


        protected override bool IsSendValue(SensorValueBase value)
        {
            switch (value)
            {
                case FileSensorValue fileValue:
                    InvokeNewValue(fileValue);
                    return false;

                case BarSensorValueBase barSensor when barSensor.Count == 0:
                    return false;
            }

            if (_prioritySensors.Contains(value.Path))
            {
                InvokeNewValue(value);
                return false;
            }

            return true;
        }

        public void AddPrioritySensor(string path) => _prioritySensors.Add(path);
    }
}