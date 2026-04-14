using Microsoft.EntityFrameworkCore;

namespace MaintenanceTracker.WinForms;

public class ReportService
{
    // 1. Technician Summary
    public async Task<List<TechSummary>> TechnicianSummaryAsync(MaintenanceContext db)
    {
        return await db.Technicians
            .Select(t => new TechSummary
            {
                TechnicianName = t.Name,
                TotalWorkOrders = t.WorkOrders.Count,
                AvgHours = t.WorkOrders.Any()
                    ? t.WorkOrders.Average(w => w.HoursWorked)
                    : 0,

                AvgDaysToClose = t.WorkOrders
                    .Where(w => w.CompletionDate != null)
                    .AsEnumerable()
                    .Select(w => (w.CompletionDate!.Value - w.RequestDate).TotalDays)
                    .DefaultIfEmpty(0)
                    .Average()
            })
            .ToListAsync();
    }

    // 2. Status Summary
    public async Task<(List<StatusCount> overall, List<StatusPerTech> perTech)> StatusSummaryAsync(MaintenanceContext db)
    {
        var overall = await db.WorkOrders
            .GroupBy(w => w.Status)
            .Select(g => new StatusCount
            {
                Status = g.Key,
                Count = g.Count()
            })
            .ToListAsync();

        var perTech = await db.WorkOrders
            .GroupBy(w => new { w.Technician!.Name, w.Status })
            .Select(g => new StatusPerTech
            {
                Technician = g.Key.Name,
                Status = g.Key.Status,
                Count = g.Count()
            })
            .ToListAsync();

        return (overall, perTech);
    }

    // 3. Weekly Labor Hours
    public async Task<List<WeeklyHours>> WeeklyLaborAsync(MaintenanceContext db)
    {
        return await db.WorkOrders
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
            .ToListAsync();
    }

    // 4. Top Performer
    public async Task<TopPerf?> TopPerformerAsync(MaintenanceContext db, int minClosed)
    {
        return await db.WorkOrders
            .Where(w => w.Status == "Closed" && w.CompletionDate != null)
            .GroupBy(w => w.Technician!.Name)
            .Select(g => new TopPerf
            {
                TechnicianName = g.Key,
                ClosedCount = g.Count(),

                AvgDays = g
                    .AsEnumerable()
                    .Select(w => (w.CompletionDate!.Value - w.RequestDate).TotalDays)
                    .Average()
            })
            .Where(x => x.ClosedCount >= minClosed)
            .OrderBy(x => x.AvgDays)
            .FirstOrDefaultAsync();
    }

    // 5. Bonus Report
    public async Task<BonusResult> BonusAsync(MaintenanceContext db)
    {
        var busiest = await db.WorkOrders
            .GroupBy(w => w.RequestDate.DayOfYear / 7)
            .Select(g => new
            {
                Week = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .FirstOrDefaultAsync();

        
        var overdue = db.WorkOrders
            .AsEnumerable()
            .Count(w =>
                w.Status == "Open" &&
                (DateTime.Now - w.RequestDate).TotalDays > 7);

        return new BonusResult
        {
            BusiestWeek = busiest != null
                ? DateTime.Now.AddDays(-(DateTime.Now.DayOfYear - busiest.Week * 7))
                : null,
            BusiestClosed = busiest?.Count ?? 0,
            OverdueCount = overdue
        };
    }
}