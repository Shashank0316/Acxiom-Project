using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Models;
using AcxiomCRM.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    private string GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        var isSalesExec = User.IsInRole("Sales Executive");
        var isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Manager");

        var leadsQuery = _context.Leads.AsQueryable();
        var oppsQuery = _context.Opportunities.AsQueryable();
        var custQuery = _context.Customers.AsQueryable();

        if (isSalesExec && !isAdminOrManager)
        {
            leadsQuery = leadsQuery.Where(l => l.AssignedToId == userId);
            oppsQuery = oppsQuery.Where(o => o.AssignedToId == userId);
            custQuery = custQuery.Where(c => c.AssignedToId == userId);
        }

        var totalLeads = await leadsQuery.CountAsync();
        var totalCustomers = await custQuery.CountAsync();
        var totalOpportunities = await oppsQuery.CountAsync();

        var leadsByStatus = await leadsQuery
            .GroupBy(l => l.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        var oppValueByStage = await oppsQuery
            .GroupBy(o => o.Stage)
            .Select(g => new { Stage = g.Key, TotalValue = g.Sum(o => o.Amount) })
            .ToDictionaryAsync(x => x.Stage, x => x.TotalValue);

        ViewBag.TotalLeads = totalLeads;
        ViewBag.TotalCustomers = totalCustomers;
        ViewBag.TotalOpportunities = totalOpportunities;
        
        ViewBag.LeadsByStatusLabels = leadsByStatus.Keys.ToList();
        ViewBag.LeadsByStatusData = leadsByStatus.Values.ToList();

        ViewBag.OppByStageLabels = oppValueByStage.Keys.ToList();
        ViewBag.OppByStageData = oppValueByStage.Values.ToList();

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
