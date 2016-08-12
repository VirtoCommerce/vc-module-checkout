using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.Domain.Cart.Model;
using VirtoCommerce.Domain.Marketing.Model;

namespace VirtoCommerce.CheckoutModule.Data.Reward
{
	public static class CartRewardProcessor
	{
		public static void ApplyRewards(this ShoppingCart shoppingCart, IEnumerable<PromotionReward> rewards)
		{
			shoppingCart.Discounts.Clear();

			var cartRewards = rewards.OfType<CartSubtotalReward>().Where(x => x.IsValid).ToList();
			foreach (var reward in cartRewards)
			{
				//todo: CartSubtotalReward should be derived from AmountBasedReward
				//var discount = reward.ToDiscountModel(shoppingCart.SubTotal, shoppingCart.SubTotal + shoppingCart.TaxTotal);
				//shoppingCart.Discounts.Add(discount);
			}

			var lineItemRewards = rewards.OfType<CatalogItemAmountReward>().Where(x => x.IsValid).ToList();
			foreach (var lineItem in shoppingCart.Items)
			{
				lineItem.ApplyRewards(lineItemRewards);
			}

			var shipmentRewards = rewards.OfType<ShipmentReward>().Where(x => x.IsValid).ToList();
			foreach (var shipment in shoppingCart.Shipments)
			{
				shipment.ApplyRewards(shipmentRewards);
			}

			if (!string.IsNullOrEmpty(shoppingCart.Coupon))
			{
				var couponRewards = rewards.Where(r => r.Promotion.Coupons != null && r.Promotion.Coupons.Any());

				//if (!couponRewards.Any())
				//{
				//	Coupon.AppliedSuccessfully = false;
				//	Coupon.ErrorCode = "InvalidCouponCode";
				//}

				foreach (var reward in couponRewards)
				{
					var couponCode = reward.Promotion.Coupons.FirstOrDefault(c => c == shoppingCart.Coupon);
					if (!string.IsNullOrEmpty(couponCode))
					{
						//Coupon.AppliedSuccessfully = reward.IsValid;
						//Coupon.Description = reward.Promotion.Description;
					}
				}
			}
		}

		public static Discount ToDiscountModel(this AmountBasedReward amountBasedReward, string currency, decimal amount, decimal amountWithTaxes)
		{
			var discount = new Discount
			{
				Currency = currency,
				DiscountAmount = GetAbsoluteDiscountAmount(amountBasedReward, amount),
				//todo: discount model should have DiscountWithTaxAmount property
				//DiscountWithTaxAmount = GetAbsoluteDiscountAmount(amountBasedReward, withTaxAmount),
				Description = amountBasedReward.Promotion.Description,
				PromotionId = amountBasedReward.Promotion.Id
			};

			return discount;
		}

		public static void ApplyRewards(this Shipment shipment, IEnumerable<ShipmentReward> shipmentRewards)
		{
			var rewards = shipmentRewards.Where(r => string.IsNullOrEmpty(r.ShippingMethod) || string.Equals(r.ShippingMethod, shipment.ShipmentMethodCode, StringComparison.InvariantCultureIgnoreCase));

			shipment.Discounts.Clear();

			foreach (var reward in rewards)
			{
				var discount = reward.ToDiscountModel(shipment.Currency, shipment.ShippingPrice, shipment.ShippingPrice + shipment.TaxTotal);
				if (reward.IsValid)
				{
					shipment.Discounts.Add(discount);
				}
			}
		}

		public static void ApplyRewards(this LineItem lineItem, IEnumerable<CatalogItemAmountReward> catalogItemAmountRewards)
		{
			var lineItemRewards = catalogItemAmountRewards.Where(r => string.IsNullOrEmpty(r.ProductId) || string.Equals(r.ProductId, lineItem.ProductId, StringComparison.OrdinalIgnoreCase));

			lineItem.Discounts.Clear();

			foreach (var reward in lineItemRewards)
			{
				var discount = reward.ToDiscountModel(lineItem.Currency, lineItem.SalePrice, lineItem.SalePrice + lineItem.TaxTotal);
				if (reward.Quantity > 0)
				{
					var money = discount.DiscountAmount * Math.Min(reward.Quantity, lineItem.Quantity);
					//var withTaxMoney = discount.AmountWithTax * Math.Min(reward.Quantity, Quantity);
					//TODO: need allocate more rightly between each quantities
					//discount.DiscountAmount = money.Allocate(Quantity).FirstOrDefault();
					discount.DiscountAmount = money;
					//discount.AmountWithTax = withTaxMoney.Allocate(Quantity).FirstOrDefault();
				}
				if (reward.IsValid)
				{
					lineItem.Discounts.Add(discount);
				}
			}
		}

		public static decimal GetAbsoluteDiscountAmount(AmountBasedReward amountBasedReward, decimal originalAmount)
		{
			var absoluteAmount = amountBasedReward.Amount;

			if (amountBasedReward.AmountType == RewardAmountType.Relative)
			{
				absoluteAmount = amountBasedReward.Amount * originalAmount / 100;
			}

			return absoluteAmount;
		}
	}
}
