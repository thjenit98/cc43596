namespace lab_FullTextSearch.Controllers;

public class ProductDetail
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public decimal Price { get; set; }
    public string Description { get; set; }
    public string[] Colors { get; set; }
    public string[] Tags { get; set; }
}