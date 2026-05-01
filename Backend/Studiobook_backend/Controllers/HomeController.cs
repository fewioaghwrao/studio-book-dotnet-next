using Microsoft.AspNetCore.Mvc;
using Studiobook_backend.Dtos.Home;
using Studiobook_backend.Services;

namespace Studiobook_backend.Controllers
{
    [ApiController]
    [Route("api/home")]
    public class HomeController : ControllerBase
    {
        private readonly HomeService _service;

        public HomeController(HomeService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<HomeResponseDto>> Get()
        {
            var result = await _service.GetHomeAsync();
            return Ok(result);
        }
    }
}