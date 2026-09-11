using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using TodoApi.Controllers;
using TodoApi.Models;
using Xunit;

namespace TodoApi.Tests
{
    public class ImportTodosTests
    {
        private TodoContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<TodoContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new TodoContext(options);
        }

        private IFormFile CreateMockFile(string content)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.OpenReadStream()).Returns(stream);
            fileMock.Setup(f => f.Length).Returns(stream.Length);
            fileMock.Setup(f => f.FileName).Returns("test.csv");
            return fileMock.Object;
        }

        [Fact]
        public async Task ImportTodos_ValidFile_ReturnsOkWithStats()
        {
            var context = GetInMemoryContext();
            var controller = new TodoItemsController(context);

            var csvContent = @"User,Name,Completed,DueDate,Priority
leniel@example.com,Buy groceries,false,2026-09-15,Normal
leniel@example.com,Finish report,true,2026-09-10,High";

            var file = CreateMockFile(csvContent);

            var result = await controller.ImportTodos(file);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var importResult = Assert.IsType<ImportResult>(okResult.Value);

            Assert.Equal(
                2,
                importResult.TotalProcessed);
            Assert.Equal(
                2,
                importResult.Imported);
            Assert.Equal(
                0,
                importResult.Failed);
            Assert.Empty(importResult.Errors);

            Assert.Equal(
                2,
                context.TodoItems.Count());
        }

        [Fact]
        public async Task ImportTodos_WithMalformedRows_HandlesExplicitly()
        {
            var context = GetInMemoryContext();
            var controller = new TodoItemsController(context);

            var csvContent = @"User,Name,Completed,DueDate,Priority
leniel@example.com,,false,2026-09-15,Normal
,No user,false,2026-09-15,Normal
leniel@example.com,Bad date,false,2026-13-40,Normal
leniel@example.com,Bad priority,false,2026-09-15,Urgent
leniel@example.com,Missing column,false,2026-09-15
leniel@example.com,Valid todo,false,2026-09-15,Normal";

            var file = CreateMockFile(csvContent);

            var result = await controller.ImportTodos(file);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var importResult = Assert.IsType<ImportResult>(okResult.Value);

            Assert.Equal(
                6,
                importResult.TotalProcessed);
            Assert.Equal(
                1,
                importResult.Imported);
            Assert.Equal(
                5,
                importResult.Failed);
            Assert.Equal(
                5,
                importResult.Errors.Count);

            var validTodo = context.TodoItems.Single();
            Assert.Equal(
                "Valid todo",
                validTodo.Name);
            Assert.Equal(
                "leniel@example.com",
                validTodo.User);
        }

        [Fact]
        public async Task ImportTodos_DuplicateRows_AreRejected()
        {
            var context = GetInMemoryContext();
            context.TodoItems.Add(new TodoItem { User = "leniel@example.com", Name = "Existing todo", DueDate = DateTime.Now, Priority = TodoItem.PriorityEnum.Normal });
            context.SaveChanges();

            var controller = new TodoItemsController(context);

            var csvContent = @"User,Name,Completed,DueDate,Priority
leniel@example.com,Existing todo,false,2026-09-15,Normal
leniel@example.com,New todo,false,2026-09-15,Normal
leniel@example.com,New todo,false,2026-09-15,Normal";

            var file = CreateMockFile(csvContent);

            var result = await controller.ImportTodos(file);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var importResult = Assert.IsType<ImportResult>(okResult.Value);

            Assert.Equal(
                3,
                importResult.TotalProcessed);
            Assert.Equal(
                1,
                importResult.Imported);
            Assert.Equal(
                2,
                importResult.Failed);

            var todos = context.TodoItems.ToList();
            Assert.Equal(
                2,
                todos.Count); // 1 existing + 1 new (the duplicate new was rejected)
        }
    }
}
