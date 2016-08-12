using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.CheckoutModule.Data.Model;
using VirtoCommerce.Domain.Cart.Model;
using VirtoCommerce.Domain.Commerce.Model;
using VirtoCommerce.Domain.Tax.Model;

namespace VirtoCommerce.CheckoutModule.Data.Tax
{
	public static class TaxRateProcessor
	{
		public static void ApplyTaxeRates(this ShoppingCart shoppingCart, IEnumerable<TaxRate> taxRates)
		{
			foreach (var lineItem in shoppingCart.Items)
			{
				lineItem.ApplyTaxRates(taxRates);
				shoppingCart.TaxTotal += lineItem.TaxTotal;
			}

			foreach (var shipment in shoppingCart.Shipments)
			{
				shipment.ApplyTaxRates(taxRates);
				shoppingCart.TaxTotal += shipment.TaxTotal;
			}
		}

		public static void ApplyTaxRates(this Shipment shipment, IEnumerable<TaxRate> taxRates)
		{
			//todo: add ShippingPriceWithTax to shipment
			//shipment.ShippingPriceWithTax = shipment.ShippingPrice;
			
			//Because TaxLine.Id may contains composite string id & extra info
			var shipmentTaxRates = taxRates.Where(x => x.Line.Id.SplitIntoTuple('&').Item1 == shipment.Id).ToList();

			shipment.TaxTotal = 0;

			if (shipmentTaxRates.Any())
			{
				var totalTaxRate = shipmentTaxRates.First(x => x.Line.Id.SplitIntoTuple('&').Item2.EqualsInvariant("total"));
				var priceTaxRate = shipmentTaxRates.First(x => x.Line.Id.SplitIntoTuple('&').Item2.EqualsInvariant("price"));
				shipment.TaxTotal += totalTaxRate.Rate;
				//shipment.ShippingPriceWithTax = shipment.ShippingPrice + priceTaxRate.Rate;
			}
		}

		public static void ApplyTaxRates(this LineItem lineItem, IEnumerable<TaxRate> taxRates)
		{
			// todo: add ListPriceWithTax and SalePriceWithTax to lineItem
			//lineItem.ListPriceWithTax = lineItem.ListPrice;
			//lineItem.SalePriceWithTax = lineItem.SalePrice;

			//Because TaxLine.Id may contains composite string id & extra info
			var lineItemTaxRates = taxRates.Where(x => x.Line.Id.SplitIntoTuple('&').Item1 == lineItem.Id).ToList();

			lineItem.TaxTotal = 0;
			
			if (lineItemTaxRates.Any())
			{
				var extendedPriceRate = lineItemTaxRates.First(x => x.Line.Id.SplitIntoTuple('&').Item2.EqualsInvariant("extended"));
				var listPriceRate = lineItemTaxRates.First(x => x.Line.Id.SplitIntoTuple('&').Item2.EqualsInvariant("list"));
				var salePriceRate = lineItemTaxRates.FirstOrDefault(x => x.Line.Id.SplitIntoTuple('&').Item2.EqualsInvariant("sale"));
				if (salePriceRate == null)
				{
					salePriceRate = listPriceRate;
				}
				lineItem.TaxTotal += extendedPriceRate.Rate;
				//lineItem.ListPriceWithTax = lineItem.ListPrice + listPriceRate.Rate;
				//lineItem.SalePriceWithTax = lineItem.SalePrice + salePriceRate.Rate;
			}
		}
	}
}
