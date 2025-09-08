using FoodOrderingSystem.Models;

namespace FoodOrderingSystem.ViewModels
{
    public class MenuViewModel
    {
        public List<MenuItem> MenuItems { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public string SelectedSortBy { get; set; } = "name";
        public string SelectedCategory { get; set; } = "";
        public HashSet<int> WishlistItemIds { get; set; } = new();

        public List<SortOption> SortOptions => new()
        {
            new SortOption { Value = "name", Text = "Name A-Z" },
            new SortOption { Value = "price-low", Text = "Price: Low to High" },
            new SortOption { Value = "price-high", Text = "Price: High to Low" },
            new SortOption { Value = "rating", Text = "Top Rated" },
            new SortOption { Value = "popular", Text = "Most Popular" },
            new SortOption { Value = "newest", Text = "Newest First" }
        };
    }

    public class SortOption
    {
        public string Value { get; set; } = "";
        public string Text { get; set; } = "";
    }
}
