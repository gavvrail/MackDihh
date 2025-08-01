using FoodOrderingSystem.Models;

namespace FoodOrderingSystem.ViewModels
{
    public class ReportViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalMenuItems { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalSales { get; set; }
        public List<MonthlySalesData> MonthlySales { get; set; } = new();
        public List<OrderStatusData> OrderStatusBreakdown { get; set; } = new();
        public List<CategorySalesData> TopCategories { get; set; } = new();
    }

    public class MonthlySalesData
    {
        public int Month { get; set; }
        public decimal Sales { get; set; }
        public int OrderCount { get; set; }
    }

    public class OrderStatusData
    {
        public OrderStatus Status { get; set; }
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    public class CategorySalesData
    {
        public string CategoryName { get; set; } = "";
        public decimal TotalSales { get; set; }
        public int TotalQuantity { get; set; }
    }
}
