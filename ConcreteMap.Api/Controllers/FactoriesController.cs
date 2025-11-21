using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConcreteMap.Domain.Models;
using ConcreteMap.Infrastructure.Data;

namespace ConcreteMap.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FactoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FactoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

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
                    PriceUrl = f.PriceUrl
                })
                .ToListAsync();

            return Ok(factories);
        }
    }
}