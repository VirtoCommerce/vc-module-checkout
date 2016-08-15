using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using VirtoCommerce.CheckoutModule.Data.Builders;
using VirtoCommerce.CheckoutModule.Data.Model;
using VirtoCommerce.Domain.Cart.Model;
using VirtoCommerce.Domain.Marketing.Model;
using VirtoCommerce.Domain.Shipping.Model;
using VirtoCommerce.Domain.Tax.Model;

namespace VirtoCommerce.CheckoutModule.Web.Controllers
{
	public class CheckoutController : ApiController
	{
		private readonly ICartBuilder _cartBuilder;
		private readonly ICartValidator _cartValidator;

		public CheckoutController(ICartBuilder cartBuilder, ICartValidator cartValidator)
		{
			_cartBuilder = cartBuilder;
			_cartValidator = cartValidator;
		}

		[HttpGet]
		[ResponseType(typeof(ShoppingCart))]
		public IHttpActionResult GetCart(string storeId, string cartName, string customerId, string customerName, string currency, string languageCode)
		{
			_cartBuilder.GetOrCreateNewTransientCart(storeId, cartName, customerId, customerName, currency, languageCode);

			_cartBuilder.EvaluateTax();
			_cartBuilder.EvaluatePromotions();
			_cartValidator.Validate(_cartBuilder.Cart);

			return Ok(_cartBuilder.Cart);
		}

		[HttpGet]
		[ResponseType(typeof(int))]
		public IHttpActionResult GetCartItemsCount(string storeId, string cartName, string customerId, string customerName, string currency, string languageCode)
		{
			_cartBuilder.GetOrCreateNewTransientCart(storeId, cartName, customerId, customerName, currency, languageCode);

			return Ok(_cartBuilder.Cart.Items.Count);
		}

		[HttpPost]
		[ResponseType(typeof(int))]
		public async Task<IHttpActionResult> AddItemToCart(string storeId, string cartName, string customerId, string customerName, string currency, string languageCode, AddItemModel addItemModel)
		{
			_cartBuilder.GetOrCreateNewTransientCart(storeId, cartName, customerId, customerName, currency, languageCode);

			using (await AsyncLock.GetLockByKey(GetAsyncLockCartKey(_cartBuilder.Cart.Id)).LockAsync())
			{
				//todo: var products = _catalogSearchService.GetProducts(new[] { id }, Model.Catalog.ItemResponseGroup.ItemLarge);
				//if (products != null && products.Any())
				//{
				//	_cartBuilder.AddItem(products.First(), quantity);
				//	_cartBuilder.Save();
				//}

				_cartBuilder.AddItem(addItemModel).Save();
			}

			return Ok(_cartBuilder.Cart.Items.Count);
		}

		[HttpPut]
		[ResponseType(typeof(void))]
		public async Task<IHttpActionResult> ChangeCartItem(string storeId, string cartName, string customerId, string customerName, string currency, string languageCode, string lineItemId, int quantity)
		{
			_cartBuilder.GetOrCreateNewTransientCart(storeId, cartName, customerId, customerName, currency, languageCode);

			using (await AsyncLock.GetLockByKey(GetAsyncLockCartKey(_cartBuilder.Cart.Id)).LockAsync())
			{
				var lineItem = _cartBuilder.Cart.Items.FirstOrDefault(i => i.Id == lineItemId);
				if (lineItem != null)
				{
					_cartBuilder.ChangeItemQuantity(lineItemId, quantity);
					_cartBuilder.Save();
				}
			}
			return Ok();
		}

		[HttpDelete]
		[ResponseType(typeof(int))]
		public async Task<IHttpActionResult> RemoveCartItem(string storeId, string cartName, string customerId, string customerName, string currency, string languageCode, string lineItemId)
		{
			_cartBuilder.GetOrCreateNewTransientCart(storeId, cartName, customerId, customerName, currency, languageCode);

			using (await AsyncLock.GetLockByKey(GetAsyncLockCartKey(_cartBuilder.Cart.Id)).LockAsync())
			{
				_cartBuilder.RemoveItem(lineItemId);
				_cartBuilder.Save();
			}

			return Ok(_cartBuilder.Cart.Items.Count);
		}

		[HttpPost]
		[ResponseType(typeof(void))]
		public async Task<IHttpActionResult> ClearCart(string storeId, string cartName, string customerId, string customerName, string currency, string languageCode)
		{
			_cartBuilder.GetOrCreateNewTransientCart(storeId, cartName, customerId, customerName, currency, languageCode);

			using (await AsyncLock.GetLockByKey(GetAsyncLockCartKey(_cartBuilder.Cart.Id)).LockAsync())
			{
				_cartBuilder.Clear();
				_cartBuilder.Save();
			}

			return Ok();
		}

		[HttpGet]
		[ResponseType(typeof(ICollection<ShippingRate>))]
		public IHttpActionResult GetCartShipmentAvailShippingRates(string storeId, string cartName, string customerId, string customerName, string currency, string languageCode, string shipmentId)
		{
			_cartBuilder.GetOrCreateNewTransientCart(storeId, cartName, customerId, customerName, currency, languageCode);

			var shippingMethods = _cartBuilder.GetAvailableShippingRates();

			return Ok(shippingMethods);
		}

		[HttpGet]
		[ResponseType(typeof(ICollection<PaymentMethod>))]
		public IHttpActionResult GetCartAvailPaymentMethods(string storeId, string cartName, string customerId, string customerName, string currency, string languageCode)
		{
			_cartBuilder.GetOrCreateNewTransientCart(storeId, cartName, customerId, customerName, currency, languageCode);

			var paymentMethods = _cartBuilder.GetAvailablePaymentMethods();

			return Ok(paymentMethods);
		}

		[HttpPost]
		[ResponseType(typeof(string))]
		public async Task<IHttpActionResult> AddCartCoupon(string storeId, string cartName, string customerId, string customerName, string currency, string languageCode, string couponCode)
		{
			_cartBuilder.GetOrCreateNewTransientCart(storeId, cartName, customerId, customerName, currency, languageCode);

			using (await AsyncLock.GetLockByKey(GetAsyncLockCartKey(_cartBuilder.Cart.Id)).LockAsync())
			{
				_cartBuilder.AddCoupon(couponCode);
				_cartBuilder.Save();
			}

			return Ok(_cartBuilder.Cart.Coupon);
		}

		[HttpDelete]
		[ResponseType(typeof(void))]
		public async Task<IHttpActionResult> RemoveCartCoupon(string storeId, string cartName, string customerId, string customerName, string currency, string languageCode)
		{
			_cartBuilder.GetOrCreateNewTransientCart(storeId, cartName, customerId, customerName, currency, languageCode);

			using (await AsyncLock.GetLockByKey(GetAsyncLockCartKey(_cartBuilder.Cart.Id)).LockAsync())
			{
				_cartBuilder.RemoveCoupon();
				_cartBuilder.Save();
			}

			return Ok();
		}

		[HttpPost]
		[ResponseType(typeof(void))]
		public async Task<IHttpActionResult> AddOrUpdateCartShipment(string storeId, string cartName, string customerId, string customerName, string currency, string languageCode, ShipmentUpdateModel shipment)
		{
			_cartBuilder.GetOrCreateNewTransientCart(storeId, cartName, customerId, customerName, currency, languageCode);

			using (await AsyncLock.GetLockByKey(GetAsyncLockCartKey(_cartBuilder.Cart.Id)).LockAsync())
			{
				_cartBuilder.AddOrUpdateShipment(shipment);
				_cartBuilder.Save();
			}

			return Ok();
		}

		[HttpPost]
		[ResponseType(typeof(void))]
		public async Task<IHttpActionResult> AddOrUpdateCartPayment(string storeId, string cartName, string customerId, string customerName, string currency, string languageCode, PaymentUpdateModel payment)
		{
			_cartBuilder.GetOrCreateNewTransientCart(storeId, cartName, customerId, customerName, currency, languageCode);

			using (await AsyncLock.GetLockByKey(GetAsyncLockCartKey(_cartBuilder.Cart.Id)).LockAsync())
			{
				_cartBuilder.AddOrUpdatePayment(payment);
				_cartBuilder.Save();
			}

			return Ok();
		}

		//[HttpPost]
		//[ResponseType(typeof(int))]
		//public Task<IHttpActionResult> CreateOrder(string storeId, string customerId, string customerName, string currency, string languageCode, OrderModule.Client.Model.BankCardInfo bankCardInfo)
		//{
		//	_cartBuilder.GetOrCreateNewTransientCart(storeId, customerId, customerName, currency, languageCode);

		//	//todo: move this to builder
		//	// using (await AsyncLock.GetLockByKey(GetAsyncLockCartKey(_cartBuilder.Cart.Id)).LockAsync())
		//	//{
		//	//	var order = _orderApi.OrderModuleCreateOrderFromCart(_cartBuilder.Cart.Id);

		//	//	//Raise domain event
		//	//	_orderPlacedEventPublisher.Publish(new OrderPlacedEvent(order.ToWebModel(WorkContext.AllCurrencies, WorkContext.CurrentLanguage), _cartBuilder.Cart));

		//	//	_cartBuilder.RemoveCart();

		//	//	OrderModule.Client.Model.ProcessPaymentResult processingResult = null;
		//	//	var incomingPayment = order.InPayments != null ? order.InPayments.FirstOrDefault() : null;
		//	//	if (incomingPayment != null)
		//	//	{
		//	//		processingResult = _orderApi.OrderModuleProcessOrderPayments(order.Id, incomingPayment.Id, bankCardInfo);
		//	//	}

		//	//	return Ok(new { order, orderProcessingResult = processingResult });
		//	//}
		//}

		private static string GetAsyncLockCartKey(string cartId)
		{
			return "Cart:" + cartId;
		}
	}
}