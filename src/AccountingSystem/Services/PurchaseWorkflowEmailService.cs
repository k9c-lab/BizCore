using BizCore.Data;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace BizCore.Services;

public class PurchaseWorkflowEmailService
{
    private readonly AccountingDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PurchaseWorkflowEmailService(
        AccountingDbContext context,
        IEmailSender emailSender,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _emailSender = emailSender;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task NotifyPrSubmittedAsync(int purchaseRequestId)
    {
        var request = await _context.PurchaseRequestHeaders
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.CreatedByUser)
            .Include(x => x.PurchaseRequestDetails)
            .FirstOrDefaultAsync(x => x.PurchaseRequestId == purchaseRequestId);

        if (request is null)
        {
            return;
        }

        var recipients = await GetEmailsByRoleAsync("CentralAdmin");
        var url = BuildUrl("PurchaseRequests", "Details", purchaseRequestId);
        var subject = $"PR pending approval: {request.PRNo}";
        var body = BuildDocumentEmail(
            "Purchase Request requires approval",
            request.PRNo,
            request.Branch?.BranchName,
            request.CreatedByUser?.DisplayName,
            request.RequestDate,
            request.Purpose,
            $"Line count: {request.PurchaseRequestDetails.Count}",
            url);

        await _emailSender.SendAsync(recipients, subject, body);
    }

    public async Task NotifyPrRejectedAsync(int purchaseRequestId)
    {
        var request = await _context.PurchaseRequestHeaders
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.RejectedByUser)
            .FirstOrDefaultAsync(x => x.PurchaseRequestId == purchaseRequestId);

        if (request is null)
        {
            return;
        }

        var recipients = request.BranchId.HasValue
            ? await GetBranchAdminEmailsAsync(request.BranchId.Value)
            : new List<string>();
        var url = BuildUrl("PurchaseRequests", "Details", purchaseRequestId);
        var subject = $"PR rejected: {request.PRNo}";
        var body = BuildDocumentEmail(
            "Purchase Request was rejected",
            request.PRNo,
            request.Branch?.BranchName,
            request.RejectedByUser?.DisplayName,
            request.RequestDate,
            request.Purpose,
            $"Reject reason: {request.RejectReason}",
            url);

        await _emailSender.SendAsync(recipients, subject, body);
    }

    public async Task NotifyPoSubmittedAsync(int purchaseOrderId)
    {
        var order = await _context.PurchaseOrderHeaders
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.Branch)
            .Include(x => x.CreatedByUser)
            .Include(x => x.PurchaseOrderDetails)
            .FirstOrDefaultAsync(x => x.PurchaseOrderId == purchaseOrderId);

        if (order is null)
        {
            return;
        }

        var recipients = await GetEmailsByRoleAsync("Executive");
        var url = BuildUrl("PurchaseOrders", "Details", purchaseOrderId);
        var subject = $"PO pending approval: {order.PONo}";
        var body = BuildDocumentEmail(
            "Purchase Order requires approval",
            order.PONo,
            order.Branch?.BranchName,
            order.CreatedByUser?.DisplayName,
            order.PODate,
            order.Supplier?.SupplierName,
            $"Total amount: {order.TotalAmount:N2}",
            url);

        await _emailSender.SendAsync(recipients, subject, body);
    }

    public async Task NotifyPoRejectedAsync(int purchaseOrderId)
    {
        var order = await _context.PurchaseOrderHeaders
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.Branch)
            .Include(x => x.RejectedByUser)
            .FirstOrDefaultAsync(x => x.PurchaseOrderId == purchaseOrderId);

        if (order is null)
        {
            return;
        }

        var recipients = await GetEmailsByRoleAsync("CentralAdmin");
        var url = BuildUrl("PurchaseOrders", "Details", purchaseOrderId);
        var subject = $"PO rejected: {order.PONo}";
        var body = BuildDocumentEmail(
            "Purchase Order was rejected",
            order.PONo,
            order.Branch?.BranchName,
            order.RejectedByUser?.DisplayName,
            order.PODate,
            order.Supplier?.SupplierName,
            $"Reject reason: {order.RejectReason}",
            url);

        await _emailSender.SendAsync(recipients, subject, body);
    }

    private async Task<List<string>> GetEmailsByRoleAsync(string role)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(x => x.IsActive && x.Role == role && x.Email != null && x.Email != "")
            .Select(x => x.Email!)
            .ToListAsync();
    }

    private async Task<List<string>> GetBranchAdminEmailsAsync(int branchId)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(x => x.IsActive &&
                x.Role == "BranchAdmin" &&
                x.BranchId == branchId &&
                x.Email != null &&
                x.Email != "")
            .Select(x => x.Email!)
            .ToListAsync();
    }

    private string BuildUrl(string controller, string action, int id)
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        if (request is null)
        {
            return $"/{controller}/{action}/{id}";
        }

        var baseUrl = UriHelper.BuildAbsolute(request.Scheme, request.Host, request.PathBase);
        return $"{baseUrl.TrimEnd('/')}/{controller}/{action}/{id}";
    }

    private static string BuildDocumentEmail(
        string title,
        string documentNo,
        string? branchName,
        string? actorName,
        DateTime documentDate,
        string? description,
        string? note,
        string url)
    {
        static string Encode(string? value) => WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(value) ? "-" : value);

        return $"""
            <div style="font-family:Arial,sans-serif;color:#1f2937;line-height:1.5">
                <h2 style="margin:0 0 12px">{Encode(title)}</h2>
                <table cellpadding="6" cellspacing="0" style="border-collapse:collapse">
                    <tr><td><strong>Document</strong></td><td>{Encode(documentNo)}</td></tr>
                    <tr><td><strong>Date</strong></td><td>{documentDate:dd MMM yyyy}</td></tr>
                    <tr><td><strong>Branch</strong></td><td>{Encode(branchName)}</td></tr>
                    <tr><td><strong>By</strong></td><td>{Encode(actorName)}</td></tr>
                    <tr><td><strong>Detail</strong></td><td>{Encode(description)}</td></tr>
                    <tr><td><strong>Note</strong></td><td>{Encode(note)}</td></tr>
                </table>
                <p style="margin-top:16px">
                    <a href="{WebUtility.HtmlEncode(url)}" style="display:inline-block;background:#2563eb;color:#fff;padding:10px 14px;border-radius:6px;text-decoration:none">Open in BizCore</a>
                </p>
            </div>
            """;
    }
}
