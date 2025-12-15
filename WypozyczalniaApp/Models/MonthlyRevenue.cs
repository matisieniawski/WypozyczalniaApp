namespace WypozyczalniaApp.Models
{
    public class MonthlyRevenue
    {
        public int MonthNumber { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
    }
}