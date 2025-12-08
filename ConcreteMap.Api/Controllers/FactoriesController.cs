using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ConcreteMap.Domain.Models;
using ConcreteMap.Infrastructure.Data;

namespace ConcreteMap.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FactoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FactoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Получить все заводы
        [HttpGet]
        public async Task<ActionResult<List<FactoryDto>>> GetFactories()
        {
            var factories = await _context.Factories
                .AsNoTracking()
                .Select(f => new FactoryDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Latitude = f.Latitude,
                    Longitude = f.Longitude,
                    IsVip = f.IsVip,
                    Address = f.Address,
                    Phone = f.Phone,
                    ProductCategories = f.ProductCategories,
                    VipProducts = f.VipProducts,
                    PriceUrl = f.PriceUrl
                })
                .ToListAsync();

            return Ok(factories);
        }

        // Поиск (новое)
        [HttpGet("search")]
        public async Task<ActionResult<List<FactoryDto>>> SearchFactories([FromQuery] string? q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Ok(new List<FactoryDto>());
            }

            var term = $"%{q.Trim()}%";

            var factories = await _context.Factories
                .AsNoTracking()
                .Where(x =>
                    (x.Name != null && EF.Functions.ILike(x.Name, term)) ||
                    (x.ProductCategories != null && EF.Functions.ILike(x.ProductCategories, term)) ||
                    (x.VipProducts != null && EF.Functions.ILike(x.VipProducts, term)) ||
                    (x.Comment != null && EF.Functions.ILike(x.Comment, term))
                )
                .Select(f => new FactoryDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Latitude = f.Latitude,
                    Longitude = f.Longitude,
                    IsVip = f.IsVip,
                    Address = f.Address,
                    Phone = f.Phone,
                    ProductCategories = f.ProductCategories,
                    VipProducts = f.VipProducts,
                    PriceUrl = f.PriceUrl
                })
                .ToListAsync();

            return Ok(factories);
        }
    }
}