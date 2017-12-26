using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DianaDevLibSample
{
    class PlotDataConnector
    {
        private static PlotDataConnector instance;
        private static object syncRoot = new Object();
        private PlotDataConnector() {}

        public UInt16[] DATA_PACKAGE { get; set; }

        public static PlotDataConnector getInstance()
        {
            if (instance == null)
            {
                if (instance == null)
                    instance = new PlotDataConnector();
            }
            return instance;
        }
    }
}