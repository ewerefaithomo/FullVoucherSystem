using FullVoucherSystem.Data;
using FullVoucherSystem.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace FullVoucherSystem.Controllers
{
    public class BillingCertificationController : Controller
    {
        private readonly AppDbContext _context;

        public BillingCertificationController(AppDbContext context)
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
            {
                decimal.TryParse(fgn_whtOption.Replace("%", ""), out fgn_whtPercentage);
            }

            if (!string.IsNullOrWhiteSpace(edg_whtOption))
            {
                decimal.TryParse(edg_whtOption.Replace("%", ""), out edg_whtPercentage);
            }

            if (!string.IsNullOrWhiteSpace(stampOption))
            {
                decimal.TryParse(stampOption.Replace("%", ""), out stampPercentage);
            }

            if (!string.IsNullOrWhiteSpace(vatOption))
            {
                decimal.TryParse(vatOption.Replace("%", ""), out vatPercentage);
            }

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

        // ✅ LIST ALL RECORDS
        public IActionResult Index()
        {
            ViewBag.Surname = HttpContext.Session.GetString("Surname");
            ViewBag.FirstName = HttpContext.Session.GetString("FirstName");

            var list = _context.BillingCertifications
                .OrderByDescending(x => x.Id)
                .ToList();

            return View(list);
        }

        // ✅ OPEN DETAILS (EDIT MODE)
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var data = _context.BillingCertifications
                               .FirstOrDefault(x => x.Id == id);

            if (data == null)
            {
                return NotFound();
            }

            return View(data);
        }

        // =======================================
        // EDIT POST ACTION (SUBMIT CERTIFICATION)
        // =======================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(BillingCertification model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var surname = HttpContext.Session.GetString("Surname");
            var firstname = HttpContext.Session.GetString("FirstName");
            var designation = HttpContext.Session.GetString("Designation");

            var billingCertification = _context.BillingCertifications
                .FirstOrDefault(x => x.Id == model.Id);

            if (billingCertification == null)
            {
                return NotFound();
            }

            // Update billing certification fields from submitted model
            billingCertification.VoucherNumber = model.VoucherNumber;
            billingCertification.CheckedLocation = model.CheckedLocation;
            billingCertification.Station = model.Station;
            billingCertification.AccountCode = model.AccountCode;
            billingCertification.Head = model.Head;
            billingCertification.SubHead = model.SubHead;
            billingCertification.ClassificationCode = model.ClassificationCode;
            billingCertification.SourceClassificationCode = model.SourceClassificationCode;
            billingCertification.Date = model.Date;
            billingCertification.Amount = model.Amount;
            billingCertification.Payee = model.Payee;
            billingCertification.Address = model.Address;
            billingCertification.Description = model.Description;
            billingCertification.FGN_WHT = model.FGN_WHT;
            billingCertification.EDG_WHT = model.EDG_WHT;
            billingCertification.StampDuty = model.StampDuty;
            billingCertification.VAT = model.VAT;
            billingCertification.TotalAmount = model.TotalAmount;
            billingCertification.QueryText = model.QueryText;

            billingCertification.Bill_Surname = surname;
            billingCertification.Bill_FirstName = firstname;
            billingCertification.Bill_Designation = designation;

            // 1. UPDATE MASTER RECORD IN VOUCHERS TABLE
            var existingVoucher = _context.Vouchers
                .FirstOrDefault(v => v.VoucherNumber == billingCertification.VoucherNumber);

            if (existingVoucher != null)
            {
                existingVoucher.CheckedLocation = billingCertification.CheckedLocation;
                existingVoucher.Station = billingCertification.Station;
                existingVoucher.AccountCode = billingCertification.AccountCode;
                existingVoucher.Head = billingCertification.Head;
                existingVoucher.SubHead = billingCertification.SubHead;
                existingVoucher.ClassificationCode = billingCertification.ClassificationCode;
                existingVoucher.SourceClassificationCode = billingCertification.SourceClassificationCode;
                existingVoucher.Date = billingCertification.Date;
                existingVoucher.Amount = billingCertification.Amount;
                existingVoucher.Payee = billingCertification.Payee;
                existingVoucher.Address = billingCertification.Address;
                existingVoucher.Description = billingCertification.Description;
                existingVoucher.FGN_WHT = billingCertification.FGN_WHT;
                existingVoucher.EDG_WHT = billingCertification.EDG_WHT;
                existingVoucher.StampDuty = billingCertification.StampDuty;
                existingVoucher.VAT = billingCertification.VAT;
                existingVoucher.TotalAmount = billingCertification.TotalAmount;

                _context.Vouchers.Update(existingVoucher);
            }

            // 2. CREATE OR UPDATE RECORD IN AUDITS TABLE
            var existingAudit = _context.Audits
                .FirstOrDefault(a => a.VoucherNumber == billingCertification.VoucherNumber);

            if (existingAudit != null)
            {
                existingAudit.CheckedLocation = billingCertification.CheckedLocation;
                existingAudit.Station = billingCertification.Station;
                existingAudit.AccountCode = billingCertification.AccountCode;
                existingAudit.Head = billingCertification.Head;
                existingAudit.SubHead = billingCertification.SubHead;
                existingAudit.ClassificationCode = billingCertification.ClassificationCode;
                existingAudit.SourceClassificationCode = billingCertification.SourceClassificationCode;
                existingAudit.Date = billingCertification.Date;
                existingAudit.Amount = billingCertification.Amount;
                existingAudit.Payee = billingCertification.Payee;
                existingAudit.Address = billingCertification.Address;
                existingAudit.Description = billingCertification.Description;
                existingAudit.FGN_WHT = billingCertification.FGN_WHT;
                existingAudit.EDG_WHT = billingCertification.EDG_WHT;
                existingAudit.StampDuty = billingCertification.StampDuty;
                existingAudit.VAT = billingCertification.VAT;
                existingAudit.TotalAmount = billingCertification.TotalAmount;

                existingAudit.Voucher_Surname = billingCertification.Voucher_Surname;
                existingAudit.Voucher_FirstName = billingCertification.Voucher_FirstName;
                existingAudit.Voucher_Designation = billingCertification.Voucher_Designation;
                existingAudit.Bill_Surname = surname;
                existingAudit.Bill_FirstName = firstname;
                existingAudit.Bill_Designation = designation;

                existingAudit.QueryText = billingCertification.QueryText;
                existingAudit.CertificationText = "I certify that the above amount is correct and may be paid.";
                existingAudit.CertifiedDate = DateTime.Now;

                _context.Audits.Update(existingAudit);
            }
            else
            {
                var auditRecord = new Audit
                {
                    VoucherNumber = billingCertification.VoucherNumber,
                    CheckedLocation = billingCertification.CheckedLocation,
                    Station = billingCertification.Station,
                    AccountCode = billingCertification.AccountCode,
                    Head = billingCertification.Head,
                    SubHead = billingCertification.SubHead,
                    ClassificationCode = billingCertification.ClassificationCode,
                    SourceClassificationCode = billingCertification.SourceClassificationCode,
                    Date = billingCertification.Date,
                    Amount = billingCertification.Amount,
                    Payee = billingCertification.Payee,
                    Address = billingCertification.Address,
                    Description = billingCertification.Description,
                    FGN_WHT = billingCertification.FGN_WHT,
                    EDG_WHT = billingCertification.EDG_WHT,
                    StampDuty = billingCertification.StampDuty,
                    VAT = billingCertification.VAT,
                    TotalAmount = billingCertification.TotalAmount,
                    Voucher_Surname = billingCertification.Voucher_Surname,
                    Voucher_FirstName = billingCertification.Voucher_FirstName,
                    Voucher_Designation = billingCertification.Voucher_Designation,
                    Bill_Surname = surname,
                    Bill_FirstName = firstname,
                    Bill_Designation = designation,
                    QueryText = billingCertification.QueryText,
                    CertificationText = "I certify that the above amount is correct and may be paid.",
                    CertifiedDate = DateTime.Now
                };

                _context.Audits.Add(auditRecord);
            }

            // 3. REMOVE FROM BILLING CERTIFICATIONS TABLE UPON SUCCESSFUL CERTIFICATION
            _context.BillingCertifications.Remove(billingCertification);
            _context.SaveChanges();

            return Json(new
            {
                success = true,
                voucherNumber = billingCertification.VoucherNumber
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SendToQuery(BillingCertification model)
        {
            if (string.IsNullOrWhiteSpace(model.QueryText))
            {
                TempData["Message"] = "Query text cannot be empty.";
                return RedirectToAction("Edit", new { id = model.Id });
            }

            var billingCert = _context.BillingCertifications.FirstOrDefault(x => x.Id == model.Id);

            if (billingCert == null)
            {
                TempData["Message"] = "Billing certification record not found.";
                return RedirectToAction("Index");
            }

            billingCert.QueryText = model.QueryText;

            var query = new QueryInformation
            {
                VoucherNumber = billingCert.VoucherNumber,
                QueryText = billingCert.QueryText,
                CreatedDate = DateTime.Now
            };

            _context.QueryInformations.Add(query);
            _context.BillingCertifications.Remove(billingCert);

            _context.SaveChanges();

            TempData["Message"] = "Voucher sent to query successfully.";

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult PrintAudit(string voucherNumber)
        {
            if (string.IsNullOrWhiteSpace(voucherNumber))
                return RedirectToAction("Index");

            var voucher = _context.Audits
                                  .FirstOrDefault(x => x.VoucherNumber == voucherNumber);

            if (voucher == null)
            {
                TempData["Message"] = "Voucher not found.";
                return RedirectToAction("Index");
            }

            return View(voucher);
        }

        [HttpGet]
        public IActionResult QueriedVouchers()
        {
            ViewBag.Surname = HttpContext.Session.GetString("Surname");
            ViewBag.FirstName = HttpContext.Session.GetString("FirstName");

            var queriedVouchers = _context.QueryInformations
                                          .OrderByDescending(x => x.CreatedDate)
                                          .ToList();

            return View(queriedVouchers);
        }

        [HttpGet]
        public IActionResult UpdateQueriedVoucher(int id)
        {
            ViewBag.Surname = HttpContext.Session.GetString("Surname");
            ViewBag.FirstName = HttpContext.Session.GetString("FirstName");
            ViewBag.Designation = HttpContext.Session.GetString("Designation");

            var queryItem = _context.QueryInformations.FirstOrDefault(q => q.Id == id);
            if (queryItem == null) return NotFound();

            var voucher = _context.Vouchers.FirstOrDefault(v => v.VoucherNumber == queryItem.VoucherNumber);
            if (voucher == null) return NotFound();

            var viewModel = new Voucher
            {
                Id = voucher.Id,
                VoucherType = voucher.VoucherType,
                VoucherNumber = voucher.VoucherNumber,
                CheckedLocation = voucher.CheckedLocation,
                Station = voucher.Station,
                AccountCode = voucher.AccountCode,
                Head = voucher.Head,
                SubHead = voucher.SubHead,
                ClassificationCode = voucher.ClassificationCode,
                SourceClassificationCode = voucher.SourceClassificationCode,
                Date = voucher.Date,
                Amount = voucher.Amount + voucher.VAT,
                Payee = voucher.Payee,
                Address = voucher.Address,
                Description = voucher.Description,
                FGN_WHT = voucher.FGN_WHT,
                EDG_WHT = voucher.EDG_WHT,
                StampDuty = voucher.StampDuty,
                VAT = voucher.VAT,
                TotalAmount = voucher.TotalAmount
            };

            if (voucher.VAT > 0)
            {
                ViewBag.SelectedVatInclusive = true;
            }

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Print(int id)
        {
            var voucher = _context.BillingCertifications
                                  .SingleOrDefault(x => x.Id == id);

            if (voucher == null)
            {
                TempData["Message"] = "Pending voucher not found.";
                return RedirectToAction("Index");
            }

            return View(voucher);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateQueriedVoucher(
            Voucher model,
            bool? isVatInclusive,
            string fgn_whtOption,
            string edg_whtOption,
            string vatOption,
            string stampOption)
        {
            if (!isVatInclusive.HasValue)
            {
                TempData["Message"] = "Please select if FGN VAT is Inclusive or Exclusive.";
                return RedirectToAction("UpdateQueriedVoucher", new { id = model.Id });
            }

            if (string.IsNullOrWhiteSpace(model.VoucherNumber))
            {
                TempData["Message"] = "Invalid voucher number.";
                return RedirectToAction("QueriedVouchers");
            }

            var voucher = _context.Vouchers
                .FirstOrDefault(v => v.VoucherNumber == model.VoucherNumber);

            if (voucher == null)
            {
                TempData["Message"] = "Voucher not found.";
                return RedirectToAction("QueriedVouchers");
            }

            var surname = HttpContext.Session.GetString("Surname");
            var firstname = HttpContext.Session.GetString("FirstName");
            var designation = HttpContext.Session.GetString("Designation");

            var (contractAmount, fgn_wht, edg_wht, stamp, vat, total) = CalculateFinancials(
                model.Amount,
                isVatInclusive.Value,
                fgn_whtOption,
                edg_whtOption,
                vatOption,
                stampOption
            );

            voucher.VoucherType = model.VoucherType;
            voucher.CheckedLocation = model.CheckedLocation;
            voucher.Station = model.Station;
            voucher.AccountCode = model.AccountCode;
            voucher.Head = model.Head;
            voucher.SubHead = model.SubHead;
            voucher.ClassificationCode = model.ClassificationCode;
            voucher.SourceClassificationCode = model.SourceClassificationCode;
            voucher.Date = model.Date;
            voucher.Amount = contractAmount;
            voucher.Payee = model.Payee;
            voucher.Address = model.Address;
            voucher.Description = model.Description;
            voucher.FGN_WHT = fgn_wht;
            voucher.EDG_WHT = edg_wht;
            voucher.StampDuty = stamp;
            voucher.VAT = vat;
            voucher.TotalAmount = total;

            voucher.Voucher_Surname = surname;
            voucher.Voucher_FirstName = firstname;
            voucher.Voucher_Designation = designation;

            var cert = _context.BillingCertifications
                .FirstOrDefault(x => x.VoucherNumber == model.VoucherNumber);

            if (cert != null)
            {
                cert.CheckedLocation = voucher.CheckedLocation;
                cert.Station = voucher.Station;
                cert.AccountCode = voucher.AccountCode;
                cert.Head = voucher.Head;
                cert.SubHead = voucher.SubHead;
                cert.ClassificationCode = voucher.ClassificationCode;
                cert.SourceClassificationCode = voucher.SourceClassificationCode;
                cert.Date = voucher.Date;
                cert.Amount = contractAmount;
                cert.Payee = model.Payee;
                cert.Address = model.Address;
                cert.Description = model.Description;
                cert.FGN_WHT = fgn_wht;
                cert.EDG_WHT = edg_wht;
                cert.VAT = vat;
                cert.StampDuty = stamp;
                cert.TotalAmount = total;
                cert.Voucher_Surname = surname;
                cert.Voucher_FirstName = firstname;
                cert.Voucher_Designation = designation;
            }
            else
            {
                var newCert = new BillingCertification
                {
                    VoucherNumber = voucher.VoucherNumber,
                    CheckedLocation = voucher.CheckedLocation,
                    Station = voucher.Station,
                    AccountCode = voucher.AccountCode,
                    Head = voucher.Head,
                    SubHead = voucher.SubHead,
                    ClassificationCode = voucher.ClassificationCode,
                    SourceClassificationCode = voucher.SourceClassificationCode,
                    Date = voucher.Date,
                    Amount = contractAmount,
                    Payee = model.Payee,
                    Address = model.Address,
                    Description = model.Description,
                    FGN_WHT = fgn_wht,
                    EDG_WHT = edg_wht,
                    StampDuty = stamp,
                    VAT = vat,
                    TotalAmount = total,
                    Voucher_Surname = surname,
                    Voucher_FirstName = firstname,
                    Voucher_Designation = designation
                };
                _context.BillingCertifications.Add(newCert);
            }

            var query = _context.QueryInformations
                .FirstOrDefault(q => q.VoucherNumber == model.VoucherNumber);

            if (query != null)
            {
                _context.QueryInformations.Remove(query);
            }

            _context.SaveChanges();

            TempData["Message"] = "Voucher information updated and sent to Billing Certification successfully.";
            return RedirectToAction("QueriedVouchers");
        }

        [HttpGet]
        public IActionResult DeleteQuery(int id)
        {
            var query = _context.QueryInformations
                .FirstOrDefault(x => x.Id == id);

            if (query == null)
                return RedirectToAction("QueriedVouchers");

            _context.QueryInformations.Remove(query);
            _context.SaveChanges();

            TempData["Message"] = "Queried information deleted successfully.";

            return RedirectToAction("QueriedVouchers");
        }
    }
}