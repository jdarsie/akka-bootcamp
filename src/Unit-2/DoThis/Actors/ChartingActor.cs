using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms.DataVisualization.Charting;
using Akka.Actor;

namespace ChartApp.Actors
{
    public class ChartingActor : ReceiveActor
    {
        #region Nested Type: AddSeries

        public class AddSeries
        {
            public Series Series { get; }

            public AddSeries(Series series)
            {
                Series = series;
            }
        }

        #endregion

        #region Nested Type: InitializeChart

        public class InitializeChart
        {
            public Dictionary<string, Series> InitialSeries { get; }

            public InitializeChart(Dictionary<string, Series> initialSeries)
            {
                InitialSeries = initialSeries;
            }
        }

        #endregion

        #region Nested Type: RemoveSeries

        public class RemoveSeries
        {
            public string SeriesName { get; }

            public RemoveSeries(string seriesName)
            {
                SeriesName = seriesName;
            }
        }

        #endregion

        private readonly Chart _chart;
        private Dictionary<string, Series> _seriesIndex;

        /// <summary>
        /// Incrementing counter we use to plot along the X-axis
        /// </summary>
        private int xPosCounter = 0;

        public ChartingActor(Chart chart) : this(chart, new Dictionary<string, Series>())
        {
        }

        public ChartingActor(Chart chart, Dictionary<string, Series> seriesIndex)
        {
            _chart = chart;
            _seriesIndex = seriesIndex;

            Receive<InitializeChart>(ic => HandleInitialize(ic));
            Receive<AddSeries>(addSeries => HandleAddSeries(addSeries));
            Receive<RemoveSeries>(removeSeries => HandleRemoveSeries(removeSeries));
            Receive<Metric>(metric => HandleMetrics(metric));
        }

        private void HandleAddSeries(AddSeries series)
        {
            if (!string.IsNullOrEmpty(series.Series.Name) && !_seriesIndex.ContainsKey(series.Series.Name))
            {
                _seriesIndex.Add(series.Series.Name, series.Series);
                _chart.Series.Add(series.Series);

                SetChartBoundaries();
            }
        }

        private void HandleInitialize(InitializeChart ic)
        {
            if (ic.InitialSeries != null)
            {
                // swap the two series out
                _seriesIndex = ic.InitialSeries;
            }

            // delete any existing series
            _chart.Series.Clear();

            // set the axes up
            var area = _chart.ChartAreas[0];
            area.AxisX.IntervalType = DateTimeIntervalType.Number;
            area.AxisY.IntervalType = DateTimeIntervalType.Number;

            SetChartBoundaries();

            // attempt to render the initial chart
            if (_seriesIndex.Any())
            {
                foreach (var series in _seriesIndex)
                {
                    // force both the chart and the internal index to use the same names
                    series.Value.Name = series.Key;
                    _chart.Series.Add(series.Value);
                }
            }

            SetChartBoundaries();
        }

        private void HandleMetrics(Metric metric)
        {
            if (!string.IsNullOrEmpty(metric.Series) && _seriesIndex.ContainsKey(metric.Series))
            {
                var series = _seriesIndex[metric.Series];
                series.Points.AddXY(xPosCounter++, metric.CounterValue);

                while (series.Points.Count > MaxPoints)
                {
                    series.Points.RemoveAt(0);
                }

                SetChartBoundaries();
            }
        }

        private void HandleRemoveSeries(RemoveSeries series)
        {
            if (!string.IsNullOrEmpty(series.SeriesName) && _seriesIndex.ContainsKey(series.SeriesName))
            {
                var seriesToRemove = _seriesIndex[series.SeriesName];
                _seriesIndex.Remove(series.SeriesName);
                _chart.Series.Remove(seriesToRemove);

                SetChartBoundaries();
            }
        }

        private void SetChartBoundaries()
        {
            var allPoints = _seriesIndex.Values.SelectMany(series => series.Points).ToList();
            var yValues = allPoints.SelectMany(point => point.YValues).ToList();

            double maxAxisX = xPosCounter;
            double minAxisX = xPosCounter - MaxPoints;

            var maxAxisY = yValues.Count > 0 ? Math.Ceiling(yValues.Max()) : 1.0d;
            var minAxisY = yValues.Count > 0 ? Math.Floor(yValues.Min()) : 0.0d;

            if (allPoints.Count <= 2)
            {
                return;
            }

            var area = _chart.ChartAreas[0];
            area.AxisX.Minimum = minAxisX;
            area.AxisX.Maximum = maxAxisX;
            area.AxisY.Minimum = minAxisY;
            area.AxisY.Maximum = maxAxisY;
        }

        /// <summary>
        /// Maximum number of points we will allow in a series
        /// </summary>
        public const int MaxPoints = 250;
    }
}
