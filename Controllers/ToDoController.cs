using RazorPagesAssignment.Data;
using RazorPagesAssignment.Models;
using ExcelDataReader;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using X.PagedList;

namespace RazorPagesAssignment.Controllers
{
    [Authorize]
    public class ToDoController : Controller
    {
        // Dictionary to map column names to sorting functions
        Dictionary<string, Func<IQueryable<ToDo>, IOrderedQueryable<ToDo>>> sortFunctions = new Dictionary<string, Func<IQueryable<ToDo>, IOrderedQueryable<ToDo>>>
            {
                { "Title", todos => todos.OrderBy(t => t.Title) },
                { "title_desc", todos => todos.OrderByDescending(t => t.Title) },
                { "IsCompleted", todos => todos.OrderBy(t => t.IsCompleted) },
                { "completed_desc", todos => todos.OrderByDescending(t => t.IsCompleted) },
                { "CreatedDate", todos => todos.OrderBy(t => t.CreatedDate) },
                { "created_desc", todos => todos.OrderByDescending(t => t.CreatedDate) },
                { "UpdatedDate", todos => todos.OrderBy(t => t.UpdatedDate) },
                { "updated_desc", todos => todos.OrderByDescending(t => t.UpdatedDate) }
            };
        private readonly ApplicationDbContext _context;

        public ToDoController(ApplicationDbContext context)
        {
            _context = context;
        }

        private Func<IQueryable<ToDo>, IOrderedQueryable<ToDo>> GetSelectedSortOrder(string sortOrder)
        {
            // Set default sort order if none is provided
            string defaultSortOrder = "Title";
            ViewData["CurrentSort"] = sortOrder;

            // Use the selected sorting function or default sorting function
            if (!string.IsNullOrEmpty(sortOrder))
                return sortFunctions.ContainsKey(sortOrder) ? sortFunctions[sortOrder] : sortFunctions[defaultSortOrder];
            else
                return sortFunctions[defaultSortOrder];
        }

        public IActionResult Index(string sortOrder, string searchString, int? page)
        {
            try
            {
                IQueryable<ToDo> todos = _context.ToDo;

                if (!string.IsNullOrEmpty(searchString))
                {
                    todos = todos.Where(t => t.Title.Contains(searchString));
                }

                Func<IQueryable<ToDo>, IOrderedQueryable<ToDo>> selectedSortFunction = GetSelectedSortOrder(sortOrder);

                todos = selectedSortFunction(todos);

                var pageNumber = page ?? 1;
                var pageSize = 5;

                IPagedList<ToDo> pagedToDos = todos.ToPagedList(pageNumber, pageSize);

                return View(pagedToDos);
            }
            catch
            {
                return RedirectToAction("Error", "Home");
            }
        }

        // GET: ToDo/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null || _context.ToDo == null)
            {
                return NotFound();
            }

            var toDo = await _context.ToDo
                .FirstOrDefaultAsync(m => m.Id == id);
            if (toDo == null)
            {
                return NotFound();
            }

            return View(toDo);
        }

        // GET: ToDo/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ToDo/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,IsCompleted,CreatedDate,UpdatedDate")] ToDo toDo)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    toDo.Id = Guid.NewGuid();
                    _context.Add(toDo);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                return View(toDo);
            }
            catch
            {
                return RedirectToAction("Error", "Home", new { errorMessage = "Resource is not created due to an error. Please try again! "});
            }
        }

        // POST: ToDo/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Title,IsCompleted,CreatedDate,UpdatedDate")] ToDo toDo)
        {
            if (id != toDo.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(toDo);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ToDoExists(toDo.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        return RedirectToAction("Error", "Home", new { errorMessage = "Resource could not be updated due to an error. Please try again! " });
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(toDo);
        }

        // POST: ToDo/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            try
            {
                if (_context.ToDo == null)
                {
                    return Problem("Entity set 'ApplicationDbContext.ToDo'  is null.");
                }
                var toDo = await _context.ToDo.FindAsync(id);
                if (toDo != null)
                {
                    _context.ToDo.Remove(toDo);
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch {
                return RedirectToAction("Error", "Home", new { errorMessage = "Resource could not be deleted due to an error. Please try again! " });
            }
        }

        private bool ToDoExists(Guid id)
        {
          return (_context.ToDo?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return RedirectToAction("Error", "Home", new { errorMessage = "No file is selected. Please select a file first. " });
            }
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;
                    bool isFirstRow = false;

                    using (var reader = GetDataReader(memoryStream, file.FileName))
                    {
                        while (reader.Read()) // Read the content of the file
                        {
                            if (!isFirstRow)
                            {
                                isFirstRow = true;
                                continue; // Skip the first row
                            }

                            ToDo toDoItem = null;
                            if (Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
                            {
                                var values = reader.GetString(0)?.Split(',');

                                if (values.Length > 0)
                                {
                                    toDoItem = new ToDo
                                    {
                                        Title = values[0],
                                        IsCompleted = bool.Parse(values[1]),
                                        CreatedDate = DateTime.Parse(values[2]),
                                        UpdatedDate = DateTime.Parse(values[3])
                                    };
                                }
                            }
                            else if (Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                            {
                                toDoItem = new ToDo
                                {
                                    Title = reader.GetString(0),
                                    IsCompleted = reader.GetBoolean(1),
                                    CreatedDate = reader.GetDateTime(2),
                                    UpdatedDate = reader.GetDateTime(3)
                                };
                            }
                            _context.ToDo.Add(toDoItem);
                        }
                    }
                }

                _context.SaveChanges(); // Save changes to the database

                return RedirectToAction("Index");
            }
            catch
            {
                return RedirectToAction("Error", "Home", new { errorMessage = "File could not be uploaded due to an error. Please try again! " });
            }
        }

        private IExcelDataReader GetDataReader(Stream stream, string fileName)
        {
            if (Path.GetExtension(fileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return ExcelReaderFactory.CreateCsvReader(stream);
            }
            else if (Path.GetExtension(fileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return ExcelReaderFactory.CreateReader(stream);
            }
            else
            {
                throw new NotSupportedException("File format not supported.");
            }
        }

    }
}
