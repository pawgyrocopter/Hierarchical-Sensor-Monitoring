﻿using System;
using System.Collections.Generic;

namespace HSMDataCollector.SyncQueue
{
    public interface ISyncQueue<T>
    {
        event Action<List<T>> NewValuesEvent;
        event Action<T> NewValueEvent;

        event Action<string, int> OverflowCntEvent;


        void Push(T value);

        void PushFailValue(T value);
    }
}