using FullVoucherSystem.Data;
using FullVoucherSystem.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace FullVoucherSystem.Controllers
{
    public class FinalLedgerController : Controller
    {
        private readonly AppDbContext _context;

        public FinalLedgerController(AppDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // DASHBOARD
        // ============================================================
        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.Surname =
                HttpContext.Session.GetString("Surname");

            ViewBag.FirstName =
                HttpContext.Session.GetString("FirstName");

            return View();
        }

        // ============================================================
        // SEARCH RAISED VOUCHERS
        // ============================================================
        [HttpPost]
        public IActionResult SearchRaisedVouchers(
            string voucherType,
            DateTime? startDate,
            DateTime? endDate)
        {
            var query = _context.Vouchers.AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(v =>
                    v.Date >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                DateTime exclusiveEndDate =
                    endDate.Value.Date.AddDays(1);

                query = query.Where(v =>
                    v.Date < exclusiveEndDate);
            }

            if (!string.IsNullOrWhiteSpace(voucherType) &&
                voucherType != "ALL TYPES")
            {
                if (voucherType == "ALL Es")
                {
                    string[] eTypes =
                    {
                        "E", "EE", "ES", "EL", "EP", "ER"
                    };

                    query = query.Where(v =>
                        eTypes.Contains(v.VoucherType));
                }
                else
                {
                    query = query.Where(v =>
                        v.VoucherType == voucherType);
                }
            }

            var results = query
                .OrderByDescending(v => v.Date)
                .ToList();

            return Json(results);
        }

        // ============================================================
        // SEARCH PAID VOUCHERS
        // Returns Final_Ledger records for the existing paid table.
        // ============================================================
        [HttpPost]
        public IActionResult SearchPaidVouchers(
            string voucherType,
            string fundingAccount,
            string batchNo,
            DateTime? startDate,
            DateTime? endDate)
        {
            var query = _context.Final_Ledger.AsQueryable();

            query = ApplyPaidFilters(
                query,
                voucherType,
                fundingAccount,
                batchNo,
                startDate,
                endDate);

            var results = query
                .OrderByDescending(v => v.Date)
                .ToList();

            return Json(results);
        }

        // ============================================================
        // PAID VOUCHERS ANALYSED REPORT
        // Combines Final_Ledger and Advances.
        // Applies the selected filters to both tables.
        // ============================================================
        [HttpPost]
        public IActionResult SearchPaidVouchersForAnalysis(
            string voucherType,
            string fundingAccount,
            string batchNo,
            DateTime? startDate,
            DateTime? endDate)
        {
            var ledgerQuery =
                _context.Final_Ledger.AsQueryable();

            var advancesQuery =
                _context.Advances.AsQueryable();

            // Apply filters to Final_Ledger.
            ledgerQuery = ApplyPaidFilters(
                ledgerQuery,
                voucherType,
                fundingAccount,
                batchNo,
                startDate,
                endDate);

            // Apply the same filters to Advances.
            advancesQuery = ApplyAdvanceFilters(
                advancesQuery,
                voucherType,
                fundingAccount,
                batchNo,
                startDate,
                endDate);

            // Project Final_Ledger records.
            var ledgerRecords = ledgerQuery
                .ToList()
                .Select(v => new PaidAnalysisRecord
                {
                    SourceTable = "Final_Ledger",
                    VoucherNumber = v.VoucherNumber,
                    ClassificationCode = v.ClassificationCode,
                    Date = v.Date,
                    ProcessedDate = v.ProcessedDate,
                    FundingAccount = v.FundingAccount,
                    Batch_No = v.Batch_No,
                    Amount = v.Amount,
                    TotalAmount = v.TotalAmount
                });

            // Project Advances records.
            var advanceRecords = advancesQuery
                .ToList()
                .Select(v => new PaidAnalysisRecord
                {
                    SourceTable = "Advances",
                    VoucherNumber = v.VoucherNumber,
                    ClassificationCode = v.ClassificationCode,
                    Date = v.Date,
                    ProcessedDate = v.ProcessedDate,
                    FundingAccount = v.FundingAccount,
                    Batch_No = v.Batch_No,
                    Amount = v.Amount,
                    TotalAmount = v.TotalAmount
                });

            // Combine records from both tables.
            var combinedRecords = ledgerRecords
                .Concat(advanceRecords)
                .OrderByDescending(v => v.Date)
                .ToList();

            return Json(combinedRecords);
        }

        // ============================================================
        // FILTER FINAL_LEDGER RECORDS
        // ============================================================
        private static IQueryable<FinalLedger> ApplyPaidFilters(
            IQueryable<FinalLedger> query,
            string voucherType,
            string fundingAccount,
            string batchNo,
            DateTime? startDate,
            DateTime? endDate)
        {
            query = ApplyVoucherTypeFilter(
                query,
                voucherType);

            if (!string.IsNullOrWhiteSpace(fundingAccount) &&
                !fundingAccount.Equals(
                    "ALL",
                    StringComparison.OrdinalIgnoreCase))
            {
                string selectedAccount = fundingAccount.Trim();

                query = query.Where(v =>
                    v.FundingAccount != null &&
                    v.FundingAccount == selectedAccount);
            }

            if (!string.IsNullOrWhiteSpace(batchNo))
            {
                string selectedBatch = batchNo.Trim();

                query = query.Where(v =>
                    v.Batch_No != null &&
                    v.Batch_No.Contains(selectedBatch));
            }

            if (startDate.HasValue)
            {
                query = query.Where(v =>
                    v.Date >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                DateTime exclusiveEndDate =
                    endDate.Value.Date.AddDays(1);

                query = query.Where(v =>
                    v.Date < exclusiveEndDate);
            }

            return query;
        }

        // ============================================================
        // FILTER ADVANCES RECORDS
        // ============================================================
        private static IQueryable<Advance> ApplyAdvanceFilters(
            IQueryable<Advance> query,
            string voucherType,
            string fundingAccount,
            string batchNo,
            DateTime? startDate,
            DateTime? endDate)
        {
            query = ApplyAdvanceVoucherTypeFilter(
                query,
                voucherType);

            if (!string.IsNullOrWhiteSpace(fundingAccount) &&
                !fundingAccount.Equals(
                    "ALL",
                    StringComparison.OrdinalIgnoreCase))
            {
                string selectedAccount = fundingAccount.Trim();

                query = query.Where(v =>
                    v.FundingAccount != null &&
                    v.FundingAccount == selectedAccount);
            }

            if (!string.IsNullOrWhiteSpace(batchNo))
            {
                string selectedBatch = batchNo.Trim();

                query = query.Where(v =>
                    v.Batch_No != null &&
                    v.Batch_No.Contains(selectedBatch));
            }

            if (startDate.HasValue)
            {
                query = query.Where(v =>
                    v.Date >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                DateTime exclusiveEndDate =
                    endDate.Value.Date.AddDays(1);

                query = query.Where(v =>
                    v.Date < exclusiveEndDate);
            }

            return query;
        }

        // ============================================================
        // VOUCHER TYPE FILTER: FINAL_LEDGER
        // ============================================================
        private static IQueryable<FinalLedger> ApplyVoucherTypeFilter(
            IQueryable<FinalLedger> query,
            string voucherType)
        {
            if (string.IsNullOrWhiteSpace(voucherType) ||
                voucherType == "ALL TYPES")
            {
                return query;
            }

            if (voucherType == "ALL Es")
            {
                return query.Where(v =>
                    v.VoucherNumber != null &&
                    (
                        v.VoucherNumber.Contains("/SA/") ||
                        v.VoucherNumber.Contains("/SA-2/") || 
                        v.VoucherNumber.Contains("/E/") ||
                        v.VoucherNumber.Contains("/EE/") ||
                        v.VoucherNumber.Contains("/ES/") ||
                        v.VoucherNumber.Contains("/EL/") ||
                        v.VoucherNumber.Contains("/EP/") ||
                        v.VoucherNumber.Contains("/ER/")
                    ));
            }

            string voucherPattern = "/" + voucherType + "/";

            return query.Where(v =>
                v.VoucherNumber != null &&
                v.VoucherNumber.Contains(voucherPattern));
        }

        // ============================================================
        // VOUCHER TYPE FILTER: ADVANCES
        // ============================================================
        private static IQueryable<Advance>
            ApplyAdvanceVoucherTypeFilter(
                IQueryable<Advance> query,
                string voucherType)
        {
            if (string.IsNullOrWhiteSpace(voucherType) ||
                voucherType == "ALL TYPES")
            {
                return query;
            }

            if (voucherType == "ALL Es")
            {
                return query.Where(v =>
                    v.VoucherNumber != null &&
                    (
                        v.VoucherNumber.Contains("/SA/") ||
                        v.VoucherNumber.Contains("/SA-2/") || 
                        v.VoucherNumber.Contains("/E/") ||
                        v.VoucherNumber.Contains("/EE/") ||
                        v.VoucherNumber.Contains("/ES/") ||
                        v.VoucherNumber.Contains("/EL/") ||
                        v.VoucherNumber.Contains("/EP/") ||
                        v.VoucherNumber.Contains("/ER/")
                    ));
            }

            string voucherPattern = "/" + voucherType + "/";

            return query.Where(v =>
                v.VoucherNumber != null &&
                v.VoucherNumber.Contains(voucherPattern));
        }

        // ============================================================
        // COMMON DATA STRUCTURE FOR ANALYSED REPORT
        // IMPORTANT: TotalAmount and Amount are nullable decimals.
        // ============================================================
        private sealed class PaidAnalysisRecord
        {
            public string? SourceTable { get; set; }

            public string? VoucherNumber { get; set; }

            public string? ClassificationCode { get; set; }

            public DateTime Date { get; set; }

            public DateTime? ProcessedDate { get; set; }

            public string? FundingAccount { get; set; }

            public string? Batch_No { get; set; }

            public decimal? Amount { get; set; }

            public decimal? TotalAmount { get; set; }
        }
    }
}
