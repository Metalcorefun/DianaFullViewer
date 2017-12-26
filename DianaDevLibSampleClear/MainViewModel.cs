using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;

using static DianaDevLibSample.DianaDevLibDLL;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace DianaDevLibSample
{
    public struct PlotModelParameter
    {
        public UInt16 _minValue { get; set; }
        public UInt16 _maxValue { get; set; }
        public bool _isAvailable { get; set; }
    }

    public class MainViewModel
    {
        private const int numberOfSeries = 1;
        private const int numberOfModels = 9;
        private const int UpdateInterval = 40;

        private PlotDataConnector pdc;
        private readonly Timer timer;
        private bool disposed;
        private readonly Stopwatch watch = new Stopwatch();

        public MainViewModel()
        {
            this.timer = new Timer(OnTimerElapsed);

            this.Prefs = new PlotModelParameter[numberOfModels];
            this.pdc = PlotDataConnector.getInstance();
            SetupModel();
        }

        private void initializePrefs()
        {
            for(int i = 0; i < numberOfModels; i++)
            {
                Prefs[i]._isAvailable = true;
                Prefs[i]._minValue = 0;
                Prefs[i]._maxValue = 16000;

                PlotModels[i].Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = Prefs[i]._minValue, Maximum = Prefs[i]._maxValue, IsZoomEnabled = false, IsPanEnabled = false });
            }
        }

        public void updatePrefs(PlotModelParameter[] temp)
        {
            Prefs = temp;

            for(int i = 0; i < numberOfModels; i++)
            {
                PlotModels[i].Axes[0].Minimum = Prefs[i]._minValue;
                PlotModels[i].Axes[0].Maximum = Prefs[i]._maxValue;
            }
        }

        private void generatePlotModels()
        {
            PlotModels = new List<PlotModel>();
            for (int i = 0; i < numberOfModels; i++)
            {
                PlotModel pl = new PlotModel();
                switch (i)
                {
                    case CH_OPTIONAL_INDEX:
                        pl.Title = "Доп. канал";
                        pl.Series.Add(new LineSeries { LineStyle = LineStyle.Solid, Color = OxyColors.Black });
                        break;

                    case CH_EDA_INDEX:
                        pl.Title = "КГР";
                        pl.Series.Add(new LineSeries { LineStyle = LineStyle.Solid, Color = OxyColors.DarkRed });
                        break;

                    case CH_TR_INDEX:
                        pl.Title = "Верхний датчик дыхания";
                        pl.Series.Add(new LineSeries { LineStyle = LineStyle.Solid, Color = OxyColors.LightGreen });
                        break;

                    case CH_AR_INDEX:
                        pl.Title = "Нижний датчик дыхания";
                        pl.Series.Add(new LineSeries { LineStyle = LineStyle.Solid, Color = OxyColors.DarkGreen });
                        break;

                    case CH_PLE_INDEX:
                        pl.Title = "Плетизмограмма";
                        pl.Series.Add(new LineSeries { LineStyle = LineStyle.Solid, Color = OxyColors.Indigo });
                        break;

                    case CH_TREMOR_INDEX:
                        pl.Title = "Тремор";
                        pl.Series.Add(new LineSeries { LineStyle = LineStyle.Solid, Color = OxyColors.Purple });
                        break;

                    case CH_BV_INDEX:
                        pl.Title = "Артериальное давление(АД)";
                        pl.Series.Add(new LineSeries { LineStyle = LineStyle.Solid, Color = OxyColors.Yellow });
                        break;

                    case CH_TEDA_INDEX:
                        pl.Title = "Тоническая составляющая КГР";
                        pl.Series.Add(new LineSeries { LineStyle = LineStyle.Solid, Color = OxyColors.DarkRed });
                        break;

                    case CH_ABSBV_INDEX:
                        pl.Title = "Абс. значение АД";
                        pl.Series.Add(new LineSeries { LineStyle = LineStyle.Solid, Color = OxyColors.Peru});
                        break;
                }
                PlotModels.Add(pl);
            }
        }
        private void SetupModel()
        {
            this.timer.Change(Timeout.Infinite, Timeout.Infinite);

            generatePlotModels();
            initializePrefs();

            this.watch.Start();
            this.RaisePropertyChanged("PlotModel");
            this.timer.Change(1000, UpdateInterval);
        }

        private void OnTimerElapsed(object state)
        {
            for(int i = 0; i < numberOfModels; i++)
            {
                lock (this.PlotModels[i].SyncRoot)
                {
                    this.UpdateData(i);
                }
                this.PlotModels[i].InvalidatePlot(true);
            }
        }

        public int TotalNumberOfPoints { get; private set; }

        public void UpdateData(int INDEX)
        {
            double t = this.watch.ElapsedMilliseconds * 0.001;
            int n = 0;

            for (int i = 0; i < PlotModels[INDEX].Series.Count; i++)
            {
                var s = (LineSeries)PlotModels[INDEX].Series[i];

                double x = s.Points.Count > 0 ? s.Points[s.Points.Count - 1].X + 1 : 0;
                if (s.Points.Count >= 200)
                    s.Points.RemoveAt(0);

                double y = 7000;
                if (pdc.DATA_PACKAGE != null)
                {
                    y = pdc.DATA_PACKAGE[INDEX];
                }
                if (Prefs[INDEX]._isAvailable)
                { s.Points.Add(new DataPoint(x, y)); }

                n += s.Points.Count;
            }
            if (this.TotalNumberOfPoints != n)
            {
                this.TotalNumberOfPoints = n;
                this.RaisePropertyChanged("TotalNumberOfPoints");
            }

        }

        public IList<PlotModel> PlotModels { get; private set; }
        public PlotModelParameter[] Prefs { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string property)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.timer.Dispose();
                }
            }
            this.disposed = true;
        }
    }
}
