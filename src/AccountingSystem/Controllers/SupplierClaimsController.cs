using BizCore.Data;
using BizCore.Models.Entities;
using BizCore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize(Roles = "Admin,Warehouse")]
public class SupplierClaimsController : CrudControllerBase
{
    private readonly AccountingDbContext _context;

    public SupplierClaimsController(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var claims = await _context.SerialClaimLogs
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.SerialNumber)
                .ThenInclude(x => x!.Item)
            .OrderByDescending(x => x.ClaimDate)
            .ThenByDescending(x => x.SerialClaimLogId)
            .ToListAsync();

        return View(claims);
    }

    public async Task<IActionResult> Create(int? serialId)
    {
        if (!serialId.HasValue)
        {
            TempData["ClaimError"] = "Open the claim page from a selected serial.";
            return RedirectToAction("Index", "SerialInquiry");
        }

        var serial = await GetSerialAsync(serialId.Value, trackChanges: false);
        if (serial is null)
        {
            return NotFound();
        }

        var model = BuildFormModel(serial);
        PopulateStatusOptions(model);
        ApplyClaimBlockState(model, serial);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SupplierClaimFormViewModel model)
    {
        if (!model.SerialId.HasValue)
        {
            return NotFound();
        }

        var serial = await GetSerialAsync(model.SerialId.Value, trackChanges: true);
        if (serial is null)
        {
            return NotFound();
        }

        PopulateSerialSummary(model, serial);
        PopulateStatusOptions(model);
        ValidateClaim(model, serial);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var claimLog = new SerialClaimLog
        {
            SerialId = serial.SerialId,
            SupplierId = serial.SupplierId!.Value,
            ClaimDate = model.ClaimDate.Date,
            ProblemDescription = string.IsNullOrWhiteSpace(model.ProblemDescription) ? null : model.ProblemDescription.Trim(),
            ClaimStatus = model.ClaimStatus,
            Remark = string.IsNullOrWhiteSpace(model.Remark) ? null : model.Remark.Trim(),
            CreatedDate = DateTime.UtcNow
        };

        _context.SerialClaimLogs.Add(claimLog);
        serial.Status = "ClaimedToSupplier";

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = claimLog.SerialClaimLogId });
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var claim = await _context.SerialClaimLogs
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.SerialNumber)
                .ThenInclude(x => x!.Item)
            .FirstOrDefaultAsync(x => x.SerialClaimLogId == id.Value);

        return claim is null ? NotFound() : View(claim);
    }

    private async Task<SerialNumber?> GetSerialAsync(int serialId, bool trackChanges)
    {
        var query = _context.SerialNumbers
            .Include(x => x.Item)
            .Include(x => x.Supplier)
            .Where(x => x.SerialId == serialId);

        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync();
    }

    private static SupplierClaimFormViewModel BuildFormModel(SerialNumber serial)
    {
        var model = new SupplierClaimFormViewModel();
        PopulateSerialSummary(model, serial);
        model.ClaimStatus = "Open";
        return model;
    }

    private static void PopulateSerialSummary(SupplierClaimFormViewModel model, SerialNumber serial)
    {
        model.SerialId = serial.SerialId;
        model.SerialNo = serial.SerialNo;
        model.ItemCode = serial.Item?.ItemCode ?? string.Empty;
        model.ItemName = serial.Item?.ItemName ?? string.Empty;
        model.PartNumber = serial.Item?.PartNumber ?? string.Empty;
        model.SupplierName = serial.Supplier?.SupplierName ?? string.Empty;
        model.CurrentSerialStatus = serial.Status;
        model.SupplierWarrantyStartDate = serial.SupplierWarrantyStartDate;
        model.SupplierWarrantyEndDate = serial.SupplierWarrantyEndDate;
    }

    private static void ApplyClaimBlockState(SupplierClaimFormViewModel model, SerialNumber serial)
    {
        if (!serial.SupplierWarrantyEndDate.HasValue)
        {
            model.IsClaimBlocked = true;
            model.ClaimBlockMessage = "Supplier warranty is missing. Claim creation is not allowed for this serial.";
            return;
        }

        if (serial.SupplierWarrantyEndDate.Value.Date < DateTime.Today)
        {
            model.IsClaimBlocked = true;
            model.ClaimBlockMessage = "Supplier warranty has expired. Claim creation is not allowed for this serial.";
        }
    }

    private static void PopulateStatusOptions(SupplierClaimFormViewModel model)
    {
        model.StatusOptions = new[]
        {
            new SelectListItem("Open", "Open"),
            new SelectListItem("Sent", "Sent"),
            new SelectListItem("Returned", "Returned"),
            new SelectListItem("Rejected", "Rejected"),
            new SelectListItem("Closed", "Closed")
        };
    }

    private void ValidateClaim(SupplierClaimFormViewModel model, SerialNumber serial)
    {
        if (!serial.SupplierId.HasValue)
        {
            ModelState.AddModelError(string.Empty, "Selected serial does not have a supplier.");
        }

        if (!serial.SupplierWarrantyEndDate.HasValue)
        {
            model.IsClaimBlocked = true;
            model.ClaimBlockMessage = "Supplier warranty is missing. Claim creation is not allowed for this serial.";
            ModelState.AddModelError(string.Empty, model.ClaimBlockMessage);
            return;
        }

        if (serial.SupplierWarrantyEndDate.Value.Date < DateTime.Today)
        {
            model.IsClaimBlocked = true;
            model.ClaimBlockMessage = "Supplier warranty has expired. Claim creation is not allowed for this serial.";
            ModelState.AddModelError(string.Empty, model.ClaimBlockMessage);
        }

        if (serial.SupplierWarrantyStartDate.HasValue && model.ClaimDate.Date < serial.SupplierWarrantyStartDate.Value.Date)
        {
            ModelState.AddModelError(nameof(model.ClaimDate), "Claim date cannot be earlier than the supplier warranty start date.");
        }

        if (model.ClaimDate.Date > serial.SupplierWarrantyEndDate.Value.Date)
        {
            ModelState.AddModelError(nameof(model.ClaimDate), "Claim date must be within the supplier warranty period.");
        }
    }
}
