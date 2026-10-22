
using FullVoucherSystem.Data;
using FullVoucherSystem.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FullVoucherSystem.Controllers
{
    public class CashroomController : Controller
    {
        private readonly AppDbContext _context;

        private static readonly HashSet<string> AllowedFundingAccounts =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "DRF",
                "DFR",
                "MISCELLANEOUS",
                "NHIA",
                "SCHOOLS"
            };

        public CashroomController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Cashroom/Cashroom
        [HttpGet]
        public IActionResult Cashroom()
        {
            ViewBag.Surname = HttpContext.Session.GetString("Surname");
            ViewBag.FirstName = HttpContext.Session.GetString("FirstName");

            var pendingVouchers = _context.CashRooms
                .OrderByDescending(c => c.Id)
                .ToList();

            return View(pendingVouchers);
        }

        // POST: /Cashroom/SubmitPaidVouchers
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitPaidVouchers(
            List<int> selectedVoucherIds,
            DateTime? processedDate,
            Dictionary<int, string> fundingAccounts,
            Dictionary<int, string> batchNumbers)
        {
            if (selectedVoucherIds == null || !selectedVoucherIds.Any())
            {
                TempData["Message"] =
                    "Please select at least one voucher to process.";

                return RedirectToAction(nameof(Cashroom));
            }

            // Remove duplicate IDs before processing.
            var selectedIds = selectedVoucherIds.Distinct().ToList();

            // Retrieve only the selected vouchers that are still in Cash Room.
            var vouchersToProcess = _context.CashRooms
                .Where(c => selectedIds.Contains(c.Id))
                .ToList();

            // Ensure every submitted ID belongs to a pending Cash Room voucher.
            if (vouchersToProcess.Count != selectedIds.Count)
            {
                TempData["Message"] =
                    "One or more selected vouchers are no longer available. Refresh the page and try again.";

                return RedirectToAction(nameof(Cashroom));
            }

            // Validate the account and batch number for every selected voucher.
            foreach (int voucherId in selectedIds)
            {
                string account = null;
                string batch = null;

                if (fundingAccounts != null)
                    fundingAccounts.TryGetValue(voucherId, out account);

                if (batchNumbers != null)
                    batchNumbers.TryGetValue(voucherId, out batch);

                if (string.IsNullOrWhiteSpace(account) ||
                    !AllowedFundingAccounts.Contains(account.Trim()))
                {
                    TempData["Message"] =
                        $"Please select a valid Funding Account for voucher ID {voucherId}.";

                    return RedirectToAction(nameof(Cashroom));
                }

                if (string.IsNullOrWhiteSpace(batch))
                {
                    TempData["Message"] =
                        $"Please enter a Batch Number for voucher ID {voucherId}.";

                    return RedirectToAction(nameof(Cashroom));
                }
            }

            DateTime paidDate = processedDate ?? DateTime.Today;

            var advancesList = new List<Advance>();
            var finalLedgersList = new List<FinalLedger>();

            foreach (var voucher in vouchersToProcess)
            {
                string voucherNumber = voucher.VoucherNumber ?? string.Empty;

                string fundingAccount = fundingAccounts[voucher.Id].Trim();
                string batchNumber = batchNumbers[voucher.Id].Trim();

                // Preserve the existing routing for advance vouchers.
                bool isAdvance =
                    voucherNumber.Contains(
                        "/SA/", StringComparison.OrdinalIgnoreCase) ||
                    voucherNumber.Contains(
                        "/SA-2/", StringComparison.OrdinalIgnoreCase);

                if (isAdvance)
                {
                    advancesList.Add(new Advance
                    {
                        VoucherNumber = voucher.VoucherNumber,
                        CheckedLocation = voucher.CheckedLocation,
                        Station = voucher.Station,
                        Head = voucher.Head,
                        SubHead = voucher.SubHead,
                        ClassificationCode = voucher.ClassificationCode,
                        SourceClassificationCode = voucher.SourceClassificationCode,
                        Date = voucher.Date,
                        Amount = voucher.Amount,
                        Payee = voucher.Payee,
                        Address = voucher.Address,
                        Description = voucher.Description,
                        FGN_WHT = voucher.FGN_WHT,
                        EDG_WHT = voucher.EDG_WHT,
                        VAT = voucher.VAT,
                        StampDuty = voucher.StampDuty,
                        TotalAmount = voucher.TotalAmount,
                        QueryText = voucher.QueryText,
                        Voucher_FirstName = voucher.Voucher_FirstName,
                        Voucher_Surname = voucher.Voucher_Surname,
                        Voucher_Designation = voucher.Voucher_Designation,
                        Bill_FirstName = voucher.Bill_FirstName,
                        Bill_Surname = voucher.Bill_Surname,
                        Bill_Designation = voucher.Bill_Designation,
                        CertifiedDate = voucher.CertifiedDate,

                        ProcessedDate = paidDate,
                        Batch_No = batchNumber,
                        FundingAccount = fundingAccount
                    });
                }
                else
                {
                    finalLedgersList.Add(new FinalLedger
                    {
                        VoucherNumber = voucher.VoucherNumber,
                        CheckedLocation = voucher.CheckedLocation,
                        Station = voucher.Station,
                        Head = voucher.Head,
                        SubHead = voucher.SubHead,
                        ClassificationCode = voucher.ClassificationCode,
                        SourceClassificationCode = voucher.SourceClassificationCode,
                        Date = voucher.Date,
                        Amount = voucher.Amount,
                        Payee = voucher.Payee,
                        Address = voucher.Address,
                        Description = voucher.Description,
                        FGN_WHT = voucher.FGN_WHT,
                        EDG_WHT = voucher.EDG_WHT,
                        VAT = voucher.VAT,
                        StampDuty = voucher.StampDuty,
                        TotalAmount = voucher.TotalAmount,
                        QueryText = voucher.QueryText,
                        Voucher_FirstName = voucher.Voucher_FirstName,
                        Voucher_Surname = voucher.Voucher_Surname,
                        Voucher_Designation = voucher.Voucher_Designation,
                        Bill_FirstName = voucher.Bill_FirstName,
                        Bill_Surname = voucher.Bill_Surname,
                        Bill_Designation = voucher.Bill_Designation,
                        CertifiedDate = voucher.CertifiedDate,

                        ProcessedDate = paidDate,
                        Batch_No = batchNumber,
                        FundingAccount = fundingAccount
                    });
                }
            }

            // Save all destination records and remove the source records.
            // EF Core normally wraps this SaveChanges call in a transaction.
            if (advancesList.Any())
                _context.Advances.AddRange(advancesList);

            if (finalLedgersList.Any())
                _context.Final_Ledger.AddRange(finalLedgersList);

            _context.CashRooms.RemoveRange(vouchersToProcess);

            _context.SaveChanges();

            TempData["Message"] =
                "Selected vouchers were successfully processed. Funding Account, Batch Number and Processed Date were saved.";

            return RedirectToAction(nameof(Cashroom));
        }
    }
}
