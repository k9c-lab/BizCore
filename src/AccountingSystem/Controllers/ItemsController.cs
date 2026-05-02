using BizCore.Data;
using BizCore.Models.Entities;
using BizCore.Models.ViewModels;
using BizCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize]
public class ItemsController : CrudControllerBase
{
    private static readonly string[] ItemTypes = { "Product", "Service", "Spare Part" };
    private readonly AccountingDbContext _context;
    private readonly ISystemSettingService _systemSettingService;

    public ItemsController(AccountingDbContext context, ISystemSettingService systemSettingService)
    {
        _context = context;
        _systemSettingService = systemSettingService;
    }

    public async Task<IActionResult> Index(string? search, string? itemType, string? status, int page = 1, int pageSize = 20)
    {
        ViewData["Search"] = search;
        ViewData["ItemType"] = itemType;
        ViewData["Status"] = status;

        var query = _context.Items.AsNoTracking();
        var keyword = search?.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x =>
                x.ItemCode.Contains(keyword) ||
                x.ItemName.Contains(keyword) ||
                x.PartNumber.Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(itemType))
        {
            query = query.Where(x => x.ItemType == itemType);
        }

        query = status switch
        {
            "Active" => query.Where(x => x.IsActive),
            "Inactive" => query.Where(x => !x.IsActive),
            _ => query
        };

        var items = await PaginatedList<Item>.CreateAsync(query.OrderBy(x => x.ItemCode), page, pageSize);
        await PopulateStockBalanceTotalsAsync(items.Items);
        return View(items);
    }

    public async Task<IActionResult> Create()
    {
        ViewData["CodeReadOnly"] = true;
        PopulateItemTypeOptions("Product");
        await PopulateNextItemCodeMapAsync();
        return View(new Item
        {
            ItemType = "Product",
            ItemCode = await GetNextItemCodeAsync("Product")
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Item item)
    {
        ViewData["CodeReadOnly"] = true;
        item.ItemType = NormalizeItemType(item.ItemType);
        item.ItemCode = await GetNextItemCodeAsync(item.ItemType);
        ModelState.Remove(nameof(Item.ItemCode));
        ValidateItemType(item.ItemType);

        if (!ModelState.IsValid)
        {
            PopulateItemTypeOptions(item.ItemType);
            await PopulateNextItemCodeMapAsync();
            return View(item);
        }

        _context.Items.Add(item);
        if (!await TrySaveAsync("Item code and part number must be unique."))
        {
            PopulateItemTypeOptions(item.ItemType);
            await PopulateNextItemCodeMapAsync();
            return View(item);
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var item = await _context.Items.FindAsync(id.Value);
        PopulateItemTypeOptions(item?.ItemType ?? "Product");
        return item is null ? NotFound() : View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Item item)
    {
        if (id != item.ItemId)
        {
            return NotFound();
        }

        ViewData["CodeReadOnly"] = false;
        item.ItemType = NormalizeItemType(item.ItemType);
        ValidateItemType(item.ItemType);
        if (!ModelState.IsValid)
        {
            PopulateItemTypeOptions(item.ItemType);
            return View(item);
        }

        _context.Update(item);
        if (!await TrySaveAsync("Item code and part number must be unique."))
        {
            PopulateItemTypeOptions(item.ItemType);
            return View(item);
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var item = await _context.Items.FirstOrDefaultAsync(x => x.ItemId == id.Value);
        if (item is not null)
        {
            item.CurrentStock = await GetStockBalanceTotalAsync(item.ItemId);
        }

        return item is null ? NotFound() : View(item);
    }

    public async Task<IActionResult> Pricing(int? id, CancellationToken cancellationToken)
    {
        if (id is null)
        {
            return NotFound();
        }

        var item = await _context.Items
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ItemId == id.Value, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        return View(await BuildPricingViewModelAsync(item, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pricing(ItemPricingViewModel model, CancellationToken cancellationToken)
    {
        var item = await _context.Items
            .FirstOrDefaultAsync(x => x.ItemId == model.ItemId, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        var priceLevels = await _context.PriceLevels
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.PriceLevelCode)
            .ToListAsync(cancellationToken);
        var validLevelIds = priceLevels.Select(x => x.PriceLevelId).ToHashSet();

        foreach (var row in model.PriceRows)
        {
            if (!validLevelIds.Contains(row.PriceLevelId))
            {
                ModelState.AddModelError(string.Empty, "One or more price levels are no longer valid. Please reload and try again.");
                break;
            }
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildPricingViewModelAsync(item, cancellationToken));
        }

        var existingPrices = await _context.ItemPrices
            .Where(x => x.ItemId == item.ItemId)
            .ToListAsync(cancellationToken);

        foreach (var level in priceLevels)
        {
            var row = model.PriceRows.FirstOrDefault(x => x.PriceLevelId == level.PriceLevelId);
            if (row is null)
            {
                continue;
            }

            var existing = existingPrices.FirstOrDefault(x => x.PriceLevelId == level.PriceLevelId);
            if (existing is null)
            {
                _context.ItemPrices.Add(new ItemPrice
                {
                    ItemId = item.ItemId,
                    PriceLevelId = level.PriceLevelId,
                    UnitPrice = row.UnitPrice,
                    IsActive = row.IsActive
                });
            }
            else
            {
                existing.UnitPrice = row.UnitPrice;
                existing.IsActive = row.IsActive;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        TempData["ItemPriceNotice"] = "Selling prices were updated successfully.";
        return RedirectToAction(nameof(Pricing), new { id = item.ItemId });
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var item = await _context.Items.FirstOrDefaultAsync(x => x.ItemId == id.Value);
        return item is null ? NotFound() : View(item);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var item = await _context.Items.FindAsync(id);
        if (item is not null)
        {
            _context.Items.Remove(item);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> TrySaveAsync(string duplicateMessage)
    {
        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex) when (IsDuplicateConstraintViolation(ex))
        {
            ModelState.AddModelError(string.Empty, duplicateMessage);
            return false;
        }
    }

    private async Task PopulateStockBalanceTotalsAsync(IEnumerable<Item> items)
    {
        var itemList = items.ToList();
        var itemIds = itemList.Select(x => x.ItemId).ToList();
        if (itemIds.Count == 0)
        {
            return;
        }

        var balanceMap = await _context.StockBalances
            .AsNoTracking()
            .Where(x => itemIds.Contains(x.ItemId))
            .GroupBy(x => x.ItemId)
            .Select(x => new
            {
                ItemId = x.Key,
                Qty = x.Sum(b => b.QtyOnHand)
            })
            .ToDictionaryAsync(x => x.ItemId, x => x.Qty);

        foreach (var item in itemList)
        {
            item.CurrentStock = balanceMap.TryGetValue(item.ItemId, out var qty) ? qty : 0m;
        }
    }

    private async Task<decimal> GetStockBalanceTotalAsync(int itemId)
    {
        return await _context.StockBalances
            .AsNoTracking()
            .Where(x => x.ItemId == itemId)
            .SumAsync(x => (decimal?)x.QtyOnHand) ?? 0m;
    }

    private async Task<ItemPricingViewModel> BuildPricingViewModelAsync(Item item, CancellationToken cancellationToken)
    {
        var pricingMode = await _systemSettingService.GetPricingModeAsync(cancellationToken);
        var levels = await _context.PriceLevels
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.PriceLevelCode)
            .ToListAsync(cancellationToken);
        var existingPrices = await _context.ItemPrices
            .AsNoTracking()
            .Where(x => x.ItemId == item.ItemId)
            .ToListAsync(cancellationToken);

        return new ItemPricingViewModel
        {
            ItemId = item.ItemId,
            ItemCode = item.ItemCode,
            ItemName = item.ItemName,
            PartNumber = item.PartNumber,
            BaseUnitPrice = item.UnitPrice,
            PricingMode = pricingMode,
            PriceRows = levels
                .Select(level =>
                {
                    var existing = existingPrices.FirstOrDefault(x => x.PriceLevelId == level.PriceLevelId);
                    return new ItemPriceEditorRowViewModel
                    {
                        ItemPriceId = existing?.ItemPriceId,
                        PriceLevelId = level.PriceLevelId,
                        PriceLevelCode = level.PriceLevelCode,
                        PriceLevelName = level.PriceLevelName,
                        Description = level.Description,
                        UnitPrice = existing?.UnitPrice ?? 0m,
                        IsActive = existing?.IsActive ?? level.IsActive
                    };
                })
                .ToList()
        };
    }

    private void PopulateItemTypeOptions(string? selectedType)
    {
        ViewData["ItemTypeOptions"] = ItemTypes.Select(type => new SelectListItem
        {
            Value = type,
            Text = type,
            Selected = string.Equals(type, selectedType, StringComparison.OrdinalIgnoreCase)
        }).ToList();
    }

    private async Task PopulateNextItemCodeMapAsync()
    {
        var map = new Dictionary<string, string>();
        foreach (var itemType in ItemTypes)
        {
            map[itemType] = await GetNextItemCodeAsync(itemType);
        }

        ViewData["NextItemCodeMap"] = map;
    }

    private Task<string> GetNextItemCodeAsync(string? itemType)
    {
        var prefix = GetCodePrefix(itemType);
        return GetNextCodeAsync(_context.Items.Select(x => x.ItemCode), prefix);
    }

    private static async Task<string> GetNextCodeAsync(IQueryable<string> codesQuery, string prefix)
    {
        var codePrefix = $"{prefix}-";
        var codes = await codesQuery.Where(x => x.StartsWith(codePrefix)).ToListAsync();
        var nextSequence = codes.Select(ExtractSequence).DefaultIfEmpty(0).Max() + 1;
        return FormatPrefixedCode(prefix, nextSequence);
    }

    private static string NormalizeItemType(string? itemType)
    {
        return ItemTypes.FirstOrDefault(type => string.Equals(type, itemType, StringComparison.OrdinalIgnoreCase)) ?? (itemType ?? string.Empty).Trim();
    }

    private void ValidateItemType(string itemType)
    {
        if (!ItemTypes.Contains(itemType))
        {
            ModelState.AddModelError(nameof(Item.ItemType), "Please select a valid item type.");
        }
    }

    private static string GetCodePrefix(string? itemType)
    {
        return itemType switch
        {
            "Service" => "SRV",
            "Spare Part" => "SPP",
            _ => "PRD"
        };
    }
}
