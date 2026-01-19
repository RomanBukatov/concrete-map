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
                    PriceUrl = f.PriceUrl, // Ссылка на сайт
                    Comment = f.Comment,
                    
                    // --- ВОТ ЭТО БЫЛО ПРОПУЩЕНО ---
                    PriceListUrl = f.PriceListUrl 
                })
                .ToListAsync();

            return Ok(factories);
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<FactoryDto>>> SearchFactories(
            [FromQuery] string? q, 
            [FromQuery] bool searchName = true, 
            [FromQuery] bool searchProd = true, 
            [FromQuery] bool searchPrice = true)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Ok(new List<FactoryDto>());
            }

            var term = $"%{q.Trim()}%";

            // Если не выбрана ни одна галочка, ничего не ищем (или можно вернуть всё, но логичнее ничего)
            if (!searchName && !searchProd && !searchPrice)
            {
                return Ok(new List<FactoryDto>());
            }

            var query = _context.Factories.AsNoTracking().AsQueryable();

            // Динамическое построение условия OR
            query = query.Where(x => 
                (searchName && x.Name != null && EF.Functions.ILike(x.Name, term)) ||
                
                (searchProd && (
                    (x.ProductCategories != null && EF.Functions.ILike(x.ProductCategories, term)) ||
                    (x.VipProducts != null && EF.Functions.ILike(x.VipProducts, term))
                )) ||
                
                (searchPrice && (
                    (x.Comment != null && EF.Functions.ILike(x.Comment, term)) ||
                    (x.PriceListContent != null && EF.Functions.ILike(x.PriceListContent, term))
                ))
            );

            var factories = await query
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
                    PriceUrl = f.PriceUrl,
                    Comment = f.Comment,
                    PriceListUrl = f.PriceListUrl
                })
                .ToListAsync();

            return Ok(factories);
        }
    }
}