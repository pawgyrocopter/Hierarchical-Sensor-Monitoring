﻿using System;
using System.Globalization;
using System.Net;

namespace HSMDataCollector.Base
{
    public abstract class SensorBase : ISensor
    {
        protected readonly string Path;
        protected readonly string ProductKey;
        protected readonly string ServerAddress;
        private int _count = 0;
        protected SensorBase(string path, string productKey, string serverAddress)
        {
            Path = path;
            ProductKey = productKey;
            ServerAddress = serverAddress;
        }

        public abstract void AddValue(object value);
        protected abstract byte[] GetBytesData(object data);
        protected void SendData(object data)
        {
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            try
            {
                ++_count;
                Console.WriteLine($"Send data for {Path} at {_count} time...");
                //Console.WriteLine($"Sending the data... Current time = {DateTime.Now.ToShortDateString()}:{DateTime.Now.ToLongTimeString()}");
                //Console.WriteLine($"Sensor path = '{Path}'");
                var request = (HttpWebRequest)WebRequest.Create(new Uri(ServerAddress));
                request.ContentType = "application/json";
                request.Method = "POST";
                request.KeepAlive = false;
                byte[] dataBytes = GetBytesData(data);
                using (var reqStream = request.GetRequestStream())
                {
                    reqStream.Write(dataBytes, 0, dataBytes.Length);
                }

                var response = request.GetResponse();
                response.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
