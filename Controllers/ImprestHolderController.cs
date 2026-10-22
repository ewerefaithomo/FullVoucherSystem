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
    public class ImprestHolderController : Controller
    {
        private readonly AppDbContext _context;

        public ImprestHolderController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new ImprestHolder());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register([FromForm] ImprestHolder model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    bool exists = _context.Imprest_Holder.Any(x => x.FolioNumber == model.FolioNumber);
                    if (exists)
                    {
                        ViewBag.ErrorMessage = $"An Imprest Holder with Folio Number '{model.FolioNumber}' already exists.";
                        return View(model);
                    }

                    _context.Imprest_Holder.Add(model);
                    _context.SaveChanges();

                    TempData["SuccessMessage"] = "Information submitted successfully.";
                    return RedirectToAction("Register");
                }
                catch (DbUpdateException dbEx)
                {
                    var innerMessage = dbEx.InnerException != null ? dbEx.InnerException.Message : dbEx.Message;
                    ViewBag.ErrorMessage = "Database Error: " + innerMessage;
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMessage = "An error occurred while saving: " + ex.GetBaseException().Message;
                }
            }
            else
            {
                var errors = ModelState.Values
                                       .SelectMany(v => v.Errors)
                                       .Select(e => e.ErrorMessage)
                                       .Where(e => !string.IsNullOrWhiteSpace(e));

                ViewBag.ErrorMessage = "Please fix validation errors: " + string.Join(" ", errors);
            }

            return View(model);
        }

        // GET: /ImprestHolder/Search
        [HttpGet]
        public IActionResult Search(string searchName, string searchStaffId)
        {
            ViewBag.Surname = HttpContext.Session.GetString("Surname");
            ViewBag.FirstName = HttpContext.Session.GetString("FirstName");

            ViewBag.SearchName = searchName;
            ViewBag.SearchStaffId = searchStaffId;

            var query = _context.Imprest_Holder.AsQueryable();

            bool isSearched = false;

            if (!string.IsNullOrWhiteSpace(searchName))
            {
                query = query.Where(h => EF.Functions.Like(h.StaffNames, $"%{searchName.Trim()}%"));
                isSearched = true;
            }

            if (!string.IsNullOrWhiteSpace(searchStaffId))
            {
                query = query.Where(h => EF.Functions.Like(h.StaffId, $"%{searchStaffId.Trim()}%"));
                isSearched = true;
            }

            ViewBag.IsSearched = isSearched;

            List<ImprestHolder> results = isSearched ? query.ToList() : new List<ImprestHolder>();

            return View(results);
        }

        // GET: /ImprestHolder/Details/{folioNumber}
        [HttpGet]
        public IActionResult Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var imprestHolder = _context.Imprest_Holder.FirstOrDefault(m => m.FolioNumber == id);
            if (imprestHolder == null)
            {
                return NotFound();
            }

            ViewBag.Surname = HttpContext.Session.GetString("Surname");
            ViewBag.FirstName = HttpContext.Session.GetString("FirstName");

            return View(imprestHolder);
        }

        // AJAX API: Get distinct staff names for live autocomplete
        [HttpGet]
        public IActionResult GetHolderNames(string term)
        {
            var names = _context.Imprest_Holder
                .Where(h => string.IsNullOrEmpty(term) || h.StaffNames.Contains(term))
                .Select(h => h.StaffNames)
                .Distinct()
                .Take(20)
                .ToList();

            return Json(names);
        }

        // AJAX API: Get distinct staff IDs for live autocomplete
        [HttpGet]
        public IActionResult GetStaffIds(string term)
        {
            var ids = _context.Imprest_Holder
                .Where(h => string.IsNullOrEmpty(term) || h.StaffId.Contains(term))
                .Select(h => h.StaffId)
                .Distinct()
                .Take(20)
                .ToList();

            return Json(ids);
        }
    }
}