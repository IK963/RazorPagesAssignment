using DotNetCore_Task3.Data;
using DotNetCore_Task3.Models;
using ExcelDataReader;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using X.PagedList;

namespace DotNetCore_Task3.Controllers
{
    [Authorize]
    public class ToDoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ToDoController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(string sortOrder, string searchString, int? page)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["TitleSort"] = String.IsNullOrEmpty(sortOrder) ? "Title" : "title_desc";
            ViewData["CompletedSort"] = sortOrder == "IsCompleted" ? "IsCompleted" : "completed_desc";
            ViewData["CreatedSort"] = sortOrder == "CreatedDate" ? "CreatedDate" : "created_desc";
            ViewData["UpdatedSort"] = sortOrder == "UpdatedDate" ? "UpdatedDate" : "updated_desc";

            var todos = _context.ToDo.ToList();

            if (!string.IsNullOrEmpty(searchString))
            {
                todos = todos.Where(t => t.Title.Contains(searchString)).ToList();
            }

            switch (sortOrder)
            {
                case "Title":
                    todos = todos.OrderBy(t => t.Title).ToList();
                    break;
                case "title_desc":
                    todos = todos.OrderByDescending(t => t.Title).ToList();
                    break;
                case "IsCompleted":
                    todos = todos.OrderBy(t => t.IsCompleted).ToList();
                    break;
                case "completed_desc":
                    todos = todos.OrderByDescending(t => t.IsCompleted).ToList();
                    break;
                case "CreatedDate":
                    todos = todos.OrderBy(t => t.CreatedDate).ToList();
                    break;
                case "created_desc":
                    todos = todos.OrderByDescending(t => t.CreatedDate).ToList();
                    break;
                case "UpdatedDate":
                    todos = todos.OrderBy(t => t.UpdatedDate).ToList();
                    break;
                case "updated_desc":
                    todos = todos.OrderByDescending(t => t.UpdatedDate).ToList();
                    break;
                default:
                    todos = todos.OrderBy(t => t.Title).ToList();
                    break;
            }

            var pageNumber = page ?? 1;
            var pageSize = 5; 

            IPagedList<ToDo> pagedToDos = todos.ToPagedList(pageNumber, pageSize);

            return View(pagedToDos);
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
            if (ModelState.IsValid)
            {
                toDo.Id = Guid.NewGuid();
                _context.Add(toDo);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(toDo);
        }

        // GET: ToDo/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null || _context.ToDo == null)
            {
                return NotFound();
            }

            var toDo = await _context.ToDo.FindAsync(id);
            if (toDo == null)
            {
                return NotFound();
            }
            return View(toDo);
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
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(toDo);
        }

        // GET: ToDo/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
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

        // POST: ToDo/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
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

        private bool ToDoExists(Guid id)
        {
          return (_context.ToDo?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return RedirectToAction("/Home/Error"); // Replace with your error handling logic
            }

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
