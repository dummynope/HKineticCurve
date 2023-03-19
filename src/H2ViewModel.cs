using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Aspects.Notify;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace HKineticCurve;

public class H2ViewModel : ObservableObject
{
    [Notify] public string? RecordName { get; set; }
    [Notify] public List<Record> Records { get; set; } = new();

    [Notify] public string? HRecordName { get; set; }
    [Notify] public List<Record> HRecords { get; set; } = new();
    [Notify] public int SkippedRecords { get; set; }

    [Notify] public DateTime MinDateTime { get; set; }
    [Notify] public DateTime MaxDateTime { get; set; }
    [Notify] public double MinTemp { get; set; }
    [Notify] public double MaxTemp { get; set; }
    [Notify] public double MinPressure { get; set; }
    [Notify] public double MaxPressure { get; set; }

    public double PressureThresholdMin { get; set; }
    public double PressureThresholdMax { get; set; } = 1.55;

    public H2ViewModel()
    {
        LoadDataCommand = new RelayCommand(LoadData);
        LoadHDataCommand = new RelayCommand(LoadHData);
    }

    private void LoadData()
    {
        var loadedData = LoadRecords();
        var records = loadedData.Records;
        MinDateTime = records.Min(o => o.DateTime);
        MaxDateTime = records.Max(o => o.DateTime);
        MinPressure = records.Min(o => o.Pressure);
        MaxPressure = records.Max(o => o.Pressure);
        MinTemp = records.Min(o => o.Temp);
        MaxTemp = records.Max(o => o.Temp);

        SkippedRecords = loadedData.Skipped;
        Records = records;
        RecordName = Path.GetFileName(loadedData.filename);
    }

    private void LoadHData()
    {
        var res = LoadRecords();
        HRecords = res.Records;
        HRecordName = Path.GetFileName(res.filename);
    }

    public const double KelvinOffset = 273.15;
    public const double BarToPA = 1E5;//10^5

    [Notify] public double R_H2 { get; set; } = 4.124; //%[J/g*K]
    [Notify] public double mass_MgFe { get; set; } = 1.0; //%[g]
    [Notify] public double vol_AC { get; set; } = 0.094; //%[l]
    [Notify] public double vol_Res { get; set; } = 0.497; //%[l]

    [Notify] public double TempThreshold { get; set; }
    public double Calc(double P_1, double P_2, double T_Res, double T_AC)
    {
        var deltPres_dehy = (P_2 * BarToPA) - (P_1 * BarToPA);// * 1E5;//10^5;

        var desorped_H2 = deltPres_dehy / R_H2 *
                          ((0.001 * vol_AC / (T_AC + KelvinOffset)) +
                           (0.001 * vol_Res / (T_Res + KelvinOffset))); //%[g]
        var capacity_H2_des = 100 * (desorped_H2 / (mass_MgFe + desorped_H2)); //%[%]
        return capacity_H2_des;
    }

    private (List<Record> Records, int Skipped, string filename) LoadRecords()
    {
        try
        {
            // Configure open file dialog box
            var dialog = new OpenFileDialog();
            dialog.FileName = "Document"; // Default file name
            dialog.DefaultExt = ".csv"; // Default file extension
            dialog.Filter = "Text documents (.csv)|*.csv"; // Filter files by extension

            // Show open file dialog box
            var result = dialog.ShowDialog();

            var hasHeader = true;
            var headerSkipped = false;
            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                var filename = dialog.FileName;
                var lines = File.ReadAllLines(filename);

                List<Record> records = new(lines.Length);
                int skipped = 0;

                foreach (var line in lines)
                {
                    var values = line.Split(',');

                    try
                    {
                        if (values.Length == 5)
                        {
                            if (values[4] == "?")
                            {
                                skipped++;
                                continue;
                            }

                            //Header überspringen
                            if (hasHeader && !headerSkipped)
                            {
                                headerSkipped = true;
                                continue;
                            }

                            var dtStrings = values[0].Split(';');

                            var dt = DateTime.Parse(dtStrings[0]);
                            var ts = TimeSpan.Parse(dtStrings[1]);

                            var record = new Record()
                            {
                                Date = dt.Date,
                                TimeSpan = ts,
                                DateTime = dt.Add(ts),
                                Temp = double.Parse(values[3], CultureInfo.InvariantCulture),
                                Pressure = double.Parse(values[4], CultureInfo.InvariantCulture),
                            };


                            records.Add(record);
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message);
                        throw;
                    }
                }

                return (records, skipped, dialog.FileName);
            }
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message);
        }

        return (new List<Record>(), 0, string.Empty);
    }

    public ICommand LoadDataCommand { get; }
    public ICommand LoadHDataCommand { get; }


    public static Record? FindClosestRecord(List<Record> records, DateTime query, TimeSpan maxDiff)
    {
        Record? closest = null;
        long min = long.MaxValue;
        var queryTicks = query.Ticks;
        foreach (var record in records)
        {
            var diff = Math.Abs(record.DateTime.Ticks - queryTicks);

            if (diff < min)
            {
                min = diff;
                closest = record;
            }
            else if (diff > min)
            {
                break;
            }
        }

        if (closest != null && min > maxDiff.Ticks)
        {
            return null;
        }
        return closest;
    }

    public List<List<Record>> SplitRecordsByThresholds()
    {
        List<List<Record>> recordsLists = new();
        List<Record> current = new List<Record>();

        //Teilabschnitte finden
        foreach (var record in Records)
        {
            if (record.Pressure >= PressureThresholdMin &&
                record.Pressure <= PressureThresholdMax)
            {
                current.Add(record);
            }
            else
            {
                if (current.Any())
                {
                    recordsLists.Add(current);
                    current = new List<Record>();
                }
            }
        }

        if (current.Any())
        {
            recordsLists.Add(current);
        }

        //Erst bei Minimum anfangen
        for (var index = 0; index < recordsLists.Count; index++)
        {
            var recordsList = recordsLists[index];
            var min = recordsList.Min(o => o.Pressure);
            int minStartIndex = recordsList.FindIndex(o => o.Pressure == min);
            recordsLists[index] = recordsList.Skip(minStartIndex).ToList();
        }

        return recordsLists;
    }
}