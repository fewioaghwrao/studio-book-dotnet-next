using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Studiobook_backend.Dtos.Admin;
using Studiobook_backend.Services;

namespace Studiobook_backend.Controllers
{
    [ApiController]
    [Authorize(Roles = "Admin")]
    [Route("api/admin/reservations")]
    public class AdminReservationsController : ControllerBase
    {
        private readonly AdminReservationService _service;

        public AdminReservationsController(AdminReservationService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<AdminReservationListResponseDto>> GetList(
            [FromQuery] string? keyword,
            [FromQuery] string? status,
            [FromQuery] int? reservationId,
            [FromQuery] int? roomId,
            [FromQuery] DateTime? startFrom,
            [FromQuery] DateTime? startTo,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetListAsync(
                keyword,
                status,
                reservationId,
                roomId,
                startFrom,
                startTo,
                page,
                pageSize
            );

            return Ok(result);
        }
    }
}