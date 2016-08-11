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

			var cartRewards = rewards.Where(r => r.GetType().Name == PromotionRewardType.CartSubtotalReward.ToString());
			//foreach (var reward in cartRewards)
			//{
			//	var discount = reward.ToDiscountModel(SubTotal, SubTotalWithTax);

			//	if (reward.IsValid)
			//	{
			//		Discounts.Add(discount);
			//	}
			//}

			//var lineItemRewards = rewards.Where(r => r.RewardType == PromotionRewardType.CatalogItemAmountReward);
			//foreach (var lineItem in Items)
			//{
			//	lineItem.ApplyRewards(lineItemRewards);
			//}

			//var shipmentRewards = rewards.Where(r => r.RewardType == PromotionRewardType.ShipmentReward);
			//foreach (var shipment in Shipments)
			//{
			//	shipment.ApplyRewards(shipmentRewards);
			//}

			//if (Coupon != null && !string.IsNullOrEmpty(Coupon.Code))
			//{
			//	var couponRewards = rewards.Where(r => r.Promotion.Coupons != null && r.Promotion.Coupons.Any());
			//	if (!couponRewards.Any())
			//	{
			//		Coupon.AppliedSuccessfully = false;
			//		Coupon.ErrorCode = "InvalidCouponCode";
			//	}
			//	foreach (var reward in couponRewards)
			//	{
			//		var couponCode = reward.Promotion.Coupons.FirstOrDefault(c => c == Coupon.Code);
			//		if (!string.IsNullOrEmpty(couponCode))
			//		{
			//			Coupon.AppliedSuccessfully = reward.IsValid;
			//			Coupon.Description = reward.Promotion.Description;
			//		}
			//	}
			//}
		}
	}
}
