
using FullVoucherSystem.Data;
using FullVoucherSystem.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;



namespace FullVoucherSystem.Controllers
{
    public class BudgetController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<BudgetController> _logger;

        public BudgetController(
            AppDbContext context,
            ILogger<BudgetController> logger)
        {
            _context = context;
            _logger = logger;
        }




        private sealed class BudgetClassification
        {
            public string Code { get; }
            public string Description { get; }

            public BudgetClassification(string code, string description)
            {
                Code = code;
                Description = description;
            }
        }

        // Approved IGR Funded Capital classifications.
        private static readonly List<BudgetClassification> Classifications =
            new List<BudgetClassification>
            {
                new("230101", "PURCHASE OF FIXED ASSETS - GENERAL"),
                new("23010105", "PURCHASE OF MOTOR VEHICLES"),
                new("23010106", "PURCHASE OF VANS"),
                new("23010107", "PURCHASE OF TRUCKS"),
                new("23010112.1", "OFFICE EQUIPMENTS (A/C, FRIDGES, FANS ETC)"),
                new("23010112.2", "OFFICE FURNITURES AND FITTINGS (WARDS)"),
                new("23010112.4", "OFFICE FURNITURES AND FITTINGS (GUEST HOUSE)"),
                new("23010112.5", "OFFICE FURNITURES AND FITTINGS (OFFICES)"),
                new("23010112.6", "OFFICE FURNITURES AND FITTINGS OTHERS"),
                new("23010113", "PURCHASE OF COMPUTERS"),
                new("23010114", "PURCHASE OF COMPUTER PRINTERS"),
                new("23010115", "PURCHASE OF PHOTOCOPYING MACHINES"),
                new("23010118", "PURCHASE OF SCANNERS"),
                new("23010119", "PURCHASE OF POWER GENERATING SET"),
                new("23010120", "PURCHASE OF KITCHEN EQUIPMENT"),
                new("23010122.2", "ENGINEERING EQUIPMENTS"),
                new("23010122.3", "PURCHASE OF LABORATORY EQUIPMENTS"),
                new("23010122.4", "HOLTICULTURE UNIT"),
                new("23010122.5", "XRAY EQUIPMENTS"),
                new("23010122.15", "PURCHASE OF DENTAL EQUIPMENTS"),
                new("23010123", "MEDICAL AND SURGICAL EQUIPMENT"),
                new("23010122.7", "PURCHASE OF MEDICAL & SURGICAL EQUIPMENTS"),
                new("23010122.1", "PURCHASE OF ENGINEERING EQUIPMENT"),
                new("23010122.9", "BOREHOLE AND SUBMERSIBLE PUMP"),
                new("23010122.16", "PURCHASE OF OPTHAMOLOGY EQUIPMENTS"),
                new("23010122.17", "PURCHASE OF OTHER MEDICAL EQUIPMENT-"),
                new("23010122.18", "OTHER MEDICAL EQUIPMENTS"),
                new("23010124", "PURCHASE OF LIBRARY BOOKS AND EQUIPMENT"),
                new("23010126", "PURCHASE OF SPORTING/GAMING EQUIPMENT"),
                new("23020103", "CONSTRUCTION/ PROVISION OF ELECTRICITY"),
                new("23030102", "REHABILITATION/REPAIRS-ELECTRICITY"),
                new("23010128", "PURCHASE OF INDUSTRIAL EQUIPMENT"),
                new("230201", "CONSTRUCTION/PROVISION OF FIXED ASSETS - GENERAL"),
                new("23020102", "CONSTRUCTION/PROVISION OF RESIDENTIAL BUILDINGS."),
                new("23020101", "CONSTRUCTION/PROVISION OF OFFICE BUILDINGS"),
                new("23020105", "CONSTRUCTION/PROVISION OF WATER"),
                new("23020106", "CONSTRUCTION/PROVISION OF HOSPITALS / HEALTH CENTRES"),
                new("23030105", "REHABILITATION/REPAIRS-HOSPITAL/HEALTH CENTRES"),
                new("23020114", "CONSTRUCTION/PROVISION OF ROAD"),
                new("23020118", "CONSTRUCTION/PROVISION OF INFRASTRUCTURE- HOUSING"),
                new("23020122", "CONSTRUCTION OF SHOPPING COMPLEX"),
                new("23030101", "REHABILITATION/REPAIRS OF RESIDENTIAL BUILDING"),
                new("23050102", "COMPUTER SOFTWARE ACQUISITION"),
                new("23050101", "RESEARCH AND DEVELOPMENT"),
                new("23050104", "ANNIVERSARIES/CELEBRATIONS")
            };



        // Other Classification Codes
        private static readonly List<BudgetClassification>
            OtherClassifications = new List<BudgetClassification>
        {
            new("21010102", "OVERTIME PAYMENTS"),
            new("21020101", "NON REGULAR ALLOWANCES (UNIFORM ALLOWANCES, NYSC ALLOWANCES)"),
            new("22010103", "DEATH BENEFITS"),
            new("22020101", "LOCAL TRAVEL & TRANSPORT: TRAINING"),
            new("22020102", "LOCAL TRAVEL & TRANSPORT: OTHERS"),
            new("22020103", "INTERNATIONAL TRAVEL & TRANSPORT: TRAINING"),
            new("22020201", "ELECTRICITY CHARGES"),
            new("22020202", "TELEPHONE CHARGES"),
            new("22020203", "INTERNET ACCESS CHARGES"),
            new("22020301", "OFFICE STATIONERIES / COMPUTER CONSUMABLES"),
            new("22020303", "NEWSPAPERS"),
            new("22020304", "MAGAZINES & PERIODICALS"),
            new("22020305", "PRINTING OF NON SECURITY DOCUMENTS"),
            new("22020307.1", "DRUGS"),
            new("22020307.2", "BLOOD TRANSFUSION/ HEAMATOLOGY"),
            new("22020307.3", "CHEMICAL PATHOLOGY"),
            new("22020307.4", "ELECTROCARDIOGRAM (E.C.G.)"),
            new("22020307.6", "MICROBIOLOGY"),
            new("22020307.7", "MORBID ANATOMY & MORTUARY"),
            new("22020307.9", "O&G CONSUMABLES"),
            new("22020307.11", "OPTHAMOLOGY MATERIALS"),
            new("22020307.12", "X RAY CONSUMABLES & MATERIALS"),
            new("22020307.13", "SANITARY/CLEANING MATERIALS"),
            new("22020307.14", "BEDDING/LINEN/LAUNDRY CONSUMABLES"),
            new("22020307.15", "LAUNDRY CONSUMABLES"),
            new("22020307.16", "OCCUPATIONAL THERAPY MATERIALS"),
            new("22020307.19", "THEATRE/SURGICAL EQUIPTMENT & MATERIALS"),
            new("22020307.21", "DENTAL STORES & CONSUMABLES"),
            new("22020307.22", "RENAL STORES & CONSUMABLES"),
            new("22020307.23", "DRESSING/MEDICAL STORES"),
            new("22020307.24", "ANAESTHESOLOGY MATERIALS"),
            new("22020307.25", "PHYSIOTHERAPY MATERIALS"),
            new("22020307.28", "MEDICAL GASES"),
            new("22020307.29", "OTHER MATERIALS & SUPPLIES"),
            new("22020307.31", "CHC OGBONNA"),
            new("22020307.32", "CHC UDO"),
            new("22020309", "UNIFORMS & OTHER CLOTHING"),
            new("22020311", "FOOD STUFF / CATERING MATERIALS SUPPLIES"),
            new("22020401", "MAINTENANCE OF MOTOR VEHICLE / TRANSPORT EQUIPMENT"),
            new("22020402", "MAINTENANCE OF OFFICE FURNITURE"),
            new("22020403", "MAINTENANCE OF OFFICE BUILDING / RESIDENTIAL QTRS"),
            new("22020404", "MAINTENANCE OF OFFICE / IT EQUIPMENTS"),
            new("22020405", "MAINTENANCE OF PLANTS/GENERATORS"),
            new("22020406", "OTHER MAINTENANCE SERVICES"),
            new("22020406.2", "MAINTENANCE OF FURNITURE & FITTINGS (WARDS & CLINIC)"),
            new("22020406.3", "MAINTENANCE OF FURNITURE & FITTINGS (HOSTEL)"),
            new("22020406.4", "ELECTRICAL INSTALLATION & MAINTENANCE"),
            new("22020406.5", "ENGINEERING STORE, SUPPLIES & SPARE PARTS"),
            new("22020406.6", "LAUNDRY EQUIPTMENT MAINTENANCE"),
            new("22020406.7", "MAINTENANCE OF BORE HOLES"),
            new("22020406.8", "MAINTENANCE OF PRINTING MACHINE"),
            new("22020406.9", "RADIOLOGY MONITORING"),
            new("22020406.1", "MAINTENANCE OF HOSPITAL EQUIPMENT"),
            new("22020406.11", "MAINTENANCE OF SEWAGE LINES"),
            new("22020406.12", "OTHERS"),
            new("22020501.1", "LOCAL TRAINING"),
            new("22020501.2", "ACCREDITATION EXPENSES (MEDICAL & DENTAL)"),
            new("22020501.3", "POST GRADUATE EDUCATION (RESIDENCY)/RESEARCH GRANT & EXPENSES"),
            new("22020501.4", "COLLEGE OF NURSING SCIENCES"),
            new("22020501.11", "CHILDREN PLAY CENTRE"),
            new("22020501.12", "INSTITUTE OF HEALTH SCIENCES & TECHNOLOGY"),
            new("22020601", "SECURITY SERVICES"),
            new("22020604", "SECURITY VOTE (INCLUDING OPERATIONS)"),
            new("22020605.1", "CLEANING & FUMIGATION SERVICES"),
            new("22020605.2", "OUTSOURCED SERVICES"),
            new("22020701", "FINANCIAL CONSULTING"),
            new("22020702", "INFORMATION TECHNOLOGY CONSULTING"),
            new("22020703", "LEGAL SERVICES"),
            new("22020704", "ENGINEERING SERVICES"),
            new("22020708", "MEDICAL CONSULTING"),
            new("22020801", "MOTOR VEHICLE FUEL COST"),
            new("22020803", "PLANT / GENERATOR FUEL COST"),
            new("22020803.2", "SUPPLY OF ENGINE OIL/LUBRICANTS"),
            new("22020806", "COOKING GAS/FUEL COST"),
            new("22020902", "INSURANCE PREMIUM"),
            new("22021001", "REFRESHMENT & MEALS"),
            new("22021002.1", "HONORARIUM & SITTING ALLOWANCE"),
            new("22021002.2", "BOARD OF MANAGEMENT EXPENSES - ALLOWANCE & BOARDINGG & LODGING"),
            new("22021002.3", "SUPERVISION AND INSPECTION FEES"),
            new("22021003", "PUBLICITY & ADVERTISEMENTS"),
            new("22021004", "MEDICAL EXPENSES"),
            new("22021006", "POSTAGES & COURIER SERVICES"),
            new("22021007.1", "WELFARE PACKAGES"),
            new("22021007.3", "HOSPITALITY & ENTERTAINMENT"),
            new("22021007.4", "DONATION & CONTRIBUTIONS"),
            new("22021008", "SUBSCRIPTION TO PROFESSIONAL BODIES"),
            new("22021009", "SPORTING ACTIVITIES"),
            new("22021011", "RECRUITMENT AND APPOINTMENT (SERVICE WIDE)"),
            new("22021013", "PROMOTION (SERVICE WIDE)"),
            new("22021014", "ANNUAL BUDGET EXPENSES & ADMINISTRATION"),
            new("22021041", "CONTIGENCY"),
            new("31040102", "INTER ACCOUNT TRANSFER"),
            new("31040104", "REVENUE REMITTANCE"),
            new("31060101", "IMPREST"),
            new("31060301", "SPECIAL IMPREST (SERVICE WIDE)"),
            new("0", "UNPAID VOUCHERS")
        };

        private bool HasValidSession()
        {
            var role = HttpContext.Session.GetString("Role");
            var surname = HttpContext.Session.GetString("Surname");
            var firstName = HttpContext.Session.GetString("FirstName");

            if (!string.IsNullOrWhiteSpace(role) &&
                !string.IsNullOrWhiteSpace(surname) &&
                !string.IsNullOrWhiteSpace(firstName))
            {
                return true;
            }

            HttpContext.Session.Clear();
            return false;
        }

        [HttpGet]
        public async System.Threading.Tasks.Task<IActionResult> ManageIgrCapital()
        {
            if (!HasValidSession())
            {
                return RedirectToAction("Login", "Account");
            }

            var savedBudgets = await _context.IgrFundedCapitalBudgets
                .AsNoTracking()
                .ToListAsync();

            var model = new IgrFundedCapitalBudgetViewModel();

            foreach (var item in Classifications)
            {
                var saved = savedBudgets.FirstOrDefault(
                    x => x.ClassificationCode == item.Code);

                model.Items.Add(new IgrFundedCapitalBudgetRow
                {
                    ClassificationCode = item.Code,
                    Description = item.Description,
                    BudgetAmount = saved?.BudgetAmount
                });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async System.Threading.Tasks.Task<IActionResult> ManageIgrCapital(
            IgrFundedCapitalBudgetViewModel model)
        {
            if (!HasValidSession())
            {
                return RedirectToAction("Login", "Account");
            }

            if (model.Items == null)
            {
                ModelState.AddModelError("", "No budget items were submitted.");
                model.Items = new List<IgrFundedCapitalBudgetRow>();
                return View(model);
            }

            // Do not trust classification codes or descriptions posted by the browser.
            var submittedCodes = model.Items
                .Select(x => x.ClassificationCode)
                .ToList();

            if (submittedCodes.Count != Classifications.Count ||
                submittedCodes.Distinct(StringComparer.Ordinal).Count()
                    != Classifications.Count ||
                Classifications.Any(c => !submittedCodes.Contains(c.Code)))
            {
                ModelState.AddModelError(
                    "",
                    "The submitted classification list is invalid. Reload the page and try again.");
            }

            foreach (var row in model.Items)
            {
                if (row.BudgetAmount.HasValue && row.BudgetAmount.Value < 0)
                {
                    ModelState.AddModelError(
                        "",
                        $"The budget amount for {row.ClassificationCode} cannot be negative.");
                }
            }

            if (!ModelState.IsValid)
            {
                // Rebuild trusted descriptions if validation fails.
                foreach (var row in model.Items)
                {
                    var approved = Classifications.FirstOrDefault(
                        c => c.Code == row.ClassificationCode);

                    if (approved != null)
                    {
                        row.Description = approved.Description;
                    }
                }

                return View(model);
            }

            var existingBudgets = await _context.IgrFundedCapitalBudgets
                .ToListAsync();

            foreach (var row in model.Items)
            {
                var classification = Classifications.First(
                    c => c.Code == row.ClassificationCode);

                // Blank amounts are not saved; existing amounts are left unchanged.
                if (!row.BudgetAmount.HasValue)
                {
                    continue;
                }

                var existing = existingBudgets.FirstOrDefault(
                    x => x.ClassificationCode == classification.Code);

                if (existing == null)
                {
                    _context.IgrFundedCapitalBudgets.Add(
                        new IgrFundedCapitalBudget
                        {
                            ClassificationCode = classification.Code,
                            Description = classification.Description,
                            BudgetAmount = row.BudgetAmount.Value,
                            DateUpdated = DateTime.Now
                        });
                }
                else
                {
                    existing.Description = classification.Description;
                    existing.BudgetAmount = row.BudgetAmount.Value;
                    existing.DateUpdated = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();

            TempData["Message"] =
                "IGR Funded Capital budget amounts have been saved successfully.";

            return RedirectToAction(nameof(ManageIgrCapital));
        }


        [HttpGet]
        public async System.Threading.Tasks.Task<IActionResult>
            ManageOtherClassifications()
        {
            if (!HasValidSession())
            {
                return RedirectToAction("Login", "Account");
            }

            var savedBudgets = await _context.OtherClassificationBudgets
                .AsNoTracking()
                .ToListAsync();

            var model = new OtherClassificationBudgetViewModel();

            foreach (var item in OtherClassifications)
            {
                var saved = savedBudgets.FirstOrDefault(
                    x => x.ClassificationCode == item.Code);

                model.Items.Add(new OtherClassificationBudgetRow
                {
                    ClassificationCode = item.Code,
                    Description = item.Description,
                    BudgetAmount = saved?.BudgetAmount
                });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async System.Threading.Tasks.Task<IActionResult>
            ManageOtherClassifications(
                OtherClassificationBudgetViewModel model)
        {
            if (!HasValidSession())
            {
                return RedirectToAction("Login", "Account");
            }

            if (model.Items == null)
            {
                ModelState.AddModelError(
                    "",
                    "No classification items were submitted.");

                model.Items = new List<OtherClassificationBudgetRow>();
                return View(model);
            }

            // Verify that the posted list contains exactly the approved codes.
            var submittedCodes = model.Items
                .Select(x => x.ClassificationCode)
                .ToList();

            if (submittedCodes.Count != OtherClassifications.Count ||
                submittedCodes.Distinct(StringComparer.Ordinal).Count()
                    != OtherClassifications.Count ||
                OtherClassifications.Any(
                    c => !submittedCodes.Contains(c.Code)))
            {
                ModelState.AddModelError(
                    "",
                    "The submitted classification list is invalid. Reload the page and try again.");
            }

            foreach (var row in model.Items)
            {
                if (row.BudgetAmount.HasValue &&
                    row.BudgetAmount.Value < 0)
                {
                    ModelState.AddModelError(
                        "",
                        $"The budget amount for {row.ClassificationCode} cannot be negative.");
                }
            }

            if (!ModelState.IsValid)
            {
                // Restore approved descriptions rather than trusting posted values.
                foreach (var row in model.Items)
                {
                    var approved = OtherClassifications.FirstOrDefault(
                        c => c.Code == row.ClassificationCode);

                    if (approved != null)
                    {
                        row.Description = approved.Description;
                    }
                }

                return View(model);
            }

            var existingBudgets = await _context.OtherClassificationBudgets
                .ToListAsync();

            foreach (var row in model.Items)
            {
                var classification = OtherClassifications.First(
                    c => c.Code == row.ClassificationCode);

                // Blank fields do not overwrite existing saved budgets.
                if (!row.BudgetAmount.HasValue)
                {
                    continue;
                }

                var existing = existingBudgets.FirstOrDefault(
                    x => x.ClassificationCode == classification.Code);

                if (existing == null)
                {
                    var newBudget = new OtherClassificationBudget
                    {
                        ClassificationCode = classification.Code,
                        Description = classification.Description,
                        BudgetAmount = row.BudgetAmount.Value,
                        DateUpdated = DateTime.Now
                    };

                    _context.OtherClassificationBudgets.Add(newBudget);

                    // Keep the in-memory list updated during this request.
                    existingBudgets.Add(newBudget);
                }
                else
                {
                    existing.Description = classification.Description;
                    existing.BudgetAmount = row.BudgetAmount.Value;
                    existing.DateUpdated = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();

            TempData["Message"] =
                "Other Classification Codes budget amounts have been saved successfully.";

            return RedirectToAction(nameof(ManageOtherClassifications));
        }


        [HttpGet]
        public async System.Threading.Tasks.Task<IActionResult> ManageStaffAdvances()
        {
            if (!HasValidSession())
                return RedirectToAction("Login", "Account");

            var voucherPayees = await _context.Vouchers
                .AsNoTracking()
                .Where(v =>
                    v.VoucherNumber != null &&
                    EF.Functions.Like(v.VoucherNumber, "%/SA-2/%") &&
                    v.Payee != null &&
                    v.Payee.Trim() != "")
                .Select(v => v.Payee!)
                .ToListAsync();

            var payees = voucherPayees
                .Select(p => p.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var savedBudgets = await _context.StaffAdvanceBudgets
                .AsNoTracking()
                .ToListAsync();

            var model = new StaffAdvanceBudgetViewModel();

            foreach (var payee in payees)
            {
                var saved = savedBudgets.FirstOrDefault(b =>
                    string.Equals(
                        b.Payee?.Trim(),
                        payee,
                        StringComparison.OrdinalIgnoreCase));

                model.Items.Add(new StaffAdvanceBudgetRow
                {
                    Payee = payee,
                    VoteRefNo = saved?.VoteRefNo,
                    BudgetAmount = saved?.BudgetAmount
                });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async System.Threading.Tasks.Task<IActionResult> ManageStaffAdvances(
            StaffAdvanceBudgetViewModel model)
        {
            if (!HasValidSession())
                return RedirectToAction("Login", "Account");

            // Diagnose the submitted form if ASP.NET Core did not bind Items.
            if (model.Items == null || model.Items.Count == 0)
            {
                var form = await Request.ReadFormAsync();
                var submittedKeys = string.Join(", ", form.Keys);

                _logger.LogWarning(
                    "Staff Advances form submitted without bound Items. Form keys: {FormKeys}",
                    submittedKeys);

                TempData["Error"] =
                    "The form reached the server, but the Staff Advances rows were not received. " +
                    "Expected fields such as Items[0].Payee, Items[0].VoteRefNo and " +
                    "Items[0].BudgetAmount. Fields received: " +
                    (string.IsNullOrWhiteSpace(submittedKeys)
                        ? "None"
                        : submittedKeys);

                return RedirectToAction(nameof(ManageStaffAdvances));
            }

            // Retrieve the authorised payees from Staff Advances vouchers.
            var voucherPayees = await _context.Vouchers
                .AsNoTracking()
                .Where(v =>
                    v.VoucherNumber != null &&
                    EF.Functions.Like(v.VoucherNumber, "%/SA-2/%") &&
                    v.Payee != null &&
                    v.Payee.Trim() != "")
                .Select(v => v.Payee!)
                .ToListAsync();

            var approvedPayees = voucherPayees
                .Select(p => p.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var submittedPayees = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

            // Validate each row and identify the exact field with a problem.
            for (int i = 0; i < model.Items.Count; i++)
            {
                var row = model.Items[i];

                row.Payee = (row.Payee ?? string.Empty).Trim();
                row.VoteRefNo = string.IsNullOrWhiteSpace(row.VoteRefNo)
                    ? null
                    : row.VoteRefNo.Trim();

                if (string.IsNullOrWhiteSpace(row.Payee))
                {
                    ModelState.AddModelError(
                        $"Items[{i}].Payee",
                        $"Row {i + 1}: Payee is missing.");
                }
                else
                {
                    if (!approvedPayees.Contains(row.Payee))
                    {
                        ModelState.AddModelError(
                            $"Items[{i}].Payee",
                            $"Row {i + 1}: '{row.Payee}' is not in the approved Staff Advances voucher list.");
                    }

                    if (!submittedPayees.Add(row.Payee))
                    {
                        ModelState.AddModelError(
                            $"Items[{i}].Payee",
                            $"Row {i + 1}: '{row.Payee}' appears more than once.");
                    }
                }

                if (row.VoteRefNo?.Length > 100)
                {
                    ModelState.AddModelError(
                        $"Items[{i}].VoteRefNo",
                        $"Row {i + 1}: Vote Ref. No. cannot exceed 100 characters.");
                }

                // decimal(18,2) supports up to 16 digits before the decimal point.
                if (row.BudgetAmount.HasValue &&
                    (row.BudgetAmount.Value < 0 ||
                     row.BudgetAmount.Value > 9999999999999999.99m))
                {
                    ModelState.AddModelError(
                        $"Items[{i}].BudgetAmount",
                        $"Row {i + 1}: Budget Amount must be between 0 and 9,999,999,999,999,999.99.");
                }
            }

            if (!ModelState.IsValid)
            {
                // Restore the official payee list so that the form remains usable.
                TempData["Error"] =
                    "Some entries need attention. See the validation messages displayed on the page.";

                return View(model);
            }

            // Load existing records and insert or update by payee.
            var existingBudgets = await _context.StaffAdvanceBudgets.ToListAsync();

            int inserted = 0;
            int updated = 0;

            foreach (var row in model.Items)
            {
                var existing = existingBudgets.FirstOrDefault(b =>
                    string.Equals(
                        b.Payee?.Trim(),
                        row.Payee,
                        StringComparison.OrdinalIgnoreCase));

                if (existing == null)
                {
                    var newBudget = new StaffAdvanceBudget
                    {
                        Payee = row.Payee,
                        VoteRefNo = row.VoteRefNo,
                        BudgetAmount = row.BudgetAmount,
                        DateUpdated = DateTime.Now
                    };

                    _context.StaffAdvanceBudgets.Add(newBudget);
                    existingBudgets.Add(newBudget);
                    inserted++;
                }
                else
                {
                    // Blank optional fields preserve the existing database values.
                    if (row.VoteRefNo != null)
                        existing.VoteRefNo = row.VoteRefNo;

                    if (row.BudgetAmount.HasValue)
                        existing.BudgetAmount = row.BudgetAmount.Value;

                    existing.DateUpdated = DateTime.Now;
                    updated++;
                }
            }

            // Save without a broad catch that hides the underlying database exception.
            await _context.SaveChangesAsync();

            TempData["Message"] =
                $"Staff Advances budget saved successfully. New records: {inserted}; updated records: {updated}.";

            return RedirectToAction(nameof(ManageStaffAdvances));
        }



    }
}
