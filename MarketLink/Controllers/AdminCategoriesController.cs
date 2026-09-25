using MarketLink.Models;
using MarketLink.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminCategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminCategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // CATEGORY LIST
        // GET: /AdminCategories
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            var rows = await _context.categories
                .AsNoTracking()
                .OrderBy(c => c.CategoryName)
                .Select(c => new AdminCategoryRowViewModel
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    Description = c.Description,
                    IsActive = c.IsActive,

                    ProductCount = _context.products.Count(p =>
                        p.CategoryId == c.CategoryId &&
                        p.IsActive)
                })
                .ToListAsync(cancellationToken);

            var model = new AdminCategoriesPageViewModel
            {
                Categories = rows,
                TotalCategories = rows.Count,
                ActiveCategories = rows.Count(x => x.IsActive),
                InactiveCategories = rows.Count(x => !x.IsActive)
            };

            return View(model);
        }

        // =========================================================
        // CREATE
        // GET: /AdminCategories/Create
        // =========================================================
        [HttpGet]
        public IActionResult Create()
        {
            return View(new AdminCategoryFormViewModel
            {
                IsActive = true
            });
        }

        // =========================================================
        // CREATE
        // POST: /AdminCategories/Create
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            AdminCategoryFormViewModel model,
            CancellationToken cancellationToken)
        {
            model.CategoryName = model.CategoryName?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(model.CategoryName))
            {
                ModelState.AddModelError(
                    nameof(model.CategoryName),
                    "Category name is required.");
            }

            var duplicateExists = await _context.categories
                .AsNoTracking()
                .AnyAsync(
                    c => c.CategoryName.ToLower() ==
                         model.CategoryName.ToLower(),
                    cancellationToken);

            if (duplicateExists)
            {
                ModelState.AddModelError(
                    nameof(model.CategoryName),
                    "A category with this name already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var category = new Category
            {
                CategoryName = model.CategoryName,
                Description = model.Description?.Trim() ?? "",
                IsActive = model.IsActive
            };

            _context.categories.Add(category);

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                "Category created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // EDIT
        // GET: /AdminCategories/Edit/5
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Edit(
            int id,
            CancellationToken cancellationToken)
        {
            var category = await _context.categories
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    c => c.CategoryId == id,
                    cancellationToken);

            if (category == null)
            {
                return NotFound();
            }

            var model = new AdminCategoryFormViewModel
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                Description = category.Description,
                IsActive = category.IsActive
            };

            return View(model);
        }

        // =========================================================
        // EDIT
        // POST: /AdminCategories/Edit/5
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            AdminCategoryFormViewModel model,
            CancellationToken cancellationToken)
        {
            model.CategoryName = model.CategoryName?.Trim() ?? "";

            var category = await _context.categories
                .FirstOrDefaultAsync(
                    c => c.CategoryId == model.CategoryId,
                    cancellationToken);

            if (category == null)
            {
                return NotFound();
            }

            var duplicateExists = await _context.categories
                .AsNoTracking()
                .AnyAsync(
                    c =>
                        c.CategoryId != model.CategoryId &&
                        c.CategoryName.ToLower() ==
                        model.CategoryName.ToLower(),
                    cancellationToken);

            if (duplicateExists)
            {
                ModelState.AddModelError(
                    nameof(model.CategoryName),
                    "Another category with this name already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            category.CategoryName = model.CategoryName;
            category.Description = model.Description?.Trim() ?? "";
            category.IsActive = model.IsActive;

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                "Category updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // TOGGLE ACTIVE / INACTIVE
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(
            int id,
            CancellationToken cancellationToken)
        {
            var category = await _context.categories
                .FirstOrDefaultAsync(
                    c => c.CategoryId == id,
                    cancellationToken);

            if (category == null)
            {
                return NotFound();
            }

            category.IsActive = !category.IsActive;

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                category.IsActive
                    ? "Category activated."
                    : "Category deactivated.";

            return RedirectToAction(nameof(Index));
        }
    }
}
