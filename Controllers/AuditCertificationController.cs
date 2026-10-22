using FullVoucherSystem.Data;
using FullVoucherSystem.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace FullVoucherSystem.Controllers
{
    public class AuditCertificationController : Controller
    {
        private readonly AppDbContext _context;

        public AuditCertificationController(AppDbContext context)
        {
            _context = context;
        }


        // ============================================================
        // HELPER METHOD FOR FINANCIAL CALCULATIONS
        // ============================================================

        private (
            decimal contractAmount,
            decimal fgn_wht,
            decimal edg_wht,
            decimal stamp,
            decimal vat,
            decimal total
        ) CalculateFinancials(
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
                decimal.TryParse(
                    fgn_whtOption.Replace("%", ""),
                    out fgn_whtPercentage);
            }

            if (!string.IsNullOrWhiteSpace(edg_whtOption))
            {
                decimal.TryParse(
                    edg_whtOption.Replace("%", ""),
                    out edg_whtPercentage);
            }

            if (!string.IsNullOrWhiteSpace(stampOption))
            {
                decimal.TryParse(
                    stampOption.Replace("%", ""),
                    out stampPercentage);
            }

            if (!string.IsNullOrWhiteSpace(vatOption))
            {
                decimal.TryParse(
                    vatOption.Replace("%", ""),
                    out vatPercentage);
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

                vat =
                    (vatPercentage / 107.5m)
                    * grossAmount;

                contractAmount =
                    grossAmount - vat;

                fgn_wht =
                    (fgn_whtPercentage / 107.5m)
                    * grossAmount;

                edg_wht =
                    (edg_whtPercentage / 107.5m)
                    * grossAmount;

                stamp =
                    (stampPercentage / 107.5m)
                    * grossAmount;

                total =
                    grossAmount
                    - vat
                    - fgn_wht
                    - edg_wht
                    - stamp;
            }
            else
            {
                contractAmount = inputAmount;

                fgn_wht =
                    contractAmount
                    * (fgn_whtPercentage / 100m);

                edg_wht =
                    contractAmount
                    * (edg_whtPercentage / 100m);

                stamp =
                    contractAmount
                    * (stampPercentage / 100m);

                vat =
                    contractAmount
                    * (vatPercentage / 100m);

                total =
                    contractAmount
                    - edg_wht
                    - fgn_wht
                    - stamp;
            }

            return (
                contractAmount,
                fgn_wht,
                edg_wht,
                stamp,
                vat,
                total
            );
        }


        // ============================================================
        // LIST ALL AUDIT RECORDS
        // ============================================================

        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.Surname =
                HttpContext.Session.GetString("Surname");

            ViewBag.FirstName =
                HttpContext.Session.GetString("FirstName");

            var list =
                _context.Audits
                    .OrderByDescending(x => x.Id)
                    .ToList();

            return View(list);
        }


        // ============================================================
        // GET: EDIT / AUDIT CERTIFICATION
        // ============================================================

        [HttpGet]
        public IActionResult Edit(int id)
        {
            ViewBag.Surname =
                HttpContext.Session.GetString("Surname");

            ViewBag.FirstName =
                HttpContext.Session.GetString("FirstName");

            var audit =
                _context.Audits
                    .FirstOrDefault(x => x.Id == id);

            if (audit == null)
            {
                TempData["Message"] =
                    "Voucher record not found.";

                return RedirectToAction(nameof(Index));
            }

            return View(audit);
        }


        // ============================================================
        // POST: EDIT / CERTIFY
        //
        // ALL FIELDS MAY BE EDITED EXCEPT VOUCHER NUMBER.
        //
        // WHEN CERTIFIED:
        //
        // 1. Audit record is updated.
        // 2. Corresponding Vouchers record is updated.
        // 3. CashRoom record is created.
        // 4. Audit record is removed.
        //
        // ALL OPERATIONS ARE PERFORMED IN ONE TRANSACTION.
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Audit model)
        {
            // ========================================================
            // LOCATE THE ORIGINAL AUDIT RECORD
            // ========================================================

            var auditRecord =
                _context.Audits
                    .FirstOrDefault(x => x.Id == model.Id);

            if (auditRecord == null)
            {
                TempData["Message"] =
                    "Voucher record not found.";

                return RedirectToAction(nameof(Index));
            }


            // ========================================================
            // IMPORTANT SECURITY RULE
            //
            // NEVER TRUST VoucherNumber FROM THE FORM.
            //
            // The original VoucherNumber in the database is used.
            // ========================================================

            string originalVoucherNumber =
                auditRecord.VoucherNumber;


            if (string.IsNullOrWhiteSpace(originalVoucherNumber))
            {
                TempData["Message"] =
                    "The voucher does not have a valid Voucher Number.";

                return RedirectToAction(
                    nameof(Edit),
                    new { id = model.Id });
            }


            // ========================================================
            // QUERY VALIDATION
            // ========================================================

            if (string.IsNullOrWhiteSpace(model.QueryText))
            {
                TempData["Message"] =
                    "Please enter NIL if there is no query, or enter the query/remark.";

                return RedirectToAction(
                    nameof(Edit),
                    new { id = model.Id });
            }


            bool isNil =
                model.QueryText
                    .Trim()
                    .Equals(
                        "NIL",
                        StringComparison.OrdinalIgnoreCase);


            // ========================================================
            // UPDATE AUDIT RECORD WITH THE EDITED INFORMATION
            // ========================================================

            auditRecord.CheckedLocation =
                model.CheckedLocation;

            auditRecord.Station =
                model.Station;

            auditRecord.Head =
                model.Head;

            auditRecord.SubHead =
                model.SubHead;

            auditRecord.ClassificationCode =
                model.ClassificationCode;

            auditRecord.SourceClassificationCode =
                model.SourceClassificationCode;


            if (model.Date != default)
            {
                auditRecord.Date =
                    model.Date.Date;
            }


            if (model.CertifiedDate != default)
            {
                auditRecord.CertifiedDate =
                    model.CertifiedDate.Date;
            }


            auditRecord.Payee =
                model.Payee;

            auditRecord.Address =
                model.Address;

            auditRecord.Description =
                model.Description;

            auditRecord.Amount =
                model.Amount;

            auditRecord.VAT =
                model.VAT;

            auditRecord.FGN_WHT =
                model.FGN_WHT;

            auditRecord.EDG_WHT =
                model.EDG_WHT;

            auditRecord.StampDuty =
                model.StampDuty;

            auditRecord.TotalAmount =
                model.TotalAmount;

            auditRecord.QueryText =
                model.QueryText;


            // ========================================================
            // IF QUERY IS NOT NIL
            //
            // DO NOT CERTIFY.
            // ========================================================

            if (!isNil)
            {
                TempData["Message"] =
                    "The voucher contains a query. Use 'Send To Query' to submit it.";

                return RedirectToAction(
                    nameof(Edit),
                    new { id = model.Id });
            }


            // ========================================================
            // CERTIFICATION PROCESS
            //
            // QUERY = NIL
            // ========================================================

            using var transaction =
                _context.Database.BeginTransaction();

            try
            {
                // ====================================================
                // STEP 1:
                // LOCATE THE ORIGINAL VOUCHER
                // ====================================================

                var voucher =
                    _context.Vouchers
                        .FirstOrDefault(
                            v => v.VoucherNumber ==
                                 originalVoucherNumber);

                if (voucher == null)
                {
                    transaction.Rollback();

                    TempData["Message"] =
                        "The corresponding voucher was not found in the Vouchers table. Certification was not completed.";

                    return RedirectToAction(
                        nameof(Edit),
                        new { id = model.Id });
                }


                // ====================================================
                // STEP 2:
                // UPDATE THE VOUCHERS TABLE
                //
                // VoucherNumber is deliberately NOT changed.
                // The existing Vouchers record is updated.
                // ====================================================

                voucher.CheckedLocation =
                    auditRecord.CheckedLocation;

                voucher.Station =
                    auditRecord.Station;

                voucher.Head =
                    auditRecord.Head;

                voucher.SubHead =
                    auditRecord.SubHead;

                voucher.ClassificationCode =
                    auditRecord.ClassificationCode;

                voucher.SourceClassificationCode =
                    auditRecord.SourceClassificationCode;

                voucher.Date =
                    auditRecord.Date;

                voucher.Amount =
                    auditRecord.Amount;

                voucher.Payee =
                    auditRecord.Payee;

                voucher.Address =
                    auditRecord.Address;

                voucher.Description =
                    auditRecord.Description;

                voucher.FGN_WHT =
                    auditRecord.FGN_WHT;

                voucher.EDG_WHT =
                    auditRecord.EDG_WHT;

                voucher.VAT =
                    auditRecord.VAT;

                voucher.StampDuty =
                    auditRecord.StampDuty;

                voucher.TotalAmount =
                    auditRecord.TotalAmount;

                voucher.Voucher_FirstName =
                    auditRecord.Voucher_FirstName;

                voucher.Voucher_Surname =
                    auditRecord.Voucher_Surname;

                voucher.Voucher_Designation =
                    auditRecord.Voucher_Designation;


                // ====================================================
                // DO NOT CHANGE:
                //
                // voucher.VoucherNumber
                //
                // The original VoucherNumber remains intact.
                // ====================================================


                // ====================================================
                // STEP 3:
                // CREATE CASH ROOM RECORD
                // ====================================================

                var cashRoomRecord =
                    new CashRoom
                    {
                        VoucherNumber =
                            originalVoucherNumber,

                        CheckedLocation =
                            auditRecord.CheckedLocation,

                        Station =
                            auditRecord.Station,

                        Head =
                            auditRecord.Head,

                        SubHead =
                            auditRecord.SubHead,

                        ClassificationCode =
                            auditRecord.ClassificationCode,

                        SourceClassificationCode =
                            auditRecord.SourceClassificationCode,

                        Date =
                            auditRecord.Date,

                        Amount =
                            auditRecord.Amount,

                        Payee =
                            auditRecord.Payee,

                        Address =
                            auditRecord.Address,

                        Description =
                            auditRecord.Description,

                        FGN_WHT =
                            auditRecord.FGN_WHT,

                        EDG_WHT =
                            auditRecord.EDG_WHT,

                        VAT =
                            auditRecord.VAT,

                        StampDuty =
                            auditRecord.StampDuty,

                        TotalAmount =
                            auditRecord.TotalAmount,

                        QueryText =
                            "NIL",

                        Voucher_FirstName =
                            auditRecord.Voucher_FirstName,

                        Voucher_Surname =
                            auditRecord.Voucher_Surname,

                        Voucher_Designation =
                            auditRecord.Voucher_Designation,

                        Bill_FirstName =
                            auditRecord.Bill_FirstName,

                        Bill_Surname =
                            auditRecord.Bill_Surname,

                        Bill_Designation =
                            auditRecord.Bill_Designation,

                        CertifiedDate =
                            auditRecord.CertifiedDate != default
                                ? auditRecord.CertifiedDate
                                : DateTime.Now
                    };


                _context.CashRooms.Add(
                    cashRoomRecord);


                // ====================================================
                // STEP 4:
                // REMOVE VOUCHER FROM AUDIT
                // ====================================================

                _context.Audits.Remove(
                    auditRecord);


                // ====================================================
                // STEP 5:
                // SAVE EVERYTHING
                // ====================================================

                _context.SaveChanges();


                // ====================================================
                // STEP 6:
                // COMMIT TRANSACTION
                // ====================================================

                transaction.Commit();


                TempData["SuccessMessage"] =
                    "Voucher successfully updated, certified, synchronized with the Vouchers table, and forwarded to Cash Room.";

                return RedirectToAction(
                    nameof(Index));
            }
            catch (Exception ex)
            {
                // ====================================================
                // ROLLBACK IF ANY PART OF THE OPERATION FAILS
                // ====================================================

                transaction.Rollback();

                TempData["Message"] =
                    "Certification failed. No changes were committed. Error: "
                    + ex.Message;

                return RedirectToAction(
                    nameof(Edit),
                    new { id = model.Id });
            }
        }


        // ============================================================
        // SEND VOUCHER TO QUERY
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SendToQuery(Audit model)
        {
            var audit =
                _context.Audits
                    .FirstOrDefault(
                        a => a.Id == model.Id);

            if (audit == null)
            {
                return NotFound();
            }


            if (string.IsNullOrWhiteSpace(model.QueryText))
            {
                TempData["Message"] =
                    "Query text cannot be empty.";

                return RedirectToAction(
                    nameof(Edit),
                    new { id = model.Id });
            }


            // ========================================================
            // UPDATE EDITABLE FIELDS
            // ========================================================

            audit.CheckedLocation =
                model.CheckedLocation;

            audit.Station =
                model.Station;

            audit.Head =
                model.Head;

            audit.SubHead =
                model.SubHead;

            audit.ClassificationCode =
                model.ClassificationCode;

            audit.SourceClassificationCode =
                model.SourceClassificationCode;


            if (model.Date != default)
            {
                audit.Date =
                    model.Date.Date;
            }


            if (model.CertifiedDate != default)
            {
                audit.CertifiedDate =
                    model.CertifiedDate.Date;
            }


            audit.Payee =
                model.Payee;

            audit.Address =
                model.Address;

            audit.Description =
                model.Description;

            audit.Amount =
                model.Amount;

            audit.FGN_WHT =
                model.FGN_WHT;

            audit.EDG_WHT =
                model.EDG_WHT;

            audit.VAT =
                model.VAT;

            audit.StampDuty =
                model.StampDuty;

            audit.TotalAmount =
                model.TotalAmount;

            audit.QueryText =
                model.QueryText;


            // ========================================================
            // CREATE QUERY RECORD
            // ========================================================

            var query =
                new AuditQueryInformation
                {
                    VoucherNumber =
                        audit.VoucherNumber,

                    QueryText =
                        model.QueryText,

                    CreatedDate =
                        DateTime.Now
                };


            _context.AuditQueryInformations.Add(
                query);


            // ========================================================
            // REMOVE FROM AUDIT
            // ========================================================

            _context.Audits.Remove(
                audit);


            _context.SaveChanges();


            TempData["Message"] =
                "Query information submitted successfully and voucher moved to queries.";

            return RedirectToAction(
                nameof(Index));
        }


        // ============================================================
        // LIST QUERIED VOUCHERS
        // ============================================================

        [HttpGet]
        public IActionResult AuditQueriedVouchers()
        {
            var auditQueriedVouchers =
                _context.AuditQueryInformations
                    .OrderByDescending(
                        x => x.CreatedDate)
                    .ToList();

            return View(
                auditQueriedVouchers);
        }


        // ============================================================
        // GET: OPEN QUERIED VOUCHER
        // ============================================================

        [HttpGet]
        public IActionResult AuditUpdateQueriedVoucher(int id)
        {
            ViewBag.Surname =
                HttpContext.Session.GetString("Surname");

            ViewBag.FirstName =
                HttpContext.Session.GetString("FirstName");


            var query =
                _context.AuditQueryInformations
                    .FirstOrDefault(
                        x => x.Id == id);

            if (query == null)
            {
                return NotFound();
            }


            var voucher =
                _context.Vouchers
                    .FirstOrDefault(
                        v => v.VoucherNumber ==
                             query.VoucherNumber);

            if (voucher == null)
            {
                TempData["Message"] =
                    "Voucher record not found in Vouchers table.";

                return RedirectToAction(
                    nameof(AuditQueriedVouchers));
            }


            decimal voucherGrossAmount =
                voucher.Amount + voucher.VAT;


            decimal contractAmt =
                voucher.Amount > 0
                    ? voucher.Amount
                    : 1;


            decimal vatPercent =
                (voucher.VAT / contractAmt) * 100;

            decimal fgnWhtPercent =
                (voucher.FGN_WHT / contractAmt) * 100;

            decimal edgWhtPercent =
                (voucher.EDG_WHT / contractAmt) * 100;

            decimal stampPercent =
                (voucher.StampDuty / contractAmt) * 100;


            ViewBag.VatOption =
                $"{Math.Round(vatPercent, 2)}%";

            ViewBag.FgnWhtOption =
                $"{Math.Round(fgnWhtPercent, 2)}%";

            ViewBag.EdgWhtOption =
                $"{Math.Round(edgWhtPercent, 2)}%";

            ViewBag.StampOption =
                $"{Math.Round(stampPercent, 2)}%";


            var auditModel =
                new Audit
                {
                    Id =
                        voucher.Id,

                    VoucherNumber =
                        voucher.VoucherNumber,

                    CheckedLocation =
                        voucher.CheckedLocation,

                    Station =
                        voucher.Station,

                    Head =
                        voucher.Head,

                    SubHead =
                        voucher.SubHead,

                    ClassificationCode =
                        voucher.ClassificationCode,

                    SourceClassificationCode =
                        voucher.SourceClassificationCode,

                    Date =
                        voucher.Date,

                    Amount =
                        voucherGrossAmount,

                    Payee =
                        voucher.Payee,

                    Address =
                        voucher.Address,

                    Description =
                        voucher.Description,

                    FGN_WHT =
                        voucher.FGN_WHT,

                    EDG_WHT =
                        voucher.EDG_WHT,

                    VAT =
                        voucher.VAT,

                    StampDuty =
                        voucher.StampDuty,

                    TotalAmount =
                        voucher.TotalAmount,

                    QueryText =
                        query.QueryText,

                    Voucher_FirstName =
                        voucher.Voucher_FirstName,

                    Voucher_Surname =
                        voucher.Voucher_Surname,

                    Voucher_Designation =
                        voucher.Voucher_Designation
                };


            return View(
                auditModel);
        }


        // ============================================================
        // POST: UPDATE QUERIED VOUCHER
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AuditUpdateQueriedVoucher(
            Audit model,
            bool? isVatInclusive,
            string fgn_whtOption,
            string edg_whtOption,
            string vatOption,
            string stampOption)
        {
            if (!isVatInclusive.HasValue)
            {
                TempData["Message"] =
                    "Please select if FGN VAT is Inclusive or Exclusive.";

                return RedirectToAction(
                    nameof(AuditUpdateQueriedVoucher),
                    new { id = model.Id });
            }


            if (string.IsNullOrWhiteSpace(
                model.VoucherNumber))
            {
                TempData["Message"] =
                    "Invalid voucher number.";

                return RedirectToAction(
                    nameof(AuditQueriedVouchers));
            }


            if (model.Date == DateTime.MinValue ||
                model.Date.Year < 1753)
            {
                model.Date =
                    DateTime.Today;
            }


            var (
                contractAmount,
                fgn_wht,
                edg_wht,
                stamp,
                vat,
                total
            ) = CalculateFinancials(
                model.Amount,
                isVatInclusive.Value,
                fgn_whtOption,
                edg_whtOption,
                vatOption,
                stampOption);


            // ========================================================
            // UPDATE VOUCHERS
            // ========================================================

            var voucher =
                _context.Vouchers
                    .FirstOrDefault(
                        v => v.VoucherNumber ==
                             model.VoucherNumber);

            if (voucher != null)
            {
                voucher.CheckedLocation =
                    model.CheckedLocation;

                voucher.Station =
                    model.Station;

                voucher.Head =
                    model.Head;

                voucher.SubHead =
                    model.SubHead;

                voucher.ClassificationCode =
                    model.ClassificationCode;

                voucher.SourceClassificationCode =
                    model.SourceClassificationCode;

                voucher.Date =
                    model.Date;

                voucher.Amount =
                    contractAmount;

                voucher.Payee =
                    model.Payee;

                voucher.Address =
                    model.Address;

                voucher.Description =
                    model.Description;

                voucher.FGN_WHT =
                    fgn_wht;

                voucher.EDG_WHT =
                    edg_wht;

                voucher.VAT =
                    vat;

                voucher.StampDuty =
                    stamp;

                voucher.TotalAmount =
                    total;

                voucher.Voucher_Designation =
                    model.Voucher_Designation;

                voucher.Voucher_FirstName =
                    model.Voucher_FirstName;

                voucher.Voucher_Surname =
                    model.Voucher_Surname;

                _context.Vouchers.Update(
                    voucher);
            }


            // ========================================================
            // UPDATE / CREATE AUDIT RECORD
            // ========================================================

            var auditRecord =
                _context.Audits
                    .FirstOrDefault(
                        a => a.VoucherNumber ==
                             model.VoucherNumber);

            if (auditRecord != null)
            {
                auditRecord.CheckedLocation =
                    model.CheckedLocation;

                auditRecord.Station =
                    model.Station;

                auditRecord.Head =
                    model.Head;

                auditRecord.SubHead =
                    model.SubHead;

                auditRecord.ClassificationCode =
                    model.ClassificationCode;

                auditRecord.SourceClassificationCode =
                    model.SourceClassificationCode;

                auditRecord.Date =
                    model.Date;

                auditRecord.Amount =
                    contractAmount;

                auditRecord.Payee =
                    model.Payee;

                auditRecord.Address =
                    model.Address;

                auditRecord.Description =
                    model.Description;

                auditRecord.FGN_WHT =
                    fgn_wht;

                auditRecord.EDG_WHT =
                    edg_wht;

                auditRecord.VAT =
                    vat;

                auditRecord.StampDuty =
                    stamp;

                auditRecord.TotalAmount =
                    total;

                auditRecord.Voucher_Designation =
                    model.Voucher_Designation;

                auditRecord.Voucher_FirstName =
                    model.Voucher_FirstName;

                auditRecord.Voucher_Surname =
                    model.Voucher_Surname;

                _context.Audits.Update(
                    auditRecord);
            }
            else
            {
                var newAudit =
                    new Audit
                    {
                        VoucherNumber =
                            model.VoucherNumber,

                        CheckedLocation =
                            model.CheckedLocation,

                        Station =
                            model.Station,

                        Head =
                            model.Head,

                        SubHead =
                            model.SubHead,

                        ClassificationCode =
                            model.ClassificationCode,

                        SourceClassificationCode =
                            model.SourceClassificationCode,

                        Date =
                            model.Date,

                        Amount =
                            contractAmount,

                        Payee =
                            model.Payee,

                        Address =
                            model.Address,

                        Description =
                            model.Description,

                        FGN_WHT =
                            fgn_wht,

                        EDG_WHT =
                            edg_wht,

                        VAT =
                            vat,

                        StampDuty =
                            stamp,

                        TotalAmount =
                            total,

                        Voucher_Designation =
                            model.Voucher_Designation,

                        Voucher_FirstName =
                            model.Voucher_FirstName,

                        Voucher_Surname =
                            model.Voucher_Surname
                    };

                _context.Audits.Add(
                    newAudit);
            }


            // ========================================================
            // UPDATE / CREATE BILLING CERTIFICATION
            // ========================================================

            var billingRecord =
                _context.BillingCertifications
                    .FirstOrDefault(
                        b => b.VoucherNumber ==
                             model.VoucherNumber);

            if (billingRecord != null)
            {
                billingRecord.CheckedLocation =
                    model.CheckedLocation;

                billingRecord.Station =
                    model.Station;

                billingRecord.Head =
                    model.Head;

                billingRecord.SubHead =
                    model.SubHead;

                billingRecord.ClassificationCode =
                    model.ClassificationCode;

                billingRecord.SourceClassificationCode =
                    model.SourceClassificationCode;

                billingRecord.Date =
                    model.Date;

                billingRecord.Amount =
                    contractAmount;

                billingRecord.Payee =
                    model.Payee;

                billingRecord.Address =
                    model.Address;

                billingRecord.Description =
                    model.Description;

                billingRecord.FGN_WHT =
                    fgn_wht;

                billingRecord.EDG_WHT =
                    edg_wht;

                billingRecord.VAT =
                    vat;

                billingRecord.StampDuty =
                    stamp;

                billingRecord.TotalAmount =
                    total;

                billingRecord.Voucher_Designation =
                    model.Voucher_Designation;

                billingRecord.Voucher_FirstName =
                    model.Voucher_FirstName;

                billingRecord.Voucher_Surname =
                    model.Voucher_Surname;

                _context.BillingCertifications.Update(
                    billingRecord);
            }
            else
            {
                var newBillingRecord =
                    new BillingCertification
                    {
                        VoucherNumber =
                            model.VoucherNumber,

                        CheckedLocation =
                            model.CheckedLocation,

                        Station =
                            model.Station,

                        Head =
                            model.Head,

                        SubHead =
                            model.SubHead,

                        ClassificationCode =
                            model.ClassificationCode,

                        SourceClassificationCode =
                            model.SourceClassificationCode,

                        Date =
                            model.Date,

                        Amount =
                            contractAmount,

                        Payee =
                            model.Payee,

                        Address =
                            model.Address,

                        Description =
                            model.Description,

                        FGN_WHT =
                            fgn_wht,

                        EDG_WHT =
                            edg_wht,

                        VAT =
                            vat,

                        StampDuty =
                            stamp,

                        TotalAmount =
                            total,

                        Voucher_Designation =
                            model.Voucher_Designation,

                        Voucher_FirstName =
                            model.Voucher_FirstName,

                        Voucher_Surname =
                            model.Voucher_Surname
                    };

                _context.BillingCertifications.Add(
                    newBillingRecord);
            }


            // ========================================================
            // REMOVE FROM AUDIT QUERY INFORMATION
            // ========================================================

            var query =
                _context.AuditQueryInformations
                    .FirstOrDefault(
                        q => q.VoucherNumber ==
                             model.VoucherNumber);

            if (query != null)
            {
                _context.AuditQueryInformations.Remove(
                    query);
            }


            _context.SaveChanges();


            TempData["Message"] =
                "Voucher successfully updated across Vouchers, Audits, and Billing Certifications, and removed from queries.";

            return RedirectToAction(
                nameof(AuditQueriedVouchers));
        }


        // ============================================================
        // DELETE AUDIT QUERY
        // ============================================================

        [HttpGet]
        public IActionResult AuditDeleteQuery(int id)
        {
            var query =
                _context.AuditQueryInformations
                    .FirstOrDefault(
                        x => x.Id == id);

            if (query == null)
            {
                return RedirectToAction(
                    nameof(AuditQueriedVouchers));
            }


            _context.AuditQueryInformations.Remove(
                query);

            _context.SaveChanges();


            TempData["Message"] =
                "Queried information deleted successfully.";

            return RedirectToAction(
                nameof(AuditQueriedVouchers));
        }
    }
}