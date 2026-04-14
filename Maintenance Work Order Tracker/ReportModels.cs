using System;

namespace MaintenanceTracker.WinForms;

public class TechSummary
{
    public string TechnicianName { get; set; } = "";
    public int TotalWorkOrders { get; set; }
    public double AvgHours { get; set; }
    public double AvgDaysToClose { get; set; }
}

public class StatusCount
{
    public string Status { get; set; } = "";
    public int Count { get; set; }
}

public class StatusPerTech
{
    public string Technician { get; set; } = "";
    public string Status { get; set; } = "";
    public int Count { get; set; }
}

public class WeeklyHours
{
    public string TechnicianName { get; set; } = "";
    public int Week { get; set; }
    public double Hours { get; set; }
}

public class TopPerf
{
    public string TechnicianName { get; set; } = "";
    public double AvgDays { get; set; }
    public int ClosedCount { get; set; }
}

public class BonusResult
{
    public DateTime? BusiestWeek { get; set; }
    public int BusiestClosed { get; set; }
    public int OverdueCount { get; set; }
}