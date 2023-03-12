using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace HKineticCurve;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly H2ViewModel _h2ViewModel;

    public MainWindow()
    {
        InitializeComponent();
        _h2ViewModel = new H2ViewModel();
        DataContext = _h2ViewModel;
    }

    private void WpfPlot1_OnMouseMove(object sender, MouseEventArgs e)
    {

    }

    private void PlotTemp(object sender, RoutedEventArgs e)
    {
        try
        {
            WpfPlot1.Plot.Clear();
            var plt = WpfPlot1.Plot;
            double[] dataX = _h2ViewModel.Records.Select(o => o.DateTime.ToOADate()).ToArray();
            double[] dataY = _h2ViewModel.Records.Select(o => o.Temp).ToArray();

            if (dataX.Length == 0 || dataY.Length == 0)
            {
                MessageBox.Show("No data loaded");
                return;
            }

            plt.XAxis.DateTimeFormat(true);
            plt.AddScatter(dataX, dataY);

            plt.Title("Date/Temp");
            plt.XLabel("Date");
            plt.YLabel("Temp");

            WpfPlot1.Refresh();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message);
        }

    }

    private void PlotPressure(object sender, RoutedEventArgs e)
    {
        try
        {
            WpfPlot1.Plot.Clear();
            var plt = WpfPlot1.Plot;
            double[] dataX = _h2ViewModel.Records.Select(o => o.DateTime.ToOADate()).ToArray();
            double[] dataY = _h2ViewModel.Records.Select(o => o.Pressure).ToArray();

            if (dataX.Length == 0 || dataY.Length == 0)
            {
                MessageBox.Show("No data loaded");
                return;
            }

            plt.XAxis.DateTimeFormat(true);
            plt.AddScatter(dataX, dataY);

            plt.Title("Date/Pressure");
            plt.XLabel("Date");
            plt.YLabel("Pressure");

            WpfPlot1.Refresh();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message);
        }

    }


    private void PlotThresholds(object sender, RoutedEventArgs e)
    {
        try
        {
            var recordsLists = _h2ViewModel.SplitRecordsByThresholds();

            WpfPlot1.Plot.Clear();
            var plt = WpfPlot1.Plot;
            plt.XAxis.DateTimeFormat(true);

            foreach (var recordsList in recordsLists)
            {
                double[] dataX = recordsList.Select(o => o.DateTime.ToOADate()).ToArray();
                double[] dataY = recordsList.Select(o => o.Pressure).ToArray();
                plt.AddScatter(dataX, dataY);
            }

            plt.Title("Date/Pressure");
            plt.XLabel("Date");
            plt.YLabel("Pressure");

            WpfPlot1.Refresh();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message);
        }
    }

    private void PlotResults(object sender, RoutedEventArgs e)
    {
        try
        {
            WpfPlot1.Plot.Clear();
            var plt = WpfPlot1.Plot;

            if (_h2ViewModel.HRecords.Count == 0)
            {
                MessageBox.Show("H Records not loaded");
                return;
            }

            var maxDiff = TimeSpan.FromSeconds(29);
            var recordsLists = _h2ViewModel.SplitRecordsByThresholds();
            foreach (var records in recordsLists)
            {
                List<double> xValues = new List<double>(records.Count);
                List<double> yValues = new List<double>(records.Count);

                double ySum = 0;
                for (var index = 0; index < records.Count - 1; index++)
                {
                    var record_t0 = records[index];
                    var record_t1 = records[index + 1];

                    //var t_0 = record_t0.DateTime;
                    var P_1 = record_t0.Pressure;
                    var P_2 = record_t1.Pressure;
                    var T_Res = record_t1.Temp;

                    var closestHRecord = H2ViewModel.FindClosestRecord(_h2ViewModel.HRecords, record_t1.DateTime, maxDiff);

                    if (closestHRecord == null)
                    {
                        continue;
                    }

                    var T_AC = closestHRecord.Temp;

                    var x = (record_t1.DateTime - records[1].DateTime).TotalSeconds;
                    xValues.Add(x);

                    var y = _h2ViewModel.Calc(P_1, P_2, T_Res, T_AC);
                    ySum += y;
                    yValues.Add(ySum);

                }
                //(double[] smoothXs, double[] smoothYs) = ScottPlot.Statistics.Interpolation.Cubic.InterpolateXY(xValues.ToArray(), yValues.ToArray(), 2048);
                //plt.AddScatterLines(smoothXs, smoothYs);
                plt.AddScatter(xValues.ToArray(), yValues.ToArray());
            }

            plt.Title("Date/Res");
            plt.XLabel("Seconds");
            plt.YLabel("Res");

            WpfPlot1.Refresh();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message);
        }
    }
}