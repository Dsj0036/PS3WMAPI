using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;


[DebuggerDisplay("{Hours}hours, {Minutes}mins, {Seconds}seconds")]
public class Time
{
    public static readonly Time Zero = new Time(0);
    long _ms;
    int _sc;
    int _mins;
    int _hours;
    int _days;
    readonly long _createdwhen;
    public TimeSpan ToSpan()
    {
        return new TimeSpan(_days, _hours, _mins, _sc);
    }
    public long Milliseconds { get => _ms; }
    public int Seconds { get => _sc; }
    public int Minutes { get => _mins; }
    public int Hours { get => _hours; }
    public int Days { get => _days; }

    public Time GetNow()
    {
        var current = CurrentTicks();
        var elapsed = current - _createdwhen;
        return new Time(Milliseconds + elapsed);
    }
    public Time GetOriginal()
    {
        return new Time(Milliseconds);
    }
    public Time(TimeSpan raw)
    {
        _ms = raw.Ticks;
        _mins = raw.Minutes;
        _hours = raw.Hours;
        _days = raw.Days;
        _sc = raw.Seconds;
        _createdwhen = CurrentTicks();
    }
    public static Time FromString(string tm)
    {
        return new Time(TimeSpan.Parse(tm));
    }
    public static Time On(int seconds)
    {
        return new Time(seconds * 1000);
    }
    public Time(long ms)
    {
        _sc = (int)ms / 1000;
        _ms = ms;
        _hours = (int)((int)ms / 3.6e+6);
        _days = (int)((int)ms / 8.64e+7);
        _mins = (int)ms / 60000;
    }
    public new string ToString()
    {
        return new TimeSpan(_days, _hours, _mins, _sc).ToString("dd\\.hh\\:mm\\:ss");
    }
    long CurrentTicks() => Process.GetCurrentProcess().StartTime.Ticks;
}
