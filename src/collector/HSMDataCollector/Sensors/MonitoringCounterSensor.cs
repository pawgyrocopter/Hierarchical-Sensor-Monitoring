using HSMDataCollector.DefaultSensors;
using HSMDataCollector.Options;
using HSMDataCollector.PublicInterface;
using HSMSensorDataObjects;
using System.Threading;

namespace HSMDataCollector.Sensors
{
    internal sealed class MonitoringCounterSensor : MonitoringSensorBase<double>, IMonitoringCounterSensor
    {
        private SensorStatus _lastStatus = SensorStatus.Ok;
        private string _lastComment = string.Empty;
        private double _sum = 0.0;


        public MonitoringCounterSensor(CounterSensorOptions options) : base(options) { }


        protected override double GetValue()
        {
            var sec = _receiveDataPeriod.TotalSeconds;
            var value = sec > 0 ? _sum / sec : 0;

            Interlocked.Exchange(ref _sum, 0d);

            _needSendValue = value > 0.0;

            return value;
        }


        protected override SensorStatus GetStatus() => _lastStatus;

        protected override string GetComment() => _lastComment;


        public void AddValue(double value) => AddValue(value, SensorStatus.Ok, string.Empty);

        public void AddValue(double value, string comment = "") => AddValue(value, SensorStatus.Ok, comment);

        public void AddValue(double value, SensorStatus status = SensorStatus.Ok, string comment = "")
        {
            Interlocked.Exchange(ref _sum, _sum + value);

            _lastComment = comment;
            _lastStatus = status;
        }
    }
}