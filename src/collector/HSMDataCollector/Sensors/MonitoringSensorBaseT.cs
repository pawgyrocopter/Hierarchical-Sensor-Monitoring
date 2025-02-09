﻿using HSMDataCollector.Extensions;
using HSMDataCollector.Options;
using HSMSensorDataObjects;
using HSMSensorDataObjects.SensorValueRequests;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HSMDataCollector.DefaultSensors
{
    public abstract class MonitoringSensorBase<T> : SensorBase<T>
    {
        private Timer _sendTimer;

        protected readonly TimeSpan _receiveDataPeriod;
        protected bool _needSendValue = true;


        protected virtual TimeSpan TimerDueTime => _receiveDataPeriod;

        protected bool IsInitialized => _sendTimer != null;


        protected MonitoringSensorBase(SensorOptions options) : base(options)
        {
            if (options is IMonitoringOptions monitoringOptions)
                _receiveDataPeriod = monitoringOptions.PostDataPeriod;
            else
                throw new ArgumentNullException(nameof(monitoringOptions));
        }


        internal override async Task<bool> Init()
        {
            if (!IsInitialized)
            {
                var baseInit = await base.Init();

                if (baseInit)
                    _sendTimer = new Timer(OnTimerTick, null, TimerDueTime, _receiveDataPeriod);
            }

            return IsInitialized;
        }

        internal override Task Stop()
        {
            if (!IsInitialized)
                return Task.FromResult(false);

            _sendTimer?.Dispose();
            _sendTimer = null;

            return Task.CompletedTask;
        }


        protected abstract T GetValue();


        protected virtual string GetComment() => null;

        protected virtual T GetDefaultValue() => default;

        protected virtual SensorStatus GetStatus() => SensorStatus.Ok;


        protected void OnTimerTick(object _ = null)
        {
            var value = BuildSensorValue();

            if (_needSendValue)
                SendValue(value);
        }

        protected virtual SensorValueBase BuildSensorValue()
        {
            try
            {
                return GetSensorValue(GetValue()).Complete(GetComment(), GetStatus());
            }
            catch (Exception ex)
            {
                ThrowException(ex);

                return GetSensorValue(GetDefaultValue()).Complete(ex.Message, SensorStatus.Error);
            }
        }
    }
}
