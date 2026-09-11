using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Models;
using System;
using System.Globalization;
using System.IO;
using Microsoft.AspNetCore.Http;

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

        /// <summary>
        /// Imports todo items from a CSV file. Validates each row and rejects
        /// malformed entries with per-row error details.
        /// </summary>
        [HttpPost("import")]
        [AllowAnonymous]
        public async Task<ActionResult<ImportResult>> ImportTodos(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var result = new ImportResult();
            var importedItems = new List<TodoItem>();

            var existingTodos = await _context.TodoItems
                .Select(t => new { t.User, t.Name })
                .ToListAsync();
            var seenKeys = new HashSet<(string User, string Name)>(
                existingTodos.Select(t => (t.User, t.Name)));

            var dateFormats = new[] { "yyyy-MM-dd", "MM/dd/yyyy" };

            using (var stream = file.OpenReadStream())
            using (var reader = new StreamReader(stream))
            using (var csv = new CsvHelper.CsvReader(
                reader,
                CultureInfo.InvariantCulture))
            {
                csv.Read();
                csv.ReadHeader();
                var expectedColumnCount = csv.HeaderRecord.Length;

                while (true)
                {
                    try
                    {
                        if (!csv.Read())
                            break;
                    }
                    catch (CsvHelper.MissingFieldException)
                    {
                        result.TotalProcessed++;
                        result.Failed++;
                        result.Errors.Add(new ImportError
                        {
                            Row = csv.Parser.Row,
                            Error = "Row has missing columns (wrong column count)."
                        });
                        continue;
                    }
                    catch (CsvHelper.BadDataException)
                    {
                        result.TotalProcessed++;
                        result.Failed++;
                        result.Errors.Add(new ImportError
                        {
                            Row = csv.Parser.Row,
                            Error = "Row contains structurally invalid data."
                        });
                        continue;
                    }

                    result.TotalProcessed++;
                    var rowNum = csv.Parser.Row;

                    try
                    {
                        if (csv.Parser.Record.Length != expectedColumnCount)
                        {
                            result.Failed++;
                            result.Errors.Add(new ImportError
                            {
                                Row = rowNum,
                                Error = "Row has the wrong column count."
                            });
                            continue;
                        }

                        var user = csv.GetField<string>("User");
                        if (string.IsNullOrWhiteSpace(user))
                        {
                            result.Failed++;
                            result.Errors.Add(new ImportError
                            {
                                Row = rowNum,
                                Error = "User is required and cannot be blank."
                            });
                            continue;
                        }

                        var name = csv.GetField<string>("Name");
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            result.Failed++;
                            result.Errors.Add(new ImportError
                            {
                                Row = rowNum,
                                Error = "Name is required and cannot be blank."
                            });
                            continue;
                        }

                        if (seenKeys.Contains((user, name)))
                        {
                            result.Failed++;
                            result.Errors.Add(new ImportError
                            {
                                Row = rowNum,
                                Error = $"Duplicate: a todo with User '{user}' and Name '{name}' already exists."
                            });
                            continue;
                        }

                        var completedStr = csv.GetField<string>("Completed");
                        if (string.IsNullOrWhiteSpace(completedStr))
                        {
                            result.Failed++;
                            result.Errors.Add(new ImportError
                            {
                                Row = rowNum,
                                Error = "Completed is required and cannot be blank."
                            });
                            continue;
                        }

                        if (!bool.TryParse(
                            completedStr,
                            out var completed))
                        {
                            result.Failed++;
                            result.Errors.Add(new ImportError
                            {
                                Row = rowNum,
                                Error = $"Completed value '{completedStr}' is not a valid boolean."
                            });
                            continue;
                        }

                        var dueDateStr = csv.GetField<string>("DueDate");
                        if (string.IsNullOrWhiteSpace(dueDateStr) ||
                            !DateTime.TryParseExact(
                                dueDateStr,
                                dateFormats,
                                CultureInfo.InvariantCulture,
                                DateTimeStyles.None,
                                out var dueDate))
                        {
                            result.Failed++;
                            result.Errors.Add(new ImportError
                            {
                                Row = rowNum,
                                Error = $"DueDate value '{dueDateStr}' is not a valid date (accepted: yyyy-MM-dd, MM/dd/yyyy)."
                            });
                            continue;
                        }

                        var priorityStr = csv.GetField<string>("Priority");
                        if (string.IsNullOrWhiteSpace(priorityStr))
                        {
                            result.Failed++;
                            result.Errors.Add(new ImportError
                            {
                                Row = rowNum,
                                Error = "Priority is required and cannot be blank."
                            });
                            continue;
                        }

                        if (!Enum.TryParse<TodoItem.PriorityEnum>(
                            priorityStr,
                            true,
                            out var priority) ||
                            !Enum.IsDefined(typeof(TodoItem.PriorityEnum), priority))
                        {
                            result.Failed++;
                            result.Errors.Add(new ImportError
                            {
                                Row = rowNum,
                                Error = $"Priority value '{priorityStr}' is not valid (accepted: Low, Normal, High, or 0, 1, 2)."
                            });
                            continue;
                        }

                        var todo = new TodoItem
                        {
                            User = user,
                            Name = name,
                            Completed = completed,
                            DueDate = dueDate,
                            Priority = priority
                        };

                        importedItems.Add(todo);
                        seenKeys.Add((user, name));
                        result.Imported++;
                    }
                    catch (CsvHelper.MissingFieldException)
                    {
                        result.Failed++;
                        result.Errors.Add(new ImportError
                        {
                            Row = rowNum,
                            Error = "Row has missing columns (wrong column count)."
                        });
                    }
                    catch (Exception ex)
                    {
                        result.Failed++;
                        result.Errors.Add(new ImportError
                        {
                            Row = rowNum,
                            Error = $"Unexpected error: {ex.Message}"
                        });
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
