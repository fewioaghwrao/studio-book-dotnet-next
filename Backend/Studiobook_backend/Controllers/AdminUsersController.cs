using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Studiobook_backend.Dtos.Admin;
using Studiobook_backend.Services;

namespace Studiobook_backend.Controllers
{
    [ApiController]
    [Authorize(Roles = "Admin")]
    [Route("api/admin/users")]
    public class AdminUsersController : ControllerBase
    {
        private readonly AdminUserService _service;

        public AdminUsersController(AdminUserService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<AdminUserListResponseDto>> GetList(
            [FromQuery] string? keyword,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetListAsync(keyword, page, pageSize);
            return Ok(result);
        }

        [HttpGet("{userId:int}")]
        public async Task<ActionResult<AdminUserDetailDto>> GetDetail(int userId)
        {
            var result = await _service.GetDetailAsync(userId);

            if (result == null)
            {
                return NotFound(new
                {
                    message = "会員情報が見つかりません。"
                });
            }

            return Ok(result);
        }
    }
}