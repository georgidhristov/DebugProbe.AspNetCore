namespace DebugProbe.SampleApi.Models;
public class CreateOrderRequest
{
    public int UserId { get; set; }
    public List<OrderItem> Items { get; set; }
}

public class OrderItem
{
    public string Product { get; set; }
    public int Quantity { get; set; }
}
