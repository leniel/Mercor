using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Controllers;
using TodoApi.Models;
using Xunit;

namespace TodoApi.Tests
{
    /// <summary>
    /// Tests CSV import validation and persistence behavior.
    /// </summary>
    public class TodoItemsControllerTests
    {
        /// <summary>
        /// Persists valid rows using supported date and priority formats.
        /// </summary>
        [Fact]
        public async Task ImportTodos_ImportsValidRows()
        {
            await using var context = CreateContext();
            var result = await ImportCsv(
                context,
                "User,Name,Completed,DueDate,Priority\n" +
                "new@example.com,First valid,false,2026-09-15,Normal\n" +
                "new@example.com,Second valid,true,09/20/2026,2");

            Assert.Equal(
                expected: 2,
                actual: result.TotalProcessed);
            Assert.Equal(
                expected: 2,
                actual: result.Imported);
            Assert.Equal(
                expected: 0,
                actual: result.Failed);
            Assert.Equal(
                expected: 2,
                actual: await context.TodoItems.CountAsync());
        }

        /// <summary>
        /// Rejects a row whose user and name match an existing todo.
        /// </summary>
        [Fact]
        public async Task ImportTodos_RejectsDuplicateRows()
        {
            await using var context = CreateContext();
            context.TodoItems.Add(new TodoItem
            {
                User = "existing@example.com",
                Name = "Already saved",
                Completed = false,
                DueDate = new DateTime(
                    year: 2026,
                    month: 9,
                    day: 1),
                Priority = TodoItem.PriorityEnum.Normal
            });
            await context.SaveChangesAsync();

            var result = await ImportCsv(
                context,
                "User,Name,Completed,DueDate,Priority\n" +
                "existing@example.com,Already saved,false,2026-09-16,High");

            Assert.Equal(
                expected: 0,
                actual: result.Imported);
            Assert.Equal(
                expected: 1,
                actual: result.Failed);
            Assert.Contains(
                result.Errors,
                error => error.Error.Contains("Duplicate"));
        }

        /// <summary>
        /// Rejects a row with a blank required user value.
        /// </summary>
        [Fact]
        public async Task ImportTodos_RejectsBlankUser()
        {
            await using var context = CreateContext();
            var result = await ImportCsv(
                context,
                "User,Name,Completed,DueDate,Priority\n" +
                ",Missing user,false,2026-09-16,Low");

            Assert.Equal(
                expected: 0,
                actual: result.Imported);
            Assert.Equal(
                expected: 1,
                actual: result.Failed);
            Assert.Contains(
                result.Errors,
                error => error.Error.Contains("User is required"));
        }

        /// <summary>
        /// Rejects an impossible due date.
        /// </summary>
        [Fact]
        public async Task ImportTodos_RejectsInvalidDueDate()
        {
            await using var context = CreateContext();
            var result = await ImportCsv(
                context,
                "User,Name,Completed,DueDate,Priority\n" +
                "new@example.com,Impossible date,false,2026-13-16,Low");

            Assert.Equal(
                expected: 0,
                actual: result.Imported);
            Assert.Equal(
                expected: 1,
                actual: result.Failed);
            Assert.Contains(
                result.Errors,
                error => error.Error.Contains("DueDate value '2026-13-16'"));
        }

        /// <summary>
        /// Rejects a row with a blank required completed value.
        /// </summary>
        [Fact]
        public async Task ImportTodos_RejectsBlankCompleted()
        {
            await using var context = CreateContext();
            var result = await ImportCsv(
                context,
                "User,Name,Completed,DueDate,Priority\n" +
                "new@example.com,Blank completed,,2026-09-16,Low");

            Assert.Contains(
                result.Errors,
                error => error.Error.Contains("Completed is required"));
        }

        /// <summary>
        /// Rejects a row with a blank required priority value.
        /// </summary>
        [Fact]
        public async Task ImportTodos_RejectsBlankPriority()
        {
            await using var context = CreateContext();
            var result = await ImportCsv(
                context,
                "User,Name,Completed,DueDate,Priority\n" +
                "new@example.com,Blank priority,false,2026-09-16,");

            Assert.Contains(
                result.Errors,
                error => error.Error.Contains("Priority is required"));
        }

        /// <summary>
        /// Rejects numeric priorities that are not defined by the enum.
        /// </summary>
        [Fact]
        public async Task ImportTodos_RejectsUndefinedNumericPriority()
        {
            await using var context = CreateContext();
            var result = await ImportCsv(
                context,
                "User,Name,Completed,DueDate,Priority\n" +
                "new@example.com,Unexpected priority,false,2026-09-16,3");

            Assert.Contains(
                result.Errors,
                error => error.Error.Contains("Priority value '3'"));
        }

        /// <summary>
        /// Rejects rows with more fields than the header declares.
        /// </summary>
        [Fact]
        public async Task ImportTodos_RejectsRowsWithExtraColumns()
        {
            await using var context = CreateContext();
            var result = await ImportCsv(
                context,
                "User,Name,Completed,DueDate,Priority\n" +
                "new@example.com,Extra field,false,2026-09-16,High,extra");

            Assert.Contains(
                result.Errors,
                error => error.Error.Contains("wrong column count"));
        }

        private static TodoContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<TodoContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new TodoContext(options);
        }

        private static async Task<ImportResult> ImportCsv(
            TodoContext context,
            string csv)
        {
            var controller = new TodoItemsController(context);
            var actionResult = await controller.ImportTodos(CreateFile(csv));
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);

            return Assert.IsType<ImportResult>(okResult.Value);
        }

        private static IFormFile CreateFile(string csv)
        {
            var bytes = Encoding.UTF8.GetBytes(csv);
            var stream = new MemoryStream(bytes);

            return new FormFile(
                stream,
                0,
                stream.Length,
                "file",
                "todos.csv")
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/csv"
            };
        }
    }
}
