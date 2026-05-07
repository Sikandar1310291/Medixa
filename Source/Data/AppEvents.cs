using System;

namespace PharmaBilling.Source.Data
{
    /// <summary>
    /// Central event bus — any module fires these events and the Dashboard
    /// (and any other subscriber) immediately refreshes its data.
    /// </summary>
    public static class AppEvents
    {
        // Fired when a new Sale is saved OR deleted
        public static event EventHandler SaleDataChanged;

        // Fired when a Purchase is saved OR deleted
        public static event EventHandler PurchaseDataChanged;

        // Fired when stock levels change (sale, purchase, return)
        public static event EventHandler StockChanged;

        public static void OnSaleDataChanged()
        {
            SaleDataChanged?.Invoke(null, EventArgs.Empty);
            StockChanged?.Invoke(null, EventArgs.Empty);
        }

        public static void OnPurchaseDataChanged()
        {
            PurchaseDataChanged?.Invoke(null, EventArgs.Empty);
            StockChanged?.Invoke(null, EventArgs.Empty);
        }

        // Fired directly when a stock batch is edited or deleted from StockUC
        public static void OnStockChanged()
        {
            StockChanged?.Invoke(null, EventArgs.Empty);
        }
    }
}
