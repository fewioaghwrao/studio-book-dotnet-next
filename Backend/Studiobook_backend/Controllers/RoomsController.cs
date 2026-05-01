using Microsoft.AspNetCore.Mvc;
using Studiobook_backend.Dtos.Rooms;
using Studiobook_backend.Services;

namespace Studiobook_backend.Controllers
{
    [ApiController]
    [Route("api/rooms")]
    public class RoomsController : ControllerBase
    {
        private readonly RoomSearchService _searchService;
        private readonly RoomDetailService _detailService;

        public RoomsController(
            RoomSearchService searchService,
            RoomDetailService detailService)
        {
            _searchService = searchService;
            _detailService = detailService;
        }

        [HttpGet]
        public async Task<ActionResult<RoomListResponseDto>> GetList(
            [FromQuery] string? keyword,
            [FromQuery] string? area,
            [FromQuery] int? price,
            [FromQuery] string? order,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _searchService.GetListAsync(
                keyword,
                area,
                price,
                order,
                page,
                pageSize
            );

            return Ok(result);
        }

        [HttpGet("{roomId:int}")]
        public async Task<ActionResult<RoomDetailDto>> GetDetail(
            [FromRoute] int roomId)
        {
            var result = await _detailService.GetDetailAsync(roomId);

            if (result == null)
            {
                return NotFound(new
                {
                    message = "スタジオが見つかりません。"
                });
            }

            return Ok(result);
        }
    }
}