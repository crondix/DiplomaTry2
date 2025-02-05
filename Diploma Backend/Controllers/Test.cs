using Microsoft.AspNetCore.Mvc;

namespace Diploma_Backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    
    public class TestController : ControllerBase
    {
       


        [HttpGet(Name = "Test")]
        public IActionResult Get()
        {
            return Ok(new { message = "Test" });
        }
    }
}
