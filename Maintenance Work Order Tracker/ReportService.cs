using Microsoft.EntityFrameworkCore;

namespace MaintenanceTracker.WinForms;

public class ReportService
{
    // 1. Technician Summary
    public async Task<List<TechSummary>> TechnicianSummaryAsync(MaintenanceContext db)
    {
        var data = await db.Technicians
            .Include(t => t.WorkOrders)
            .ToListAsync();

        return data.Select(t => new TechSummary
        {
            TechnicianName = t.Name,
            TotalWorkOrders = t.WorkOrders.Count,
            AvgHours = t.WorkOrders.Any()
                ? t.WorkOrders.Average(w => w.HoursWorked)
                : 0,

            AvgDaysToClose = t.WorkOrders
                .Where(w => w.CompletionDate != null)
                .Select(w => (w.CompletionDate!.Value - w.RequestDate).TotalDays)
                .DefaultIfEmpty(0)
                .Average()
        }).ToList();
    }

    // 2. Status Summary
    public async Task<(List<StatusCount> overall, List<StatusPerTech> perTech)> StatusSummaryAsync(MaintenanceContext db)
    {
        var data = await db.WorkOrders
            .Include(w => w.Technician)
            .ToListAsync();

        var overall = data
            .GroupBy(w => w.Status)
            .Select(g => new StatusCount
            {
                Status = g.Key,
                Count = g.Count()
            })
            .ToList();

        var perTech = data
            .GroupBy(w => new { w.Technician!.Name, w.Status })
            .Select(g => new StatusPerTech
            {
                Technician = g.Key.Name,
                Status = g.Key.Status,
                Count = g.Count()
            })
            .ToList();

        return (overall, perTech);
    }

    // 3. Weekly Labor Hours
    public async Task<List<WeeklyHours>> WeeklyLaborAsync(MaintenanceContext db)
    {
        var data = await db.WorkOrders
            .Include(w => w.Technician)
            .ToListAsync();

        return data
            .GroupBy(w => new
            {
                w.Technician!.Name,
                Week = w.RequestDate.DayOfYear / 7
            })
            .Select(g => new WeeklyHours
            {
                TechnicianName = g.Key.Name,
                Week = g.Key.Week,
                Hours = g.Sum(x => x.HoursWorked)
            })
            .ToList();
    }

    // 4. Top Performer
    public async Task<TopPerf?> TopPerformerAsync(MaintenanceContext db, int minClosed)
    {
        var data = await db.WorkOrders
            .Include(w => w.Technician)
            .Where(w => w.Status == "Closed" && w.CompletionDate != null)
            .ToListAsync();

        return data
            .GroupBy(w => w.Technician!.Name)
            .Select(g => new TopPerf
            {
                TechnicianName = g.Key,
                ClosedCount = g.Count(),
                AvgDays = g
                    .Select(w => (w.CompletionDate!.Value - w.RequestDate).TotalDays)
                    .Average()
            })
            .Where(x => x.ClosedCount >= minClosed)
            .OrderBy(x => x.AvgDays)
            .FirstOrDefault();
    }

    // 5. Bonus Report
    public async Task<BonusResult> BonusAsync(MaintenanceContext db)
    {
        var data = await db.WorkOrders.ToListAsync();

        var busiest = data
            .GroupBy(w => w.RequestDate.DayOfYear / 7)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        var overdue = data.Count(w =>
            w.Status == "Open" &&
            (DateTime.Now - w.RequestDate).TotalDays > 7);

        return new BonusResult
        {
            BusiestWeek = busiest != null
                ? DateTime.Now.AddDays(-(DateTime.Now.DayOfYear - busiest.Key * 7))
                : null,
            BusiestClosed = busiest?.Count() ?? 0,
            OverdueCount = overdue
        };
    }
}