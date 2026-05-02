using DebugProbe.SampleApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace DebugProbe.SampleApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DemoController : ControllerBase
    {
        private readonly ILogger<DemoController> _logger;

        public DemoController(ILogger<DemoController> logger)
        {
            _logger = logger;
        }


        [HttpGet("GetUser/{id}")]
        public IActionResult GetUser(int id)
        {
            if (id == 0)
            {
                return BadRequest(new
                {
                    error = "Invalid user id"
                });
            }

            if (id == 401)
            {
                return Unauthorized(new
                {
                    error = "Token missing or invalid"
                });
            }

            if (id == 403)
            {
                return StatusCode(403, new
                {
                    error = "Access denied"
                });
            }

            if (id == 500)
            {
                throw new Exception("Database failure");
            }

            return Ok(new
            {
                id,
                name = id % 2 == 0 ? "John Doe" : "Alice Smith",
                email = $"user{id}@example.com",
                roles = id % 2 == 0 ? new[] { "Admin", "User" } : new[] { "User" }
            });
        }

        [HttpGet("GetOrder/{id}")]
        public IActionResult GetOrder(int id)
        {
            return Ok(new
            {
                orderId = id,
                createdAt = DateTime.UtcNow,
                status = id % 2 == 0 ? "Completed" : "Pending",
                total = id * 25.5,
                customer = new
                {
                    id = id,
                    name = "John Doe"
                },
                items = new[]
                {
                new { product = "Laptop", qty = 1, price = 1200 },
                new { product = "Mouse", qty = 2, price = 25 }
            },
                metadata = new
                {
                    source = "web",
                    traceId = Guid.NewGuid()
                }
            });
        }

        [HttpPost("CreateOrder")]
        public IActionResult CreateOrder([FromBody] CreateOrderRequest request)
        {
            if (request == null || request.Items == null || !request.Items.Any())
            {
                return BadRequest(new
                {
                    error = "Order must contain at least one item"
                });
            }

            if (request.UserId == 401)
            {
                return Unauthorized(new
                {
                    error = "Token missing or invalid"
                });
            }

            if (request.UserId == 403)
            {
                return StatusCode(403, new
                {
                    error = "User does not have permission to create orders"
                });
            }

            if (request.UserId == 500)
            {
                throw new Exception("Database write failed");
            }

            return Ok(new
            {
                orderId = new Random().Next(1000, 9999),
                status = "Created",
                createdAt = DateTime.UtcNow
            });
        }
    }
}
