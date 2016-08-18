namespace VirtoCommerce.CheckoutModule.Data.Model
{
	public class CartContext
	{
		public string StoreId { get; set; }

		public string CartName { get; set; }

		public string CustomerId { get; set; }

		public string CustomerName { get; set; }

		public string Currency { get; set; }

		public string LanguageCode { get; set; }

		public bool IsRegistered { get; set; }
	}
}
