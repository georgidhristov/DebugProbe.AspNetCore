using Microsoft.AspNetCore.Mvc;

namespace DebugProbe.SampleApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ILogger<TestController> _logger;

        public TestController(ILogger<TestController> logger)
        {
            _logger = logger;
        }

        // 1. Basic (your existing)
        [HttpGet("basic")]
        public List<string> Basic(int id)
        {
            if (id == 1)
                throw new Exception("Id cannot be 1");

            return new List<string>
        {
            $"Received id: {id}",
            $"DateTime: {DateTime.Now}",
            "Id is not 1"
        };
        }

        // 2. Dynamic response (good for diff)
        [HttpGet("dynamic")]
        public object Dynamic(int id)
        {
            if (id % 2 == 0)
            {
                return new
                {
                    Type = "Even",
                    Value = id,
                    Time = DateTime.Now
                };
            }

            return new
            {
                Type = "Odd",
                Message = "Different shape",
                Random = Guid.NewGuid()
            };
        }

        // 3. Based on header (perfect for env diff)
        [HttpGet("header")]
        public object FromHeader()
        {
            var source = Request.Headers["x-env"].FirstOrDefault();

            if (source == "dev")
            {
                return new { Env = "DEV", Debug = true };
            }

            return new { Env = "PROD", Debug = false };
        }

        [HttpGet("complex")]
        public object Complex(int id)
        {
            return new
            {
                Id = id,
                Timestamp = DateTime.UtcNow,
                User = new
                {
                    Name = id % 2 == 0 ? "John" : "Alice",
                    Roles = id % 2 == 0
                        ? new[] { "Admin", "User" }
                        : new[] { "User" },
                    Profile = new
                    {
                        Age = 20 + id,
                        Active = id % 2 == 0,
                        Address = new
                        {
                            City = id % 2 == 0 ? "Sofia" : "Plovdiv",
                            Zip = $"100{id}"
                        }
                    }
                },
                Orders = Enumerable.Range(1, 3).Select(x => new
                {
                    OrderId = x,
                    Amount = id * x * 10,
                    Status = x % 2 == 0 ? "Completed" : "Pending",
                    Items = new[]
                    {
                new { Name = "ItemA", Qty = x },
                new { Name = "ItemB", Qty = x + 1 }
            }
                }),
                Meta = new
                {
                    Source = id % 2 == 0 ? "API-A" : "API-B",
                    Flags = new
                    {
                        IsTest = true,
                        HasError = id == 5
                    }
                }
            };
        }
    }
}
