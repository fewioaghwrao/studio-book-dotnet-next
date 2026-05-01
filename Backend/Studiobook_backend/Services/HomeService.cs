using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos.Home;

namespace Studiobook_backend.Services
{
    public class HomeService
    {
        private readonly AppDbContext _context;

        public HomeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<HomeResponseDto> GetHomeAsync()
        {
            var roomQuery = _context.Rooms
                .AsNoTracking()
                .Select(room => new HomeRoomDto
                {
                    Id = room.Id,
                    Name = room.Name,
                    Address = room.Address,
                    Price = room.Price,
                    ImageName = room.ImageName,

                    // トップページなので公開レビューのみを対象にする
                    AverageScore = room.Reviews
                        .Where(review => review.PublicVisible)
                        .Select(review => (double?)review.Score)
                        .Average(),

                    ReviewCount = room.Reviews
                        .Count(review => review.PublicVisible)
                });

            var popularRooms = await roomQuery
                .OrderByDescending(room => room.ReviewCount)
                .ThenByDescending(room => room.AverageScore ?? 0)
                .ThenBy(room => room.Id)
                .Take(3)
                .ToListAsync();

            var newRooms = await roomQuery
                .OrderByDescending(room => room.Id)
                .Take(4)
                .ToListAsync();

            return new HomeResponseDto
            {
                PopularRooms = popularRooms,
                NewRooms = newRooms
            };
        }
    }
}