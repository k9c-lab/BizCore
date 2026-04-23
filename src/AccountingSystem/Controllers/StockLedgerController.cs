using BizCore.Data;
using BizCore.Models.Entities;
using BizCore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize]
public class StockLedgerController : CrudControllerBase
{
    private readonly AccountingDbContext _context;

    public StockLedgerController(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(
        string? search,
        int? itemId,
        int? branchId,
        string? movementType,
        string? referenceType,
        DateTime? dateFrom,
        DateTime? dateTo,
        int page = 1,
        int pageSize = 20)
    {
        var canAccessAllBranches = CanAccessAllBranches();
        var effectiveBranchId = ResolveBranchId(branchId, canAccessAllBranches);
        var branchName = await ResolveBranchNameAsync(effectiveBranchId, canAccessAllBranches);

        var query = BuildFilteredQuery(search, itemId, effectiveBranchId, movementType, referenceType, dateFrom, dateTo);
        var issueSummaryQuery = BuildFilteredQuery(search, itemId, effectiveBranchId, null, "StockIssue", dateFrom, dateTo);

        var issueQty = await issueSummaryQuery
            .Where(x => x.MovementType == "Issue")
            .SumAsync(x => (decimal?)x.Qty) ?? 0m;
        var issueCancelQty = await issueSummaryQuery
            .Where(x => x.MovementType == "IssueCancel")
            .SumAsync(x => (decimal?)x.Qty) ?? 0m;

        var results = await PaginatedList<StockMovement>.CreateAsync(
            query
                .OrderByDescending(x => x.MovementDate)
                .ThenByDescending(x => x.StockMovementId),
            page,
            pageSize);

        var model = new StockLedgerPageViewModel
        {
            Search = search?.Trim(),
            ItemId = itemId,
            BranchId = effectiveBranchId,
            MovementType = movementType,
            ReferenceType = referenceType,
            DateFrom = dateFrom,
            DateTo = dateTo,
            BranchName = branchName,
            CanAccessAllBranches = canAccessAllBranches,
            IssueQty = issueQty,
            IssueCancelQty = issueCancelQty,
            NetIssuedQty = issueQty - issueCancelQty,
            BranchOptions = await BuildBranchOptionsAsync(effectiveBranchId, canAccessAllBranches),
            ItemOptions = await BuildItemOptionsAsync(itemId),
            MovementTypeOptions = BuildMovementTypeOptions(movementType),
            ReferenceTypeOptions = BuildReferenceTypeOptions(referenceType),
            Pagination = results.Pagination,
            Results = results.Items.Select(x => MapRow(x, effectiveBranchId)).ToList()
        };

        return View(model);
    }

    private IQueryable<StockMovement> BuildFilteredQuery(
        string? search,
        int? itemId,
        int? branchId,
        string? movementType,
        string? referenceType,
        DateTime? dateFrom,
        DateTime? dateTo)
    {
        var query = _context.StockMovements
            .AsNoTracking()
            .Include(x => x.Item)
            .Include(x => x.SerialNumber)
            .Include(x => x.FromBranch)
            .Include(x => x.ToBranch)
            .Include(x => x.CreatedByUser)
            .AsQueryable();

        if (itemId.HasValue)
        {
            query = query.Where(x => x.ItemId == itemId.Value);
        }

        if (branchId.HasValue)
        {
            var selectedBranchId = branchId.Value;
            query = query.Where(x => x.FromBranchId == selectedBranchId || x.ToBranchId == selectedBranchId);
        }

        if (!string.IsNullOrWhiteSpace(movementType))
        {
            query = query.Where(x => x.MovementType == movementType);
        }

        if (!string.IsNullOrWhiteSpace(referenceType))
        {
            query = query.Where(x => x.ReferenceType == referenceType);
        }

        if (dateFrom.HasValue)
        {
            var fromDate = dateFrom.Value.Date;
            query = query.Where(x => x.MovementDate.Date >= fromDate);
        }

        if (dateTo.HasValue)
        {
            var toDate = dateTo.Value.Date;
            query = query.Where(x => x.MovementDate.Date <= toDate);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(x =>
                x.MovementType.Contains(keyword) ||
                (x.ReferenceType != null && x.ReferenceType.Contains(keyword)) ||
                (x.Remark != null && x.Remark.Contains(keyword)) ||
                (x.Item != null && (
                    x.Item.ItemCode.Contains(keyword) ||
                    x.Item.ItemName.Contains(keyword) ||
                    x.Item.PartNumber.Contains(keyword))) ||
                (x.SerialNumber != null && x.SerialNumber.SerialNo.Contains(keyword)) ||
                (x.FromBranch != null && (
                    x.FromBranch.BranchCode.Contains(keyword) ||
                    x.FromBranch.BranchName.Contains(keyword))) ||
                (x.ToBranch != null && (
                    x.ToBranch.BranchCode.Contains(keyword) ||
                    x.ToBranch.BranchName.Contains(keyword))));
        }

        return query;
    }

    private static StockLedgerRowViewModel MapRow(StockMovement movement, int? selectedBranchId)
    {
        var (inQty, outQty) = ComputeInOutQty(movement, selectedBranchId);

        return new StockLedgerRowViewModel
        {
            StockMovementId = movement.StockMovementId,
            MovementDate = movement.MovementDate,
            MovementType = movement.MovementType,
            ReferenceType = movement.ReferenceType,
            ReferenceId = movement.ReferenceId,
            ItemId = movement.ItemId,
            ItemCode = movement.Item?.ItemCode ?? string.Empty,
            ItemName = movement.Item?.ItemName ?? string.Empty,
            PartNumber = movement.Item?.PartNumber ?? string.Empty,
            SerialId = movement.SerialId,
            SerialNo = movement.SerialNumber?.SerialNo ?? "-",
            FromBranchId = movement.FromBranchId,
            FromBranchName = movement.FromBranch?.BranchName ?? "-",
            ToBranchId = movement.ToBranchId,
            ToBranchName = movement.ToBranch?.BranchName ?? "-",
            Qty = movement.Qty,
            InQty = inQty,
            OutQty = outQty,
            Remark = movement.Remark,
            CreatedByName = movement.CreatedByUser?.DisplayName ?? "-"
        };
    }

    private static (decimal InQty, decimal OutQty) ComputeInOutQty(StockMovement movement, int? selectedBranchId)
    {
        var qty = Math.Abs(movement.Qty);

        if (selectedBranchId.HasValue)
        {
            var direction = ResolveBranchDirection(movement, selectedBranchId.Value);
            return direction switch
            {
                StockMovementDirection.In => (qty, 0m),
                StockMovementDirection.Out => (0m, qty),
                _ => (0m, 0m)
            };
        }

        if (IsInboundMovement(movement.MovementType))
        {
            return (qty, 0m);
        }

        return IsOutboundMovement(movement.MovementType) ? (0m, qty) : (0m, 0m);
    }

    private static StockMovementDirection ResolveBranchDirection(StockMovement movement, int selectedBranchId)
    {
        return movement.MovementType switch
        {
            "Receiving" => movement.ToBranchId == selectedBranchId ? StockMovementDirection.In : StockMovementDirection.None,
            "ReceivingCancel" => movement.FromBranchId == selectedBranchId ? StockMovementDirection.Out : StockMovementDirection.None,
            "InvoiceIssue" => movement.FromBranchId == selectedBranchId ? StockMovementDirection.Out : StockMovementDirection.None,
            "InvoiceCancel" => movement.ToBranchId == selectedBranchId ? StockMovementDirection.In : StockMovementDirection.None,
            "Issue" => movement.FromBranchId == selectedBranchId ? StockMovementDirection.Out : StockMovementDirection.None,
            "IssueCancel" => movement.FromBranchId == selectedBranchId ? StockMovementDirection.In : StockMovementDirection.None,
            "CustomerClaimReplacement" => movement.FromBranchId == selectedBranchId ? StockMovementDirection.Out : StockMovementDirection.None,
            "SupplierReplacement" => movement.ToBranchId == selectedBranchId ? StockMovementDirection.In : StockMovementDirection.None,
            "SupplierRepairReturn" => movement.ToBranchId == selectedBranchId ? StockMovementDirection.In : StockMovementDirection.None,
            "Transfer" => ResolveTransferDirection(movement, selectedBranchId),
            "TransferCancel" => ResolveTransferDirection(movement, selectedBranchId),
            _ => StockMovementDirection.None
        };
    }

    private static StockMovementDirection ResolveTransferDirection(StockMovement movement, int selectedBranchId)
    {
        if (movement.ToBranchId == selectedBranchId)
        {
            return StockMovementDirection.In;
        }

        if (movement.FromBranchId == selectedBranchId)
        {
            return StockMovementDirection.Out;
        }

        return StockMovementDirection.None;
    }

    private static bool IsInboundMovement(string movementType)
    {
        return movementType is "Receiving" or "InvoiceCancel" or "IssueCancel" or "SupplierReplacement" or "SupplierRepairReturn";
    }

    private static bool IsOutboundMovement(string movementType)
    {
        return movementType is "ReceivingCancel" or "InvoiceIssue" or "Transfer" or "TransferCancel" or "Issue" or "CustomerClaimReplacement";
    }

    private enum StockMovementDirection
    {
        None,
        In,
        Out
    }

    private bool CanAccessAllBranches()
    {
        return CurrentUserCanAccessAllBranches();
    }

    private new int? CurrentBranchId()
    {
        return base.CurrentBranchId();
    }

    private int? ResolveBranchId(int? requestedBranchId, bool canAccessAllBranches)
    {
        return canAccessAllBranches ? requestedBranchId : CurrentBranchId();
    }

    private async Task<string> ResolveBranchNameAsync(int? branchId, bool canAccessAllBranches)
    {
        if (!branchId.HasValue && canAccessAllBranches)
        {
            return "All Branches";
        }

        if (!branchId.HasValue)
        {
            return "No Branch";
        }

        return await _context.Branches
            .AsNoTracking()
            .Where(x => x.BranchId == branchId.Value)
            .Select(x => x.BranchName)
            .FirstOrDefaultAsync() ?? "No Branch";
    }

    private async Task<IReadOnlyList<SelectListItem>> BuildBranchOptionsAsync(int? selectedBranchId, bool canAccessAllBranches)
    {
        if (!canAccessAllBranches)
        {
            return Array.Empty<SelectListItem>();
        }

        var options = new List<SelectListItem>
        {
            new() { Value = string.Empty, Text = "All Branches", Selected = !selectedBranchId.HasValue }
        };

        options.AddRange(await _context.Branches
            .AsNoTracking()
            .OrderBy(x => x.BranchCode)
            .Select(x => new SelectListItem
            {
                Value = x.BranchId.ToString(),
                Text = x.BranchCode + " - " + x.BranchName,
                Selected = selectedBranchId.HasValue && x.BranchId == selectedBranchId.Value
            })
            .ToListAsync());

        return options;
    }

    private async Task<IReadOnlyList<SelectListItem>> BuildItemOptionsAsync(int? selectedItemId)
    {
        var options = new List<SelectListItem>
        {
            new() { Value = string.Empty, Text = "All Items", Selected = !selectedItemId.HasValue }
        };

        options.AddRange(await _context.Items
            .AsNoTracking()
            .Where(x => x.TrackStock)
            .OrderBy(x => x.ItemCode)
            .Select(x => new SelectListItem
            {
                Value = x.ItemId.ToString(),
                Text = x.ItemCode + " - " + x.ItemName,
                Selected = selectedItemId.HasValue && x.ItemId == selectedItemId.Value
            })
            .ToListAsync());

        return options;
    }

    private static IReadOnlyList<SelectListItem> BuildMovementTypeOptions(string? selectedMovementType)
    {
        var movementTypes = new[]
        {
            "Receiving",
            "ReceivingCancel",
            "InvoiceIssue",
            "InvoiceCancel",
            "Transfer",
            "TransferCancel",
            "Issue",
            "IssueCancel",
            "CustomerClaimReplacement",
            "SupplierReplacement",
            "SupplierRepairReturn"
        };

        return BuildOptions("All Movements", movementTypes, selectedMovementType);
    }

    private static IReadOnlyList<SelectListItem> BuildReferenceTypeOptions(string? selectedReferenceType)
    {
        var referenceTypes = new[]
        {
            "Receiving",
            "Invoice",
            "StockTransfer",
            "StockIssue",
            "CustomerClaim",
            "SupplierClaim"
        };

        return BuildOptions("All References", referenceTypes, selectedReferenceType);
    }

    private static IReadOnlyList<SelectListItem> BuildOptions(string emptyText, IEnumerable<string> values, string? selectedValue)
    {
        var options = new List<SelectListItem>
        {
            new() { Value = string.Empty, Text = emptyText, Selected = string.IsNullOrWhiteSpace(selectedValue) }
        };

        options.AddRange(values.Select(x => new SelectListItem
        {
            Value = x,
            Text = x,
            Selected = string.Equals(x, selectedValue, StringComparison.OrdinalIgnoreCase)
        }));

        return options;
    }
}
