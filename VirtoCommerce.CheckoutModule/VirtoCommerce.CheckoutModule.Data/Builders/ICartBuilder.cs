using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.CheckoutModule.Data.Model;
using VirtoCommerce.Domain.Cart.Model;
using VirtoCommerce.Domain.Store.Model;
using VirtoCommerce.Domain.Customer.Model;
using VirtoCommerce.Domain.Marketing.Model;
using VirtoCommerce.Domain.Shipping.Model;
using VirtoCommerce.Domain.Tax.Model;

namespace VirtoCommerce.CheckoutModule.Data.Builders
{
	/// <summary>
	/// Represent abstraction for working with customer shopping cart
	/// </summary>
	public interface ICartBuilder
	{
		/// <summary>
		///  Capture passed cart and all next changes will be implemented on it
		/// </summary>
		/// <param name="cart"></param>
		/// <returns></returns>
		ICartBuilder TakeCart(ShoppingCart cart);

		/// <summary>
		/// Load or created new cart for current user and capture it
		/// </summary>
		/// <param name="customerName"></param>
		/// <param name="cartName"></param>
		/// <param name="currency"></param>
		/// <param name="storeId"></param>
		/// <param name="customerId"></param>
		/// <param name="languageCode"></param>
		/// <returns></returns>
		ICartBuilder GetOrCreateNewTransientCart(string storeId, string cartName, string customerId, string customerName, string currency, string languageCode);

		/// <summary>
		/// Add new product to cart
		/// </summary>
		/// <param name="addItemModel"></param>
		/// <returns></returns>
		ICartBuilder AddItem(AddItemModel addItemModel);

		/// <summary>
		/// Change cart item qty by product index
		/// </summary>
		/// <param name="lineItemId"></param>
		/// <param name="quantity"></param>
		/// <returns></returns>
		ICartBuilder ChangeItemQuantity(string lineItemId, int quantity);

		/// <summary>
		/// Change cart item qty by item id
		/// </summary>
		/// <param name="lineItemIndex"></param>
		/// <param name="quantity"></param>
		/// <returns></returns>
		ICartBuilder ChangeItemQuantity(int lineItemIndex, int quantity);

		ICartBuilder ChangeItemsQuantities(int[] quantities);

		/// <summary>
		/// Remove item from cart by id
		/// </summary>
		/// <param name="lineItemId"></param>
		/// <returns></returns>
		ICartBuilder RemoveItem(string lineItemId);

		/// <summary>
		/// Apply marketing coupon to captured cart
		/// </summary>
		/// <param name="couponCode"></param>
		/// <returns></returns>
		ICartBuilder AddCoupon(string couponCode);

		/// <summary>
		/// remove exist coupon from cart
		/// </summary>
		/// <returns></returns>
		ICartBuilder RemoveCoupon();

		/// <summary>
		/// Clear cart remove all items and shipments and payments
		/// </summary>
		/// <returns></returns>
		ICartBuilder Clear();

		/// <summary>
		/// Add or update shipment to cart
		/// </summary>
		/// <param name="updateModel"></param>
		/// <param name="taxEvaluationContext"></param>
		/// <returns></returns>
		ICartBuilder AddOrUpdateShipment(ShipmentUpdateModel updateModel);

		/// <summary>
		/// Remove exist shipment from cart
		/// </summary>
		/// <param name="shipmentId"></param>
		/// <returns></returns>
		ICartBuilder RemoveShipment(string shipmentId);

		/// <summary>
		/// Add or update payment in cart
		/// </summary>
		/// <param name="updateModel"></param>
		/// <returns></returns>
		ICartBuilder AddOrUpdatePayment(PaymentUpdateModel updateModel);

		/// <summary>
		/// Merge other cart with captured
		/// </summary>
		/// <param name="cart"></param>
		/// <returns></returns>
		ICartBuilder MergeWithCart(ShoppingCart cart);

		/// <summary>
		/// Remove cart from service
		/// </summary>
		/// <returns></returns>
		ICartBuilder RemoveCart();

		///// <summary>
		///// Fill current captured cart from RFQ
		///// </summary>
		///// <param name="quoteRequest"></param>
		///// <returns></returns>
		//ICartBuilder> FillFromQuoteRequest(QuoteRequest quoteRequest);

		/// <summary>
		/// Returns all available shipment methods for current cart
		/// </summary>
		/// <returns></returns>
		ICollection<ShippingRate> GetAvailableShippingRates();

		/// <summary>
		/// Returns all available payment methods for current cart
		/// </summary>
		/// <returns></returns>
		ICollection<PaymentMethod> GetAvailablePaymentMethods();

		/// <summary>
		/// Evaluate marketing discounts for captured cart
		/// </summary>
		/// <returns></returns>
		ICartBuilder EvaluatePromotions();

		/// <summary>
		/// Evaluate taxes  for captured cart
		/// </summary>
		/// <returns></returns>
		ICartBuilder EvaluateTax();

		//Save cart changes
		void Save();

		ShoppingCart Cart { get; }
	}
}
