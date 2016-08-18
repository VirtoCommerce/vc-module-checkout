using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Hosting;
using System.Web.Http;
using System.Web.Http.Description;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VirtoCommerce.CheckoutModule.Data.Builders;
using VirtoCommerce.CheckoutModule.Data.Model;
using VirtoCommerce.Domain.Cart.Model;
using VirtoCommerce.Domain.Marketing.Model;
using VirtoCommerce.Domain.Order.Services;
using VirtoCommerce.Domain.Payment.Model;
using VirtoCommerce.Domain.Shipping.Model;
using VirtoCommerce.Domain.Tax.Model;

namespace VirtoCommerce.CheckoutModule.Web.Controllers
{
	[RoutePrefix("api/checkout")]
	public class CheckoutController : ApiController
	{
		private readonly ICartBuilder _cartBuilder;
		private readonly ICartValidator _cartValidator;
		private readonly ICustomerOrderService _customerOrderService;

		public CheckoutController(ICartBuilder cartBuilder, ICartValidator cartValidator, ICustomerOrderService customerOrderService)
		{
			_cartBuilder = cartBuilder;
			_cartValidator = cartValidator;
			_customerOrderService = customerOrderService;
		}

		[HttpPost]
		[Route("carts/current")]
		[ResponseType(typeof(ShoppingCart))]
		public IHttpActionResult GetCart(CartContext cartContext)
		{
			_cartBuilder.GetOrCreateNewTransientCart(cartContext);

			_cartBuilder.EvaluateTax();
			_cartBuilder.EvaluatePromotions();
			_cartValidator.Validate(_cartBuilder.Cart);

			return Ok(_cartBuilder.Cart);
		}

		[HttpGet]
		[Route("cart/itemscount")]
		[ResponseType(typeof(int))]
		public IHttpActionResult GetCartItemsCount(CartContext cartContext)
		{
			_cartBuilder.GetOrCreateNewTransientCart(cartContext);

			return Ok(_cartBuilder.Cart.Items.Count);
		}

		[HttpPost]
		[Route("cart/items")]
		[ResponseType(typeof(int))]
		public async Task<IHttpActionResult> AddItemToCart(AddItemModel addItemModel)
		{
			_cartBuilder.GetOrCreateNewTransientCart(addItemModel.CartContext);

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
		[Route("cart/items")]
		[ResponseType(typeof(void))]
		public async Task<IHttpActionResult> ChangeCartItem(CartContext cartContext, string lineItemId, int quantity)
		{
			_cartBuilder.GetOrCreateNewTransientCart(cartContext);

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
		[Route("cart/items")]
		[ResponseType(typeof(int))]
		public async Task<IHttpActionResult> RemoveCartItem(CartContext cartContext, string lineItemId)
		{
			_cartBuilder.GetOrCreateNewTransientCart(cartContext);

			using (await AsyncLock.GetLockByKey(GetAsyncLockCartKey(_cartBuilder.Cart.Id)).LockAsync())
			{
				_cartBuilder.RemoveItem(lineItemId);
				_cartBuilder.Save();
			}

			return Ok(_cartBuilder.Cart.Items.Count);
		}

		[HttpPost]
		[Route("cart/clear")]
		[ResponseType(typeof(void))]
		public async Task<IHttpActionResult> ClearCart(CartContext cartContext)
		{
			_cartBuilder.GetOrCreateNewTransientCart(cartContext);

			using (await AsyncLock.GetLockByKey(GetAsyncLockCartKey(_cartBuilder.Cart.Id)).LockAsync())
			{
				_cartBuilder.Clear();
				_cartBuilder.Save();
			}

			return Ok();
		}

		[HttpPost]
		[Route("cart/shipments/{shipmentId}/shippingmethods")]
		[ResponseType(typeof(ICollection<ShippingRate>))]
		public IHttpActionResult GetCartShipmentAvailShippingRates(CartContext cartContext, string shipmentId)
		{
			_cartBuilder.GetOrCreateNewTransientCart(cartContext);

			var shippingMethods = _cartBuilder.GetAvailableShippingRates();

			return Ok(shippingMethods);
		}

		[HttpPost]
		[Route("cart/paymentmethods")]
		[ResponseType(typeof(ICollection<Domain.Payment.Model.PaymentMethod>))]
		public IHttpActionResult GetCartAvailPaymentMethods(CartContext cartContext)
		{
			_cartBuilder.GetOrCreateNewTransientCart(cartContext);

			var paymentMethods = _cartBuilder.GetAvailablePaymentMethods();

			return Ok(paymentMethods);
		}

		[HttpPost]
		[Route("cart/coupons")]
		[ResponseType(typeof(string))]
		public async Task<IHttpActionResult> AddCartCoupon(CartContext cartContext, string couponCode)
		{
			_cartBuilder.GetOrCreateNewTransientCart(cartContext);

			using (await AsyncLock.GetLockByKey(GetAsyncLockCartKey(_cartBuilder.Cart.Id)).LockAsync())
			{
				_cartBuilder.AddCoupon(couponCode);
				_cartBuilder.Save();
			}

			return Ok(_cartBuilder.Cart.Coupon);
		}

		[HttpDelete]
		[Route("cart/coupons")]
		[ResponseType(typeof(void))]
		public async Task<IHttpActionResult> RemoveCartCoupon(CartContext cartContext)
		{
			_cartBuilder.GetOrCreateNewTransientCart(cartContext);

			using (await AsyncLock.GetLockByKey(GetAsyncLockCartKey(_cartBuilder.Cart.Id)).LockAsync())
			{
				_cartBuilder.RemoveCoupon();
				_cartBuilder.Save();
			}

			return Ok();
		}

		[HttpPost]
		[Route("cart/shipments")]
		[ResponseType(typeof(void))]
		public async Task<IHttpActionResult> AddOrUpdateCartShipment(ShipmentUpdateModel shipmentUpdateModel)
		{
			_cartBuilder.GetOrCreateNewTransientCart(shipmentUpdateModel.CartContext);

			using (await AsyncLock.GetLockByKey(GetAsyncLockCartKey(_cartBuilder.Cart.Id)).LockAsync())
			{
				_cartBuilder.AddOrUpdateShipment(shipmentUpdateModel).Save();
			}

			return Ok();
		}

		[HttpPost]
		[Route("cart/payments")]
		[ResponseType(typeof(void))]
		public async Task<IHttpActionResult> AddOrUpdateCartPayment(PaymentUpdateModel paymentUpdateModel)
		{
			_cartBuilder.GetOrCreateNewTransientCart(paymentUpdateModel.CartContext);

			using (await AsyncLock.GetLockByKey(GetAsyncLockCartKey(_cartBuilder.Cart.Id)).LockAsync())
			{
				_cartBuilder.AddOrUpdatePayment(paymentUpdateModel).Save();
			}

			return Ok();
		}

		[HttpPost]
		[Route("cart/createorder")]
		[ResponseType(typeof(CreateOrderResult))]
		public async Task<IHttpActionResult> CreateOrder(CreateOrderModel createOrderModel)
		{
			_cartBuilder.GetOrCreateNewTransientCart(createOrderModel.CartContext);

			using (await AsyncLock.GetLockByKey(GetAsyncLockCartKey(_cartBuilder.Cart.Id)).LockAsync())
			{
				var order = _customerOrderService.CreateByShoppingCart(_cartBuilder.Cart.Id);

				//todo: Raise domain event
				//_orderPlacedEventPublisher.Publish(new OrderPlacedEvent(order.ToWebModel(WorkContext.AllCurrencies, WorkContext.CurrentLanguage), _cartBuilder.Cart));

				_cartBuilder.RemoveCart();

				var result = new CreateOrderResult()
				{
					Order = order
				};

				var incomingPayment = order.InPayments?.FirstOrDefault();
				if (incomingPayment != null)
				{
					var paymentMethods = _cartBuilder.GetAvailablePaymentMethods();
					var paymentMethod = paymentMethods.FirstOrDefault(x => x.Code == incomingPayment.GatewayCode);
					if (paymentMethod == null)
					{
						return BadRequest("An appropriate paymentMethod is not found.");
					}

					result.PaymentMethodType = paymentMethod.PaymentMethodType;

					var context = new ProcessPaymentEvaluationContext
					{
						Order = order,
						Payment = incomingPayment,
						Store = _cartBuilder.Store,
						BankCardInfo = createOrderModel.BankCardInfo
					};
					result.ProcessPaymentResult = paymentMethod.ProcessPayment(context);

					_customerOrderService.Update(new[] { order });
				}

				return Ok(result);
			}
		}

		[HttpGet]
		[Route("countries")]
		[ResponseType(typeof(Country[]))]
		public IHttpActionResult GetCountries()
		{
			return Ok(GetAllCounries());
		}

		[HttpGet]
		[Route("countries/{countryCode}/regions")]
		[ResponseType(typeof(CountryRegion[]))]
		public IHttpActionResult GetCountryRegions(string countryCode)
		{
			var country = GetAllCounries().FirstOrDefault(c => c.Code2.Equals(countryCode, StringComparison.InvariantCultureIgnoreCase) || c.Code3.Equals(countryCode, StringComparison.InvariantCultureIgnoreCase));
			if (country != null)
			{
				return Ok(country.Regions);
			}
			return Ok();
		}

		private static Country[] GetAllCounries()
		{
			var regions = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
				.Select(GetRegionInfo)
				.Where(r => r != null)
				.ToList();

			var countriesJson = File.ReadAllText(HostingEnvironment.MapPath("~/Modules/VirtoCommerce.Checkout/countries.json"));
			var countriesDict = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(countriesJson);

			var countries = countriesDict
				.Select(kvp => ParseCountry(kvp, regions))
				.Where(c => c.Code3 != null)
				.ToArray();

			return countries;
		}

		private static Country ParseCountry(KeyValuePair<string, JObject> pair, List<RegionInfo> regions)
		{
			var region = regions.FirstOrDefault(r => string.Equals(r.EnglishName, pair.Key, StringComparison.OrdinalIgnoreCase));

			var country = new Country
			{
				Name = pair.Key,
				Code2 = region != null ? region.TwoLetterISORegionName : string.Empty,
				Code3 = region != null ? region.ThreeLetterISORegionName : string.Empty,
				RegionType = pair.Value["label"] != null ? pair.Value["label"].ToString() : null
			};

			var provinceCodes = pair.Value["province_codes"].ToObject<Dictionary<string, string>>();
			if (provinceCodes != null && provinceCodes.Any())
			{
				country.Regions = provinceCodes
					.Select(kvp => new CountryRegion { Name = kvp.Key, Code = kvp.Value })
					.ToArray();
			}

			return country;
		}

		private static RegionInfo GetRegionInfo(CultureInfo culture)
		{
			RegionInfo result = null;

			try
			{
				result = new RegionInfo(culture.LCID);
			}
			catch
			{
				// ignored
			}

			return result;
		}

		private static string GetAsyncLockCartKey(string cartId)
		{
			return "Cart:" + cartId;
		}
	}
}