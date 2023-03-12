using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HKineticCurve;

public class Record : ObservableObject
{
    public required DateTime Date { get; set; }
    public required TimeSpan TimeSpan { get; set; }

    private readonly DateTime _dateTime;
    public required DateTime DateTime
    {
        get => _dateTime;
        init => SetProperty(ref _dateTime, value);
    }

    private readonly double _temp;
    public required double Temp
    {
        get => _temp;
        init => SetProperty(ref _temp, value);
    }

    private readonly double _pressure;
    public required double Pressure
    {
        get => _pressure;
        init => SetProperty(ref _pressure, value);
    }
}