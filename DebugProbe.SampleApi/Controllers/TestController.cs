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

        [HttpGet(Name = "TestGet")]
        public List<string> TestGet(int id)
        {
            List<string> result = new List<string>();
            if (id == 1)
            {
                throw new Exception("Id cannot be 1");
            }
            else
            {
                result.Add($"Received id: {id}");
                result.Add($"DateTime: {DateTime.Now}");
                result.Add("Id is not 1");
            }

            return result;
        }
    }
}
