﻿using HSMDataCollector.Core;
using HSMDataCollector.DefaultSensors.Unix;
using HSMDataCollector.Options;
using HSMDataCollector.PublicInterface;
using System;

namespace HSMDataCollector.DefaultSensors
{
    internal sealed class UnixSensorsCollection : DefaultSensorsCollection, IUnixCollection
    {
        protected override bool IsCorrectOs => !DataCollector.IsWindowsOS;


        internal UnixSensorsCollection(SensorsStorage storage, PrototypesCollection prototype) : base(storage, prototype) { }


        public IUnixCollection AddAllComputer() =>
            (this as IUnixCollection).AddSystemMonitoringSensors().AddDiskMonitoringSensors();

        public IUnixCollection AddAllModule(Version productVersion)
        {
            var moduleCollection = (this as IUnixCollection).AddProcessMonitoringSensors()
                                                            .AddCollectorMonitoringSensors();

            if (productVersion != null)
            {
                var versionOptions = new VersionSensorOptions(productVersion) { Version = productVersion };

                moduleCollection.AddProductVersion(versionOptions);
            }

            return moduleCollection;
        }

        public IUnixCollection AddAllCollection(Version productVersion) => AddAllComputer().AddAllModule(productVersion);




        #region Process

        public IUnixCollection AddProcessCpu(BarSensorOptions options)
        {
            return ToUnix(new UnixProcessCpu(_prototype.ProcessCpu.Get(options)));
        }

        public IUnixCollection AddProcessMemory(BarSensorOptions options)
        {
            return ToUnix(new UnixProcessMemory(_prototype.ProcessMemory.Get(options)));
        }

        public IUnixCollection AddProcessThreadCount(BarSensorOptions options)
        {
            return ToUnix(new UnixProcessThreadCount(_prototype.ProcessThreadCount.Get(options)));
        }

        public IUnixCollection AddProcessMonitoringSensors(BarSensorOptions options) =>
            AddProcessCpu(options).AddProcessMemory(options).AddProcessThreadCount(options);

        #endregion


        #region System

        public IUnixCollection AddTotalCpu(BarSensorOptions options)
        {
            return ToUnix(new UnixTotalCpu(_prototype.TotalCPU.Get(options)));
        }

        public IUnixCollection AddFreeRamMemory(BarSensorOptions options)
        {
            return ToUnix(new UnixFreeRamMemory(_prototype.FreeRam.Get(options)));
        }

        public IUnixCollection AddSystemMonitoringSensors(BarSensorOptions options) =>
            AddFreeRamMemory(options).AddTotalCpu(options);

        #endregion


        #region Disk

        public IUnixCollection AddFreeDiskSpace(DiskSensorOptions options)
        {
            return ToUnix(new UnixFreeDiskSpace(_prototype.UnixFreeSpaceOnDisk.Get(options)));
        }

        public IUnixCollection AddFreeDiskSpacePrediction(DiskSensorOptions options)
        {
            return ToUnix(new UnixFreeDiskSpacePrediction(_prototype.UnixFreeSpaceOnDiskPrediction.Get(options)));
        }

        public IUnixCollection AddDiskMonitoringSensors(DiskSensorOptions options) =>
            AddFreeDiskSpace(options).AddFreeDiskSpacePrediction(options);

        #endregion


        #region Collector

        public IUnixCollection AddCollectorAlive(CollectorMonitoringInfoOptions options) => (IUnixCollection)AddCollectorAliveCommon(options);

        public IUnixCollection AddCollectorVersion() => (IUnixCollection)AddCollectorVersionCommon();

        public IUnixCollection AddCollectorErrors() => (IUnixCollection)AddCollectorErrorsCommon();

        public IUnixCollection AddCollectorMonitoringSensors(CollectorMonitoringInfoOptions options) => (IUnixCollection)AddFullCollectorMonitoringCommon(options);

        #endregion


        #region Diagnostic

        public IUnixCollection AddQueuePackageProcessTime(BarSensorOptions options = null)
        {
            throw new System.NotImplementedException();
        }

        public IUnixCollection AddQueuePackageContentSize(InstantSensorOptions options = null) => (IUnixCollection)AddPackageSizeCommon(options);

        public IUnixCollection AddQueuePackageValuesCount(BarSensorOptions options = null) => (IUnixCollection)AddPackageValuesCountCommon(options);

        public IUnixCollection AddQueueOverflow(BarSensorOptions options = null) => (IUnixCollection)AddQueueOverflowCommon(options);

        public IUnixCollection AddAllQueueDiagnosticSensors(InstantSensorOptions instantOptions = null, BarSensorOptions barOptions = null) =>
            AddQueueOverflow(barOptions).AddQueuePackageValuesCount(barOptions).AddQueuePackageContentSize(instantOptions);

        #endregion


        public IUnixCollection AddProductVersion(VersionSensorOptions options) => (IUnixCollection)AddProductVersionCommon(options);


        private UnixSensorsCollection ToUnix(SensorBase sensor) => (UnixSensorsCollection)Register(sensor);
    }
}
