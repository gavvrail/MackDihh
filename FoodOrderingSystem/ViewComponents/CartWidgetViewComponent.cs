using System.Threading.Tasks;
using FoodOrderingSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace FoodOrderingSystem.ViewComponents
{
    public class CartWidgetViewComponent : ViewComponent
    {
        private readonly CartService _cartService;

        public CartWidgetViewComponent(CartService cartService)
        {
            _cartService = cartService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var cartItemCount = await _cartService.GetCartItemCountAsync();
            return View(cartItemCount);
        }
    }
}
