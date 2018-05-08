using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProductCatalogApi.Data;
using ProductCatalogApi.Domain;
using ProductCatalogApi.ViewModels;

namespace ProductCatalogApi.Controllers
{
    [Produces("application/json")]
    [Route("api/Catalog")]
    public class CatalogController : Controller
    {
        private readonly CatalogContext _catalogContext;
        private readonly IOptionsSnapshot<CatalogSettings> _settings;

        public CatalogController(CatalogContext catalogContext, IOptionsSnapshot<CatalogSettings> settings)
        {
            _catalogContext = catalogContext;
            _settings = settings;
            ((DbContext)catalogContext).ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        [HttpGet]
        [Route("types")]
        public async Task<IActionResult> CatalogTypes()
        {
            var items = await _catalogContext.CatalogTypes.ToListAsync();
            return Ok(items);
        }

        [HttpGet]
        [Route("brands")]
        public async Task<IActionResult> CatalogBrands()
        {
            var items = await _catalogContext.CatalogBrands.ToListAsync();
            return Ok(items);
        }
       
        [HttpGet]
        [Route("item/{id:int}")]
        public async Task<IActionResult> GetItemById(int id)
        {
            if (id <= 0)
            {
                return BadRequest();
            }
            var item = await _catalogContext.CatalogItems.SingleOrDefaultAsync(x => x.Id == id) ;
            if (item != null)
            {
                item.PictureUrl = ReplaceStrgURL(item.PictureUrl);
                return Ok(item);
            }
            return NotFound();
        }

        [HttpPost]
        [Route("items")]
        public async Task<IActionResult> CreateProduct([FromBody] CatalogItem product)
        {
            var item = new CatalogItem
            {
                CatalogBrandId = product.CatalogBrandId,
                CatalogTypeId = product.CatalogTypeId,
                Description = product.Description,
                Name = product.Name,
                PictureFileName = product.PictureFileName,
                PictureUrl = product.PictureUrl,
                Price = product.Price
            };
            _catalogContext.CatalogItems.Add(item);
            await _catalogContext.SaveChangesAsync();
            return CreatedAtAction(nameof(GetItemById), new { id = item.Id});
        }

        [HttpPut]
        [Route("items")]
        public async Task<IActionResult> UpdateProduct([FromBody] CatalogItem product)
        {
            var catalogItem = await _catalogContext.CatalogItems.SingleOrDefaultAsync(i => i.Id == product.Id);
            if (catalogItem == null)
                return NotFound(new { Message = $"Item {product.Id} not found." });

            catalogItem = product;
            _catalogContext.CatalogItems.Update(catalogItem);
            await _catalogContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetItemById), new { id = catalogItem.Id });
        }

        [HttpDelete]
        [Route("items")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var catalogItem = await _catalogContext.CatalogItems.SingleOrDefaultAsync(i => i.Id == id);
            if (catalogItem == null)
                return NotFound(new { Message = $"Item {id} not found." });

             _catalogContext.CatalogItems.Remove(catalogItem);
            await _catalogContext.SaveChangesAsync();
            return Ok();
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> Items(string name ,  int? catalogTypeId, int? catalogBrandId, [FromQuery] int pageSize = 6, [FromQuery] int pageIndex = 0)
        {
            var root = (IQueryable<CatalogItem>)_catalogContext.CatalogItems;
            if (catalogTypeId.HasValue)
                root = root.Where(c => c.CatalogTypeId == catalogTypeId);
            
            if (catalogBrandId.HasValue)            
                root = root.Where(c => c.CatalogBrandId == catalogBrandId);

            if (name != null)
                root = root.Where(c => c.Name.StartsWith(name));

            var total = await root.LongCountAsync();
            var itemsonpage = await root.OrderBy(c => c.Name)
                                    .Skip(pageSize * pageIndex)
                                    .Take(pageSize)
                                    .ToListAsync();
            itemsonpage = ChangeUrlPlaceHolder(itemsonpage);
            var model = new PaginatedItemsViewModel<CatalogItem>(pageSize, pageIndex, total, itemsonpage);
            return Ok(model);
        }

        private List<CatalogItem> ChangeUrlPlaceHolder(List<CatalogItem> items)
        {
            items.ForEach(
                x => x.PictureUrl = ReplaceStrgURL(x.PictureUrl)
            );
            return items;
        }

        private string ReplaceStrgURL(string old) {

            return old.Replace("http://externalcatalogbaseurltobereplaced", _settings.Value.ExternalCatalogBaseUrl);
        }

    }
}