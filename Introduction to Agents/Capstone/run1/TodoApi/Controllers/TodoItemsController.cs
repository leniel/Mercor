using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;

namespace TodoApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TodoItemsController : ControllerBase
    {
        private readonly TodoContext _context;

        public TodoItemsController(TodoContext context)
        {
            _context = context;
        }

        // GET: api/TodoItems
        [HttpGet]
        [Route("")]
        [Authorize("read:todos", Roles="Reader")]
        public async Task<ActionResult<IEnumerable<TodoItem>>> GetTodoItems()
        {
            // Custom claim passed through an Auth0 rule.
            // More info: https://auth0.com/docs/rules
            var user = ((ClaimsIdentity)HttpContext.User.Identity).
            FindFirst(c => c.Type == "https://reacttodo.com/email").Value;

            return await _context.TodoItems.Where(todo => todo.User == user).ToListAsync();
        }

        [HttpGet]
        [Route("Count")]
        public async Task<ActionResult<long>> GetTodoItemsCount()
        {
            return await _context.TodoItems.LongCountAsync();
        }

        // GET: api/TodoItems/5
        [HttpGet("{id}")]
        [Authorize("read:todos", Roles = "Reader")]
        public async Task<ActionResult<TodoItem>> GetTodoItem(long id)
        {
            var todoItem = await _context.TodoItems.FindAsync(id);

            if (todoItem == null)
            {
                return NotFound();
            }

            return todoItem;
        }

        // PUT: api/TodoItems/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPut("{id}")]
        [Authorize("edit:todos", Roles = "Editor")]
        public async Task<IActionResult> PutTodoItem(long id, TodoItem todoItem)
        {
            if (id != todoItem.Id)
            {
                return BadRequest();
            }

            _context.Entry(todoItem).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TodoItemExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/TodoItems
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost]
        [Authorize("add:todos", Roles = "Creator")]
        public async Task<ActionResult<TodoItem>> PostTodoItem([FromBody]TodoItem todoItem)
        {
            _context.TodoItems.Add(todoItem);
            await _context.SaveChangesAsync();

            //await Task.Delay(10000);

            return CreatedAtAction(nameof(GetTodoItem), new { id = todoItem.Id }, todoItem);
        }

        // DELETE: api/TodoItems/5
        [HttpDelete("{id}")]
        [Authorize("delete:todos", Roles = "Deleter")]
        public async Task<ActionResult<TodoItem>> DeleteTodoItem(long id)
        {
            var todoItem = await _context.TodoItems.FindAsync(id);
            if (todoItem == null)
            {
                return NotFound();
            }

            _context.TodoItems.Remove(todoItem);
            await _context.SaveChangesAsync();

            return todoItem;
        }

        [HttpPost("import")]
        [AllowAnonymous]
        public async Task<ActionResult<ImportResult>> ImportTodos(IFormFile file)
        {
            var result = new ImportResult();

            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            var importedItems = new List<TodoItem>();
            var existingTodos = await _context.TodoItems.Select(t => new { t.User, t.Name }).ToListAsync();
            var processedNames = new HashSet<(string User, string Name)>(existingTodos.Select(t => (t.User, t.Name)));

            using (var stream = file.OpenReadStream())
            using (var reader = new StreamReader(stream))
            using (var csv = new CsvHelper.CsvReader(
                reader,
                System.Globalization.CultureInfo.InvariantCulture))
            {
                csv.Read();
                csv.ReadHeader();

                while (true)
                {
                    try
                    {
                        if (!csv.Read()) break;
                    }
                    catch (CsvHelper.MissingFieldException)
                    {
                        result.TotalProcessed++;
                        result.Failed++;
                        result.Errors.Add(new ImportError { Row = csv.Parser.Row, Error = "Row is missing expected columns." });
                        continue;
                    }
                    catch (CsvHelper.BadDataException)
                    {
                        result.TotalProcessed++;
                        result.Failed++;
                        result.Errors.Add(new ImportError { Row = csv.Parser.Row, Error = "Row contains structurally invalid data." });
                        continue;
                    }

                    result.TotalProcessed++;
                    int rowNum = csv.Parser.Row;

                    try
                    {
                        var user = csv.GetField<string>("User");
                        if (string.IsNullOrWhiteSpace(user))
                        {
                            result.Failed++;
                            result.Errors.Add(new ImportError { Row = rowNum, Error = "User is required and cannot be empty." });
                            continue;
                        }

                        var name = csv.GetField<string>("Name");
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            result.Failed++;
                            result.Errors.Add(new ImportError { Row = rowNum, Error = "Name is required and cannot be empty." });
                            continue;
                        }

                        if (processedNames.Contains((user, name)))
                        {
                            result.Failed++;
                            result.Errors.Add(new ImportError { Row = rowNum, Error = $"Duplicate todo found for User '{user}' and Name '{name}'." });
                            continue;
                        }

                        var completedString = csv.GetField<string>("Completed");
                        if (!bool.TryParse(
                            completedString,
                            out bool completed))
                        {
                            result.Failed++;
                            result.Errors.Add(new ImportError { Row = rowNum, Error = "Completed must be a valid boolean." });
                            continue;
                        }

                        var dueDateString = csv.GetField<string>("DueDate");
                        string[] formats = { "yyyy-MM-dd", "MM/dd/yyyy" };
                        if (!DateTime.TryParseExact(
                            dueDateString,
                            formats,
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None,
                            out DateTime dueDate))
                        {
                            result.Failed++;
                            result.Errors.Add(new ImportError { Row = rowNum, Error = "DueDate must be a valid date." });
                            continue;
                        }

                        var priorityString = csv.GetField<string>("Priority");
                        if (!Enum.TryParse<TodoItem.PriorityEnum>(
                            priorityString,
                            true,
                            out var priority))
                        {
                            result.Failed++;
                            result.Errors.Add(new ImportError { Row = rowNum, Error = $"Priority '{priorityString}' is not valid." });
                            continue;
                        }

                        var newTodo = new TodoItem
                        {
                            User = user,
                            Name = name,
                            Completed = completed,
                            DueDate = dueDate,
                            Priority = priority
                        };

                        importedItems.Add(newTodo);
                        processedNames.Add((user, name));
                        result.Imported++;
                    }
                    catch (CsvHelper.MissingFieldException)
                    {
                        result.Failed++;
                        result.Errors.Add(new ImportError { Row = rowNum, Error = "Row is missing expected columns." });
                    }
                    catch (Exception ex)
                    {
                        result.Failed++;
                        result.Errors.Add(new ImportError { Row = rowNum, Error = $"Unexpected error: {ex.Message}" });
                    }
                }
            }

            if (importedItems.Any())
            {
                _context.TodoItems.AddRange(importedItems);
                await _context.SaveChangesAsync();
            }

            return Ok(result);
        }

        private bool TodoItemExists(long id)
        {
            return _context.TodoItems.Any(e => e.Id == id);
        }
    }
}
