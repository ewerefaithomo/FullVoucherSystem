using FullVoucherSystem.Data;
using FullVoucherSystem.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FullVoucherSystem.Controllers
{
    public class VoucherController : Controller
    {
        private readonly AppDbContext _context;

        public VoucherController(AppDbContext context)
        {
            _context = context;
        }

        private (decimal contractAmount, decimal fgn_wht, decimal edg_wht, decimal stamp, decimal vat, decimal total) CalculateFinancials(
            decimal inputAmount,
            bool isVatInclusive,
            string fgn_whtOption,
            string edg_whtOption,
            string vatOption,
            string stampOption)
        {
            decimal fgn_whtPercentage = 0;
            decimal edg_whtPercentage = 0;
            decimal stampPercentage = 0;
            decimal vatPercentage = 0;

            if (!string.IsNullOrWhiteSpace(fgn_whtOption))
                decimal.TryParse(fgn_whtOption.Replace("%", ""), out fgn_whtPercentage);

            if (!string.IsNullOrWhiteSpace(edg_whtOption))
                decimal.TryParse(edg_whtOption.Replace("%", ""), out edg_whtPercentage);

            if (!string.IsNullOrWhiteSpace(stampOption))
                decimal.TryParse(stampOption.Replace("%", ""), out stampPercentage);

            if (!string.IsNullOrWhiteSpace(vatOption))
                decimal.TryParse(vatOption.Replace("%", ""), out vatPercentage);

            decimal contractAmount;
            decimal vat;
            decimal fgn_wht;
            decimal edg_wht;
            decimal stamp;
            decimal total;

            if (isVatInclusive)
            {
                decimal grossAmount = inputAmount;
                vat = (vatPercentage / 107.5m) * grossAmount;
                contractAmount = grossAmount - vat;
                fgn_wht = (fgn_whtPercentage / 107.5m) * grossAmount;
                edg_wht = (edg_whtPercentage / 107.5m) * grossAmount;
                stamp = (stampPercentage / 107.5m) * grossAmount;
                total = grossAmount - vat - fgn_wht - edg_wht - stamp;
            }
            else
            {
                contractAmount = inputAmount;
                fgn_wht = contractAmount * (fgn_whtPercentage / 100m);
                edg_wht = contractAmount * (edg_whtPercentage / 100m);
                stamp = contractAmount * (stampPercentage / 100m);
                vat = contractAmount * (vatPercentage / 100m);
                total = contractAmount - edg_wht - fgn_wht - stamp;
            }

            return (contractAmount, fgn_wht, edg_wht, stamp, vat, total);
        }

        /// <summary>
        /// Generates a unique Voucher Number by checking the Vouchers table.
        /// If the generated number exists, it increments the sequence number by 1 until a free number is found.
        /// </summary>
        private string GenerateUniqueVoucherNumber(string voucherType)
        {
            string currentMonth = DateTime.Now.ToString("MMM").ToUpper();
            string currentYear = DateTime.Now.ToString("yy");

            int count = _context.Vouchers
                .Where(v => v.Date.Month == DateTime.Now.Month &&
                            v.Date.Year == DateTime.Now.Year &&
                            v.VoucherType == voucherType)
                .Count();

            int nextNumber = count + 1;
            string candidateVoucherNumber = $"{currentMonth}/{voucherType}/{nextNumber}A/{currentYear}";

            // Loop to check if candidateVoucherNumber already exists in Vouchers table
            while (_context.Vouchers.Any(v => v.VoucherNumber == candidateVoucherNumber))
            {
                nextNumber++;
                candidateVoucherNumber = $"{currentMonth}/{voucherType}/{nextNumber}A/{currentYear}";
            }

            return candidateVoucherNumber;
        }

        [HttpGet]
        public IActionResult Create(int? id)
        {
            ViewBag.Surname = HttpContext.Session.GetString("Surname");
            ViewBag.FirstName = HttpContext.Session.GetString("FirstName");

            if (id != null)
            {
                var voucher = _context.Vouchers.FirstOrDefault(v => v.Id == id);
                return View(voucher);
            }

            return View();
        }

        [HttpGet]
        public JsonResult GenerateVoucherNumber(string voucherType)
        {
            if (string.IsNullOrWhiteSpace(voucherType))
            {
                return Json("");
            }

            string voucherNumber = GenerateUniqueVoucherNumber(voucherType);
            return Json(voucherNumber);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(
            Voucher model,
            bool? isVatInclusive,
            string fgn_whtOption,
            string edg_whtOption,
            string vatOption,
            string stampOption)
        {
            ViewBag.Surname = HttpContext.Session.GetString("Surname");
            ViewBag.FirstName = HttpContext.Session.GetString("FirstName");

            if (!isVatInclusive.HasValue)
            {
                ViewBag.SelectedVatInclusive = null;
                ModelState.AddModelError(string.Empty, "Please select if FGN VAT is Inclusive or Exclusive.");
                return View(model);
            }

            ViewBag.SelectedVatInclusive = isVatInclusive.Value;

            var surname = HttpContext.Session.GetString("Surname");
            var firstname = HttpContext.Session.GetString("FirstName");
            var designation = HttpContext.Session.GetString("Designation");

            decimal originalInput = model.Amount;

            // =========================================================================
            // STAFF ADVANCE (SA-2) & IMPREST (31060101) BUDGET VALIDATION & DEDUCTION
            // =========================================================================
            bool isSa2Voucher = (model.VoucherType == "SA-2") ||
                               (!string.IsNullOrEmpty(model.VoucherNumber) && model.VoucherNumber.Contains("/SA-2/"));

            if (isSa2Voucher)
            {
                string payeeName = model.Payee?.Trim() ?? string.Empty;

                // 1. Check Staff Advance Budget by Payee
                var staffBudget = _context.StaffAdvanceBudgets
                    .FirstOrDefault(b => b.Payee != null && b.Payee.Trim().ToLower() == payeeName.ToLower());

                if (staffBudget != null)
                {
                    decimal payeeAvailableBudget = staffBudget.BudgetAmount ?? 0m;

                    if (originalInput > payeeAvailableBudget)
                    {
                        ViewBag.ErrorMessage = "Payee already exceeds budgeted amount.";
                        ModelState.AddModelError(string.Empty, "Payee already exceeds budgeted amount.");
                        return View(model);
                    }
                }

                // 2. Check Imprest Budget (Classification Code: 31060101)
                var imprestBudget = _context.OtherClassificationBudgets
                    .FirstOrDefault(b => b.ClassificationCode == "31060101");

                if (imprestBudget != null)
                {
                    if (imprestBudget.BudgetAmount <= 0)
                    {
                        ViewBag.ErrorMessage = "Amount budgeted for imprest is exhausted.";
                        ModelState.AddModelError(string.Empty, "Amount budgeted for imprest is exhausted.");
                        return View(model);
                    }

                    if (originalInput > imprestBudget.BudgetAmount)
                    {
                        ViewBag.ErrorMessage = "Amount budgeted for imprest is exhausted.";
                        ModelState.AddModelError(string.Empty, "Amount budgeted for imprest is exhausted.");
                        return View(model);
                    }
                }

                // 3. Apply deductions safely only after all checks pass
                if (staffBudget != null)
                {
                    staffBudget.BudgetAmount -= originalInput;
                    staffBudget.DateUpdated = DateTime.Now;
                }

                if (imprestBudget != null)
                {
                    imprestBudget.BudgetAmount -= originalInput;
                    imprestBudget.DateUpdated = DateTime.Now;
                }
            }
            // =========================================================================

            var (contractAmount, fgn_wht, edg_wht, stamp, vat, total) = CalculateFinancials(
                originalInput,
                isVatInclusive.Value,
                fgn_whtOption,
                edg_whtOption,
                vatOption,
                stampOption
            );

            if (model.Id == 0)
            {
                // Double check uniqueness right before insertion to cover concurrent form posts
                if (string.IsNullOrWhiteSpace(model.VoucherNumber) || _context.Vouchers.Any(v => v.VoucherNumber == model.VoucherNumber))
                {
                    model.VoucherNumber = GenerateUniqueVoucherNumber(model.VoucherType);
                }

                model.OriginalInputAmount = originalInput;
                model.IsVatInclusive = isVatInclusive.Value;
                model.Amount = contractAmount;
                model.FGN_WHT = fgn_wht;
                model.EDG_WHT = edg_wht;
                model.StampDuty = stamp;
                model.VAT = vat;
                model.TotalAmount = total;

                model.VatOption = vatOption;
                model.FgnWhtOption = fgn_whtOption;
                model.EdgWhtOption = edg_whtOption;
                model.StampOption = stampOption;

                model.Voucher_Surname = surname;
                model.Voucher_FirstName = firstname;
                model.Voucher_Designation = designation;

                _context.Vouchers.Add(model);

                var cert = new BillingCertification
                {
                    VoucherNumber = model.VoucherNumber,
                    CheckedLocation = model.CheckedLocation,
                    Station = model.Station,
                    AccountCode = model.AccountCode,
                    Head = model.Head,
                    SubHead = model.SubHead,
                    ClassificationCode = model.ClassificationCode,
                    SourceClassificationCode = model.SourceClassificationCode,
                    Date = model.Date,
                    Amount = model.Amount,
                    Payee = model.Payee,
                    Address = model.Address,
                    Description = model.Description,
                    FGN_WHT = model.FGN_WHT,
                    EDG_WHT = model.EDG_WHT,
                    StampDuty = model.StampDuty,
                    VAT = model.VAT,
                    TotalAmount = model.TotalAmount,
                    Voucher_Surname = surname,
                    Voucher_FirstName = firstname,
                    Voucher_Designation = designation
                };

                _context.BillingCertifications.Add(cert);

                try
                {
                    _context.SaveChanges();
                }
                catch (DbUpdateException)
                {
                    _context.ChangeTracker.Clear();
                    ViewBag.ErrorMessage = "An error occurred while saving the voucher record. Please try again.";
                    ModelState.AddModelError(string.Empty, "An error occurred while saving the voucher record.");
                    return View(model);
                }
            }
            else
            {
                var existingVoucher = _context.Vouchers.FirstOrDefault(v => v.Id == model.Id);
                if (existingVoucher == null) return RedirectToAction("Create");

                existingVoucher.VoucherType = model.VoucherType;
                existingVoucher.VoucherNumber = model.VoucherNumber;
                existingVoucher.CheckedLocation = model.CheckedLocation;
                existingVoucher.Station = model.Station;
                existingVoucher.AccountCode = model.AccountCode;
                existingVoucher.Head = model.Head;
                existingVoucher.SubHead = model.SubHead;
                existingVoucher.ClassificationCode = model.ClassificationCode;
                existingVoucher.SourceClassificationCode = model.SourceClassificationCode;
                existingVoucher.Date = model.Date;
                existingVoucher.OriginalInputAmount = originalInput;
                existingVoucher.IsVatInclusive = isVatInclusive.Value;
                existingVoucher.Amount = contractAmount;
                existingVoucher.Payee = model.Payee;
                existingVoucher.Address = model.Address;
                existingVoucher.Description = model.Description;
                existingVoucher.FGN_WHT = fgn_wht;
                existingVoucher.EDG_WHT = edg_wht;
                existingVoucher.StampDuty = stamp;
                existingVoucher.VAT = vat;
                existingVoucher.TotalAmount = total;

                existingVoucher.VatOption = vatOption;
                existingVoucher.FgnWhtOption = fgn_whtOption;
                existingVoucher.EdgWhtOption = edg_whtOption;
                existingVoucher.StampOption = stampOption;

                existingVoucher.Voucher_Surname = surname;
                existingVoucher.Voucher_FirstName = firstname;
                existingVoucher.Voucher_Designation = designation;

                var existingCert = _context.BillingCertifications.FirstOrDefault(c => c.VoucherNumber == model.VoucherNumber);
                if (existingCert != null)
                {
                    existingCert.CheckedLocation = model.CheckedLocation;
                    existingCert.Station = model.Station;
                    existingCert.AccountCode = model.AccountCode;
                    existingCert.Head = model.Head;
                    existingCert.SubHead = model.SubHead;
                    existingCert.ClassificationCode = model.ClassificationCode;
                    existingCert.SourceClassificationCode = model.SourceClassificationCode;
                    existingCert.Date = model.Date;
                    existingCert.Amount = contractAmount;
                    existingCert.Payee = model.Payee;
                    existingCert.Address = model.Address;
                    existingCert.Description = model.Description;
                    existingCert.FGN_WHT = fgn_wht;
                    existingCert.EDG_WHT = edg_wht;
                    existingCert.StampDuty = stamp;
                    existingCert.VAT = vat;
                    existingCert.TotalAmount = total;
                }

                try
                {
                    _context.SaveChanges();
                }
                catch (DbUpdateException)
                {
                    _context.ChangeTracker.Clear();
                    ViewBag.ErrorMessage = "An error occurred while updating the voucher.";
                    ModelState.AddModelError(string.Empty, "An error occurred while updating the voucher.");
                    return View(model);
                }
            }

            return RedirectToAction("Details", new { id = model.Id });
        }

        public IActionResult Details(int id)
        {
            var voucher = _context.Vouchers.FirstOrDefault(v => v.Id == id);
            if (voucher == null) return RedirectToAction("Create");
            return View(voucher);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(
            Voucher model,
            bool? isVatInclusive,
            string fgn_whtOption,
            string edg_whtOption,
            string vatOption,
            string stampOption)
        {
            ViewBag.Surname = HttpContext.Session.GetString("Surname");
            ViewBag.FirstName = HttpContext.Session.GetString("FirstName");

            if (!isVatInclusive.HasValue)
            {
                ViewBag.SelectedVatInclusive = null;
                ModelState.AddModelError(string.Empty, "Please select if FGN VAT is Inclusive or Exclusive.");
                return View("Create", model);
            }

            ViewBag.SelectedVatInclusive = isVatInclusive.Value;

            if (ModelState.IsValid)
            {
                var surname = HttpContext.Session.GetString("Surname");
                var firstname = HttpContext.Session.GetString("FirstName");
                var designation = HttpContext.Session.GetString("Designation");

                var voucher = _context.Vouchers.FirstOrDefault(x => x.Id == model.Id);
                if (voucher == null) return NotFound();

                decimal newOriginalInput = model.Amount;
                decimal oldOriginalInput = voucher.OriginalInputAmount;
                decimal diff = newOriginalInput - oldOriginalInput;

                // =========================================================================
                // STAFF ADVANCE (SA-2) & IMPREST (31060101) BUDGET VALIDATION (ON EDIT)
                // =========================================================================
                bool isSa2Voucher = (model.VoucherType == "SA-2") ||
                                   (!string.IsNullOrEmpty(model.VoucherNumber) && model.VoucherNumber.Contains("/SA-2/"));

                if (isSa2Voucher && diff != 0)
                {
                    string payeeName = model.Payee?.Trim() ?? string.Empty;

                    var staffBudget = _context.StaffAdvanceBudgets
                        .FirstOrDefault(b => b.Payee != null && b.Payee.Trim().ToLower() == payeeName.ToLower());

                    if (staffBudget != null)
                    {
                        decimal payeeAvailableBudget = staffBudget.BudgetAmount ?? 0m;

                        if (diff > payeeAvailableBudget)
                        {
                            ViewBag.ErrorMessage = "Payee already exceeds budgeted amount.";
                            ModelState.AddModelError(string.Empty, "Payee already exceeds budgeted amount.");
                            return View("Create", model);
                        }
                    }

                    var imprestBudget = _context.OtherClassificationBudgets
                        .FirstOrDefault(b => b.ClassificationCode == "31060101");

                    if (imprestBudget != null)
                    {
                        if (imprestBudget.BudgetAmount <= 0 || diff > imprestBudget.BudgetAmount)
                        {
                            ViewBag.ErrorMessage = "Amount budgeted for imprest is exhausted.";
                            ModelState.AddModelError(string.Empty, "Amount budgeted for imprest is exhausted.");
                            return View("Create", model);
                        }
                    }

                    if (staffBudget != null)
                    {
                        staffBudget.BudgetAmount -= diff;
                        staffBudget.DateUpdated = DateTime.Now;
                    }

                    if (imprestBudget != null)
                    {
                        imprestBudget.BudgetAmount -= diff;
                        imprestBudget.DateUpdated = DateTime.Now;
                    }
                }
                // =========================================================================

                var (contractAmount, fgn_wht, edg_wht, stamp, vat, total) = CalculateFinancials(
                    newOriginalInput,
                    isVatInclusive.Value,
                    fgn_whtOption,
                    edg_whtOption,
                    vatOption,
                    stampOption
                );

                voucher.VoucherType = model.VoucherType;
                voucher.VoucherNumber = model.VoucherNumber;
                voucher.CheckedLocation = model.CheckedLocation;
                voucher.Station = model.Station;
                voucher.AccountCode = model.AccountCode;
                voucher.Head = model.Head;
                voucher.SubHead = model.SubHead;
                voucher.ClassificationCode = model.ClassificationCode;
                voucher.SourceClassificationCode = model.SourceClassificationCode;
                voucher.Date = model.Date;
                voucher.OriginalInputAmount = newOriginalInput;
                voucher.IsVatInclusive = isVatInclusive.Value;
                voucher.Amount = contractAmount;
                voucher.Payee = model.Payee;
                voucher.Address = model.Address;
                voucher.Description = model.Description;
                voucher.FGN_WHT = fgn_wht;
                voucher.EDG_WHT = edg_wht;
                voucher.StampDuty = stamp;
                voucher.VAT = vat;
                voucher.TotalAmount = total;

                voucher.VatOption = vatOption;
                voucher.FgnWhtOption = fgn_whtOption;
                voucher.EdgWhtOption = edg_whtOption;
                voucher.StampOption = stampOption;

                voucher.Voucher_Surname = surname;
                voucher.Voucher_FirstName = firstname;
                voucher.Voucher_Designation = designation;

                var cert = _context.BillingCertifications.FirstOrDefault(c => c.VoucherNumber == voucher.VoucherNumber);
                if (cert != null)
                {
                    cert.CheckedLocation = model.CheckedLocation;
                    cert.Station = model.Station;
                    cert.AccountCode = model.AccountCode;
                    cert.Head = model.Head;
                    cert.SubHead = model.SubHead;
                    cert.ClassificationCode = model.ClassificationCode;
                    cert.SourceClassificationCode = model.SourceClassificationCode;
                    cert.Date = model.Date;
                    cert.Amount = contractAmount;
                    cert.Payee = model.Payee;
                    cert.Address = model.Address;
                    cert.Description = model.Description;
                    cert.FGN_WHT = fgn_wht;
                    cert.EDG_WHT = edg_wht;
                    cert.StampDuty = stamp;
                    cert.VAT = vat;
                    cert.TotalAmount = total;

                    cert.Voucher_Surname = surname;
                    cert.Voucher_FirstName = firstname;
                    cert.Voucher_Designation = designation;
                }

                _context.SaveChanges();
                return RedirectToAction("Details", new { id = voucher.Id });
            }

            return View("Create", model);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var voucher = _context.Vouchers.FirstOrDefault(x => x.Id == id);
            if (voucher == null) return NotFound();

            if (voucher.OriginalInputAmount > 0)
            {
                voucher.Amount = voucher.OriginalInputAmount;
            }
            else if (voucher.VAT > 0)
            {
                voucher.Amount = voucher.Amount + voucher.VAT;
            }

            ViewBag.SelectedVatInclusive = voucher.IsVatInclusive;

            return View("Create", voucher);
        }

        [HttpGet]
        public IActionResult SearchVoucher()
        {
            ViewBag.Surname = HttpContext.Session.GetString("Surname");
            ViewBag.FirstName = HttpContext.Session.GetString("FirstName");

            ViewBag.VoucherNumbers = _context.Vouchers
                .OrderByDescending(x => x.Id)
                .Select(x => x.VoucherNumber)
                .Distinct()
                .ToList();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SearchVoucher(string voucherNumber)
        {
            var voucher = _context.Vouchers.FirstOrDefault(x => x.VoucherNumber == voucherNumber);
            if (voucher == null)
            {
                ViewBag.Message = "No Record Found";
                ViewBag.VoucherNumbers = _context.Vouchers
                    .OrderByDescending(x => x.Id)
                    .Select(x => x.VoucherNumber)
                    .Distinct()
                    .ToList();
                return View();
            }

            return RedirectToAction("Edit", new { id = voucher.Id });
        }

        [HttpGet]
        public IActionResult LocateVoucher(string searchQuery, string searchPayee, DateTime? startDate, DateTime? endDate)
        {
            string query = !string.IsNullOrWhiteSpace(searchQuery) ? searchQuery : searchPayee;

            ViewBag.Surname = HttpContext.Session.GetString("Surname");
            ViewBag.FirstName = HttpContext.Session.GetString("FirstName");
            ViewBag.SearchQuery = query;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

            bool isSearched = !string.IsNullOrWhiteSpace(query) || startDate.HasValue || endDate.HasValue;
            ViewBag.IsSearched = isSearched;

            List<Voucher> results = new List<Voucher>();

            if (isSearched)
            {
                string trimmedQuery = query?.Trim() ?? string.Empty;
                bool hasQuery = !string.IsNullOrWhiteSpace(trimmedQuery);

                var q1 = _context.Vouchers.AsQueryable();
                var q2 = _context.BillingCertifications.AsQueryable();
                var q3 = _context.Audits.AsQueryable();
                var q4 = _context.CashRooms.AsQueryable();
                var q5 = _context.Final_Ledger.AsQueryable();
                var q6 = _context.Advances.AsQueryable();

                if (startDate.HasValue)
                {
                    q1 = q1.Where(x => x.Date >= startDate.Value);
                    q2 = q2.Where(x => x.Date >= startDate.Value);
                    q3 = q3.Where(x => x.Date >= startDate.Value);
                    q4 = q4.Where(x => x.Date >= startDate.Value);
                    q5 = q5.Where(x => x.Date >= startDate.Value);
                    q6 = q6.Where(x => x.Date >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    q1 = q1.Where(x => x.Date <= endDate.Value);
                    q2 = q2.Where(x => x.Date <= endDate.Value);
                    q3 = q3.Where(x => x.Date <= endDate.Value);
                    q4 = q4.Where(x => x.Date <= endDate.Value);
                    q5 = q5.Where(x => x.Date <= endDate.Value);
                    q6 = q6.Where(x => x.Date <= endDate.Value);
                }

                if (hasQuery)
                {
                    q1 = q1.Where(x => EF.Functions.Like(x.Payee, $"%{trimmedQuery}%") || EF.Functions.Like(x.VoucherNumber, $"%{trimmedQuery}%"));
                    q2 = q2.Where(x => EF.Functions.Like(x.Payee, $"%{trimmedQuery}%") || EF.Functions.Like(x.VoucherNumber, $"%{trimmedQuery}%"));
                    q3 = q3.Where(x => EF.Functions.Like(x.Payee, $"%{trimmedQuery}%") || EF.Functions.Like(x.VoucherNumber, $"%{trimmedQuery}%"));
                    q4 = q4.Where(x => EF.Functions.Like(x.Payee, $"%{trimmedQuery}%") || EF.Functions.Like(x.VoucherNumber, $"%{trimmedQuery}%"));
                    q5 = q5.Where(x => EF.Functions.Like(x.Payee, $"%{trimmedQuery}%") || EF.Functions.Like(x.VoucherNumber, $"%{trimmedQuery}%"));
                    q6 = q6.Where(x => EF.Functions.Like(x.Payee, $"%{trimmedQuery}%") || EF.Functions.Like(x.VoucherNumber, $"%{trimmedQuery}%"));
                }

                var list1 = q1.ToList();
                var list2 = q2.Select(x => new Voucher { Id = x.Id, VoucherNumber = x.VoucherNumber, Payee = x.Payee, Description = x.Description, Address = x.Address, Amount = x.Amount, Date = x.Date }).ToList();
                var list3 = q3.Select(x => new Voucher { Id = x.Id, VoucherNumber = x.VoucherNumber, Payee = x.Payee, Description = x.Description, Address = x.Address, Amount = x.Amount, Date = x.Date }).ToList();
                var list4 = q4.Select(x => new Voucher { Id = x.Id, VoucherNumber = x.VoucherNumber, Payee = x.Payee, Description = x.Description, Address = x.Address, Amount = x.Amount, Date = x.Date }).ToList();
                var list5 = q5.Select(x => new Voucher { Id = x.Id, VoucherNumber = x.VoucherNumber, Payee = x.Payee, Description = x.Description, Address = x.Address, Amount = x.Amount, Date = x.Date }).ToList();
                var list6 = q6.Select(x => new Voucher { Id = x.Id, VoucherNumber = x.VoucherNumber, Payee = x.Payee, Description = x.Description, Address = x.Address, Amount = x.Amount, Date = x.Date }).ToList();

                results = list1.Concat(list2).Concat(list3).Concat(list4).Concat(list5).Concat(list6)
                               .OrderByDescending(x => x.Date)
                               .GroupBy(x => x.VoucherNumber)
                               .Select(g => g.First())
                               .ToList();
            }

            return View(results);
        }

        [HttpGet]
        public IActionResult CheckStatus(string voucherNumber)
        {
            if (string.IsNullOrEmpty(voucherNumber)) return RedirectToAction("LocateVoucher");

            ViewBag.Surname = HttpContext.Session.GetString("Surname");
            ViewBag.FirstName = HttpContext.Session.GetString("FirstName");
            ViewBag.VoucherNumber = voucherNumber;

            string statusMessage = "";

            bool inBillingCert = _context.BillingCertifications.Any(x => x.VoucherNumber == voucherNumber);
            if (inBillingCert)
            {
                statusMessage = "The specific voucher searched for is still in Billing Certification Stage";
            }
            else
            {
                bool inAudit = _context.Audits.Any(x => x.VoucherNumber == voucherNumber);
                if (inAudit)
                {
                    statusMessage = "The specific voucher searched for is still in Auditing Stage.";
                }
                else
                {
                    bool inCashroom = _context.CashRooms.Any(x => x.VoucherNumber == voucherNumber);
                    if (inCashroom)
                    {
                        statusMessage = "The specific voucher searched for is in Cash Room for payment.";
                    }
                    else
                    {
                        bool inFinalLedger = _context.Final_Ledger.Any(x => x.VoucherNumber == voucherNumber);
                        bool inAdvances = _context.Advances.Any(x => x.VoucherNumber == voucherNumber);

                        if (inFinalLedger || inAdvances)
                        {
                            statusMessage = "The Voucher has been paid.";
                        }
                        else
                        {
                            statusMessage = "The specific voucher is yet to be raised.";
                        }
                    }
                }
            }

            ViewBag.StatusMessage = statusMessage;
            return View();
        }

        [HttpGet]
        public IActionResult GetPayees()
        {
            var payees = _context.Vouchers
                .Where(v => !string.IsNullOrEmpty(v.Payee))
                .Select(v => v.Payee)
                .Distinct()
                .OrderBy(p => p)
                .ToList();

            return Json(payees);
        }

        public IActionResult Close()
        {
            return RedirectToAction("Index", "Dashboard");
        }
    }
}