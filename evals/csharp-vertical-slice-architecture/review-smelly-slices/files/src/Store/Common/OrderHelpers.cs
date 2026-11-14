using Store.Domain;

namespace Store.Common;

public static class OrderHelpers
{
    public static decimal CalculateRefund(Order order)
    {
        if (order.Status == "Placed")
        {
            return order.Total;
        }

        if (order.Status == "Packed")
        {
            return order.Total * 0.9m;
        }

        return 0m;
    }

    public static bool CanCancel(Order order) => order.Status is "Placed" or "Packed";
}
