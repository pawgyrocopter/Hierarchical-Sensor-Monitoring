﻿using HSMClientWPFControls.ViewModel;

namespace HSMClientWPFControls
{
    public interface IMonitoringSensorStatusHandler
    {
        void UpdateStatus(MonitoringSensorBaseViewModel sensor);
    }
}
