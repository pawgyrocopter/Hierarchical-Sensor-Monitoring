﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using HSMClientWPFControls.Objects;
using HSMClientWPFControls.ViewModel;

namespace HSMClientWPFControls
{
    public interface IMonitoringModel
    {
        public object Connector { get; }
        ObservableCollection<MonitoringNodeBase> Nodes { get; set; }
        ObservableCollection<ProductViewModel> Products { get; set; }
        void Dispose();
        void ShowProducts();
        void UpdateProducts();
        void RemoveProduct(ProductInfo product);
        ProductInfo AddProduct(string name);
    }
}
