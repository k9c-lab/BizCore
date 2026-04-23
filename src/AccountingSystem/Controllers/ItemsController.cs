using BizCore.Data;
using BizCore.Models.Entities;
using BizCore.Models.ViewModels;
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

    public ItemsController(AccountingDbContext context)
    {
        _context = context;
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
