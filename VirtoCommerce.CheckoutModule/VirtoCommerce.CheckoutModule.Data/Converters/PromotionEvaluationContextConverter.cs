using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Omu.ValueInjecter;
using VirtoCommerce.Domain.Cart.Model;
using VirtoCommerce.Domain.Marketing.Model;

namespace VirtoCommerce.CheckoutModule.Data.Converters
{
	public static class PromotionEvaluationContextConverter
	{
		public static PromotionEvaluationContext ToPromotionEvaluationContext(this ShoppingCart cart)
		{
			var promotionItems = cart.Items.Select(i => i.ToPromotionItem()).ToList();

			var retVal = new PromotionEvaluationContext
			{
				CartPromoEntries = promotionItems,
				CartTotal = cart.Total,
				Coupon = cart.Coupon,
				Currency = cart.Currency,
				CustomerId = cart.CustomerId,
				//todo: IsRegisteredUser = cart.Customer.IsRegisteredUser,
				Language = cart.LanguageCode,
				PromoEntries = promotionItems,
				StoreId = cart.StoreId
			};

			return retVal;
		}

		public static ProductPromoEntry ToPromotionItem(this LineItem lineItem)
		{
			var promoItem = new ProductPromoEntry();

			promoItem.InjectFrom(lineItem);

			promoItem.Discount = lineItem.DiscountTotal;
			promoItem.Price = lineItem.PlacedPrice;
			promoItem.Quantity = lineItem.Quantity;
			promoItem.Variations = null; // TODO

			return promoItem;
		}
	}
}
