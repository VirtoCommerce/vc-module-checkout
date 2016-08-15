using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.CheckoutModule.Data.Converters;
using VirtoCommerce.CheckoutModule.Data.Model;
using VirtoCommerce.CheckoutModule.Data.Reward;
using VirtoCommerce.CheckoutModule.Data.Tax;
using VirtoCommerce.Domain.Cart.Services;
using VirtoCommerce.Domain.Store.Services;
using VirtoCommerce.Domain.Tax.Model;
using VirtoCommerce.Domain.Cart.Model;
using VirtoCommerce.Domain.Marketing.Services;
using VirtoCommerce.Domain.Shipping.Model;
using VirtoCommerce.Domain.Store.Model;
using AnonymousComparer = VirtoCommerce.CheckoutModule.Data.Model.AnonymousComparer;
using EntryState = VirtoCommerce.CheckoutModule.Data.Model.EntryState;
using PromotionEvaluationContext = VirtoCommerce.Domain.Marketing.Model.PromotionEvaluationContext;
using StringExtensions = VirtoCommerce.CheckoutModule.Data.Model.StringExtensions;

namespace VirtoCommerce.CheckoutModule.Data.Builders
{
	public class CartBuilder : ICartBuilder//, IAsyncObserver<UserLoginEvent>
	{
		private readonly IStoreService _storeService;
		private readonly IShoppingCartService _shoppingCartService;
		private readonly IShoppingCartSearchService _shoppingCartSearchService;
		private readonly IMarketingPromoEvaluator _marketingPromoEvaluator;

		private ShoppingCart _cart;

		[CLSCompliant(false)]
		public CartBuilder(IStoreService storeService, IShoppingCartService shoppingShoppingCartService,
			IShoppingCartSearchService shoppingCartSearchService, IMarketingPromoEvaluator marketingPromoEvaluator)
		{
			_storeService = storeService;
			_shoppingCartService = shoppingShoppingCartService;
			_shoppingCartSearchService = shoppingCartSearchService;
			_marketingPromoEvaluator = marketingPromoEvaluator;
		}

		#region ICartBuilder Members

		public ICartBuilder TakeCart(ShoppingCart cart)
		{
			_cart = cart;
			return this;
		}

		public virtual ICartBuilder GetOrCreateNewTransientCart(string storeId, string cartName, string customerId, string customerName, string currency, string languageCode)
		{
			var cart = GetCurrentCart(storeId, customerId, cartName);
			if (cart == null)
			{
				cart = new ShoppingCart()
				{
					Name = cartName,
					Currency = currency,
					CustomerId = customerId,
					CustomerName = customerName,
					StoreId = storeId
				};
			}

			_cart = cart;

			return this;
		}

		public virtual ICartBuilder AddItem(AddItemModel addItemModel)
		{
			if (_cart.Items == null)
			{
				_cart.Items = new List<LineItem>();
			}

			var lineItem = new LineItem()
			{
				Currency = _cart.Currency,
				ProductId = addItemModel.ProductId,
				CatalogId = addItemModel.CatalogId,
				Sku = addItemModel.Sku,
				Name = addItemModel.Name,
				ListPrice = addItemModel.ListPrice,
				SalePrice = addItemModel.SalePrice,
				PlacedPrice = addItemModel.PlacedPrice,
				ExtendedPrice = addItemModel.ExtendedPrice,
				DiscountTotal = addItemModel.DiscountTotal,
				TaxTotal = addItemModel.TaxTotal,
				Quantity = addItemModel.Quantity,
				CreatedDate = DateTime.UtcNow,
			};

			AddLineItem(lineItem);

			EvaluatePromotionsAndTaxes();

			return this;
		}

		public virtual ICartBuilder ChangeItemQuantity(string id, int quantity)
		{
			var lineItem = _cart.Items.FirstOrDefault(i => i.Id == id);
			if (lineItem != null)
			{
				InnerChangeItemQuantity(lineItem, quantity);
				EvaluatePromotionsAndTaxes();
			}
			return this;
		}

		public virtual ICartBuilder ChangeItemQuantity(int lineItemIndex, int quantity)
		{
			var lineItem = _cart.Items.ElementAt(lineItemIndex);
			if (lineItem != null)
			{
				InnerChangeItemQuantity(lineItem, quantity);
				EvaluatePromotionsAndTaxes();
			}
			return this;
		}

		public virtual ICartBuilder ChangeItemsQuantities(int[] quantities)
		{
			for (var i = 0; i < quantities.Length; i++)
			{
				var lineItem = _cart.Items.ElementAt(i);
				if (lineItem != null && quantities[i] > 0)
				{
					InnerChangeItemQuantity(lineItem, quantities[i]);
				}
			}
			EvaluatePromotionsAndTaxes();
			return this;
		}

		public virtual ICartBuilder RemoveItem(string id)
		{
			var lineItem = _cart.Items.FirstOrDefault(i => i.Id == id);
			if (lineItem != null)
			{
				_cart.Items.Remove(lineItem);
				EvaluatePromotionsAndTaxes();
			}
			return this;
		}

		public virtual ICartBuilder Clear()
		{
			_cart.Items.Clear();

			EvaluatePromotionsAndTaxes();

			return this;
		}

		public virtual ICartBuilder AddCoupon(string couponCode)
		{
			_cart.Coupon = couponCode;

			EvaluatePromotionsAndTaxes();

			return this;
		}

		public virtual ICartBuilder RemoveCoupon()
		{
			_cart.Coupon = null;

			EvaluatePromotionsAndTaxes();

			return this;
		}

		public virtual ICartBuilder AddOrUpdateShipment(ShipmentUpdateModel updateModel)
		{
			var changedShipment = updateModel.ToShipmentModel(_cart.Currency);
			foreach (var updateItemModel in updateModel.Items)
			{
				var cartItem = _cart.Items.FirstOrDefault(i => i.Id == updateItemModel.LineItemId);
				if (cartItem != null)
				{
					var shipmentItem = cartItem.ToShipmentItem();
					shipmentItem.Quantity = updateItemModel.Quantity;
					changedShipment.Items.Add(shipmentItem);
				}
			}

			//Temporary support only one shipment in cart
			var shipment = _cart.Shipments.FirstOrDefault();
			if (!string.IsNullOrEmpty(changedShipment.Id))
			{
				shipment = _cart.Shipments.FirstOrDefault(s => s.Id == changedShipment.Id);
				if (shipment == null)
				{
					throw new Exception(string.Format("Shipment {0} not found", changedShipment.Id));
				}
			}

			if (shipment == null)
			{
				shipment = new Shipment()
				{
					Currency = _cart.Currency
				};
				_cart.Shipments.Add(shipment);
			}

			if (changedShipment.DeliveryAddress != null)
			{
				shipment.DeliveryAddress = changedShipment.DeliveryAddress;
			}

			//Update shipment items
			if (changedShipment.Items != null)
			{
				Action<EntryState, ShipmentItem, ShipmentItem> pathAction = (changeState, sourceItem, targetItem) =>
				{
					if (changeState == EntryState.Added)
					{
						var cartLineItem = _cart.Items.FirstOrDefault(i => i.Id == sourceItem.LineItem.Id);
						if (cartLineItem != null)
						{
							var newShipmentItem = cartLineItem.ToShipmentItem();
							newShipmentItem.Quantity = sourceItem.Quantity;
							shipment.Items.Add(newShipmentItem);
						}
					}
					else if (changeState == EntryState.Modified)
					{
						targetItem.Quantity = sourceItem.Quantity;
					}
					else if (changeState == EntryState.Deleted)
					{
						shipment.Items.Remove(sourceItem);
					}
				};

				var shipmentItemComparer = AnonymousComparer.Create((ShipmentItem x) => x.LineItem.Id);
				changedShipment.Items.CompareTo(shipment.Items, shipmentItemComparer, pathAction);
			}

			if (!string.IsNullOrEmpty(changedShipment.ShipmentMethodCode))
			{
				var availableShippingRates = GetAvailableShippingRates();
				var shippingRate = availableShippingRates.FirstOrDefault(sm => (StringExtensions.EqualsInvariant(changedShipment.ShipmentMethodCode, sm.ShippingMethod.Code)) && (StringExtensions.EqualsInvariant(changedShipment.ShipmentMethodOption, sm.OptionName)));
				if (shippingRate == null)
				{
					throw new Exception(string.Format("Unknown shipment method: {0} with option: {1}", changedShipment.ShipmentMethodCode, changedShipment.ShipmentMethodOption));
				}

				shipment.ShipmentMethodCode = shippingRate.ShippingMethod.Code;
				shipment.ShipmentMethodOption = shippingRate.OptionName;
				shipment.ShippingPrice = shippingRate.Rate;
				shipment.TaxType = shippingRate.ShippingMethod.TaxType;
			}

			EvaluatePromotionsAndTaxes();

			return this;
		}

		public virtual ICartBuilder RemoveShipment(string shipmentId)
		{
			var shipment = _cart.Shipments.FirstOrDefault(s => s.Id == shipmentId);
			if (shipment != null)
			{
				_cart.Shipments.Remove(shipment);
			}

			EvaluatePromotionsAndTaxes();

			return this;
		}

		public virtual ICartBuilder AddOrUpdatePayment(PaymentUpdateModel updateModel)
		{
			Payment payment;
			if (!string.IsNullOrEmpty(updateModel.Id))
			{
				payment = _cart.Payments.FirstOrDefault(s => s.Id == updateModel.Id);
				if (payment == null)
				{
					throw new Exception(string.Format("Payment with {0} not found", updateModel.Id));
				}
			}
			else
			{
				payment = new Payment()
				{
					Currency = _cart.Currency
				};
				_cart.Payments.Add(payment);
			}

			if (updateModel.BillingAddress != null)
			{
				payment.BillingAddress = updateModel.BillingAddress;
			}

			if (!string.IsNullOrEmpty(updateModel.PaymentGatewayCode))
			{
				var availablePaymentMethods = GetAvailablePaymentMethods();
				var paymentMethod = availablePaymentMethods.FirstOrDefault(pm => string.Equals(pm.GatewayCode, updateModel.PaymentGatewayCode, StringComparison.InvariantCultureIgnoreCase));
				if (paymentMethod == null)
				{
					throw new Exception("Unknown payment method " + updateModel.PaymentGatewayCode);
				}
				payment.PaymentGatewayCode = paymentMethod.GatewayCode;
			}

			payment.OuterId = updateModel.OuterId;
			payment.Amount = _cart.Total;

			return this;
		}

		public virtual ICartBuilder MergeWithCart(ShoppingCart cart)
		{

			foreach (var lineItem in cart.Items)
			{
				AddLineItem(lineItem);
			}

			_cart.Coupon = cart.Coupon;

			_cart.Shipments.Clear();
			_cart.Shipments = cart.Shipments;

			_cart.Payments.Clear();
			_cart.Payments = cart.Payments;

			EvaluatePromotionsAndTaxes();

			_shoppingCartService.Delete(new[] { cart.Id });
			//_cacheManager.Remove(CartCaheKey, _cartCacheRegion);

			return this;
		}

		public virtual ICartBuilder RemoveCart()
		{
			_shoppingCartService.Delete(new string[] { _cart.Id });
			//_cacheManager.Remove(CartCaheKey, _cartCacheRegion);

			return this;
		}

		//	public virtual async Task<ICartBuilder> FillFromQuoteRequest(QuoteRequest quoteRequest)
		//	{
		//		var productIds = quoteRequest.Items.Select(i => i.ProductId);
		//		var products = await _catalogSearchService.GetProductsAsync(productIds.ToArray(), ItemResponseGroup.ItemLarge);

		//		_cart.Items.Clear();
		//		foreach (var product in products)
		//		{
		//			var quoteItem = quoteRequest.Items.FirstOrDefault(i => i.ProductId == product.Id);
		//			if (quoteItem != null)
		//			{
		//				var lineItem = product.ToLineItem(_cart.Language, (int)quoteItem.SelectedTierPrice.Quantity);
		//				lineItem.ListPrice = quoteItem.ListPrice;
		//				lineItem.SalePrice = quoteItem.SelectedTierPrice.Price;
		//				lineItem.ValidationType = ValidationType.None;

		//				AddLineItem(lineItem);
		//			}
		//		}

		//		if (quoteRequest.RequestShippingQuote)
		//		{
		//			_cart.Shipments.Clear();
		//			var shipment = new Shipment(_cart.Currency);

		//			foreach (var item in _cart.Items)
		//			{
		//				shipment.Items.Add(item.ToShipmentItem());
		//			}

		//			if (quoteRequest.ShippingAddress != null)
		//			{
		//				shipment.DeliveryAddress = quoteRequest.ShippingAddress;
		//			}

		//			if (quoteRequest.ShipmentMethod != null)
		//			{
		//				var availableShippingMethods = await GetAvailableShippingMethodsAsync();
		//				if (availableShippingMethods != null)
		//				{
		//					var availableShippingMethod = availableShippingMethods.FirstOrDefault(sm => sm.ShipmentMethodCode == quoteRequest.ShipmentMethod.ShipmentMethodCode);
		//					if (availableShippingMethod != null)
		//					{
		//						shipment = quoteRequest.ShipmentMethod.ToShipmentModel(_cart.Currency);
		//					}
		//				}
		//			}

		//			_cart.Shipments.Add(shipment);
		//		}

		//		_cart.Payments.Clear();
		//		var payment = new Payment(_cart.Currency);

		//		if (quoteRequest.BillingAddress != null)
		//		{
		//			payment.BillingAddress = quoteRequest.BillingAddress;
		//		}

		//		payment.Amount = quoteRequest.Totals.GrandTotalInclTax;

		//		_cart.Payments.Add(payment);

		//		return this;
		//	}

		public virtual ICollection<ShippingRate> GetAvailableShippingRates()
		{
			var store = _storeService.GetById(_cart.StoreId);

			// TODO: Remake with shipmentId
			var evalContext = new ShippingEvaluationContext(_cart);

			var activeAvailableShippingRates = store.ShippingMethods.Where(x => x.IsActive);

			var availableShippingRates = activeAvailableShippingRates
				.SelectMany(x => x.CalculateRates(evalContext))
				.Where(x => x.ShippingMethod == null || x.ShippingMethod.IsActive).ToArray();

			////Evaluate tax for shipping methods
			//taxEvaluationContext.Lines.AddRange(availableShippingMethods.Select(x => x.ToTaxLine()));
			//var taxResult = await _commerceApi.CommerceEvaluateTaxesAsync(_cart.StoreId, taxEvalContext);
			//if (taxResult != null)
			//{
			//	var taxRates = taxResult.Select(x => x.ToWebModel(_cart.Currency)).ToList();
			//	foreach (var shippingMethod in availableShippingMethods)
			//	{
			//		shippingMethod.ApplyTaxRates(taxRates);
			//	}
			//}

			////Evaluate promotions for shipping methods
			//var promoEvalContext = _cart.ToPromotionEvaluationContext();
			//await _promotionEvaluator.EvaluateDiscountsAsync(promoEvalContext, availableShippingMethods);

			return availableShippingRates;
		}

		public virtual ICollection<PaymentMethod> GetAvailablePaymentMethods()
		{
			var store = _storeService.GetById(_cart.StoreId);

			return store.PaymentMethods.Where(x => x.IsActive).Select(x=>x.ToCartModel()).ToList();
		}

		public virtual ICartBuilder EvaluatePromotions()
		{
			var promotionEvaluationContext = _cart.ToPromotionEvaluationContext();
			var promotionResult = _marketingPromoEvaluator.EvaluatePromotion(promotionEvaluationContext);
			if (promotionResult.Rewards != null)
			{
				//todo:_cart.ApplyRewards(promotionResult.Rewards);
			}

			return this;
		}

		public ICartBuilder EvaluateTax()
		{
			var store = _storeService.GetById(_cart.StoreId);

			var activeTaxProvider = store.TaxProviders.FirstOrDefault(x => x.IsActive);
			if (activeTaxProvider != null)
			{
				var taxEvaluationContext = _cart.ToTaxEvalContext();
				var taxRates = activeTaxProvider.CalculateRates(taxEvaluationContext);
				//todo:_cart.ApplyTaxeRates(taxRates);
			}

			return this;
		}

		public virtual void Save()
		{
			//Invalidate cart in cache
			//_cacheManager.Remove(CartCaheKey, _cartCacheRegion);

			if (_cart.IsTransient())
			{
				_cart = _shoppingCartService.Create(_cart);
			}
			else
			{
				_shoppingCartService.Update(new ShoppingCart[] { _cart });
			}
		}

		public ShoppingCart Cart
		{
			get
			{
				return _cart;
			}
		}

		//	#endregion

		//	#region IObserver<UserLoginEvent> Members
		//	/// <summary>
		//	/// Merger anonymous cart by loging event
		//	/// </summary>
		//	/// <param name="userLoginEvent"></param>
		//	public async Task OnNextAsync(UserLoginEvent userLoginEvent)
		//	{
		//		if (userLoginEvent == null)
		//			return;

		//		var workContext = userLoginEvent.WorkContext;
		//		var prevUser = userLoginEvent.PrevUser;
		//		var prevUserCart = userLoginEvent.WorkContext.CurrentCart;
		//		var newUser = userLoginEvent.NewUser;

		//		//If previous user was anonymous and it has not empty cart need merge anonymous cart to personal
		//		if (!prevUser.IsRegisteredUser && prevUserCart != null && prevUserCart.Items.Any())
		//		{

		//			//we load or create cart for new user
		//			await GetOrCreateNewTransientCartAsync(workContext.CurrentStore, newUser, workContext.CurrentLanguage, workContext.CurrentCurrency);
		//			await MergeWithCartAsync(prevUserCart);
		//			await SaveAsync();
		//		}
		//	}

		#endregion

		private void InnerChangeItemQuantity(LineItem lineItem, int quantity)
		{
			if (lineItem != null)
			{
				//todo: tier price
				//var product = (await _catalogSearchService.GetProductsAsync(new[] { lineItem.ProductId }, ItemResponseGroup.ItemWithPrices)).FirstOrDefault();
				//if (product != null)
				//{
				//	lineItem.SalePrice = product.Price.GetTierPrice(quantity).Price;
				//}
				if (quantity > 0)
				{
					lineItem.Quantity = quantity;
				}
				else
				{
					_cart.Items.Remove(lineItem);
				}
			}
		}

		private void AddLineItem(LineItem lineItem)
		{
			var existingLineItem = _cart.Items.FirstOrDefault(li => li.ProductId == lineItem.ProductId);
			if (existingLineItem != null)
			{
				existingLineItem.Quantity += lineItem.Quantity;
			}
			else
			{
				lineItem.Id = null;
				_cart.Items.Add(lineItem);
			}
		}

		private void EvaluatePromotionsAndTaxes()
		{
			EvaluatePromotions();
			EvaluateTax();
		}

		private ShoppingCart GetCurrentCart(string customerId, string storeId, string cartName)
		{
			var criteria = new Domain.Cart.Model.SearchCriteria
			{
				CustomerId = string.IsNullOrEmpty(customerId) ? "anonymous" : customerId,
				StoreId = storeId,
				Keyword = cartName
			};

			var searchResult = _shoppingCartSearchService.Search(criteria);

			var retVal = searchResult.ShopingCarts.FirstOrDefault(x => !string.IsNullOrEmpty(x.Name) && x.Name.Equals("default", StringComparison.OrdinalIgnoreCase))
						 ?? searchResult.ShopingCarts.FirstOrDefault();

			if (retVal != null)
			{
				retVal = _shoppingCartService.GetById(retVal.Id);
			}

			return retVal;
		}
	}
}

