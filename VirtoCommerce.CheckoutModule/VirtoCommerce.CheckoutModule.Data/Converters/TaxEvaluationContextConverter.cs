﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.Domain.Cart.Model;
using VirtoCommerce.Domain.Customer.Model;
using VirtoCommerce.Domain.Tax.Model;

namespace VirtoCommerce.CheckoutModule.Data.Converters
{
	public static class TaxEvaluationContextConverter
	{
		public static TaxEvaluationContext ToTaxEvalContext(this ShoppingCart cart)
		{
			var retVal = new TaxEvaluationContext
			{
				Id = cart.Id,
				Code = cart.Name,
				Currency = cart.Currency,
				Type = "Cart",
				Lines = new List<TaxLine>()
			};

			if (cart.Items != null)
			{
				foreach (var lineItem in cart.Items)
				{
					var extendedTaxLine = new TaxLine
					{
						Id = lineItem.Id + "&extended",
						Code = lineItem.Sku,
						Name = lineItem.Name,
						TaxType = lineItem.TaxType,
						Amount = lineItem.ExtendedPrice
					};
					retVal.Lines.Add(extendedTaxLine);

					var listTaxLine = new TaxLine
					{
						Id = lineItem.Id + "&list",
						Code = lineItem.Sku,
						Name = lineItem.Name,
						TaxType = lineItem.TaxType,
						Amount = lineItem.ListPrice
					};
					retVal.Lines.Add(listTaxLine);

					if (lineItem.ListPrice != lineItem.SalePrice)
					{
						var saleTaxLine = new TaxLine
						{
							Id = lineItem.Id + "&sale",
							Code = lineItem.Sku,
							Name = lineItem.Name,
							TaxType = lineItem.TaxType,
							Amount = lineItem.SalePrice
						};
						retVal.Lines.Add(saleTaxLine);
					}
				}
			}

			if (cart.Shipments != null)
			{
				foreach (var shipment in cart.Shipments)
				{
					var totalTaxLine = new TaxLine
					{
						Id = shipment.Id + "&total",
						Code = shipment.ShipmentMethodCode,
						Name = shipment.ShipmentMethodCode,
						TaxType = shipment.TaxType,
						Amount = shipment.Total
					};
					retVal.Lines.Add(totalTaxLine);
					var priceTaxLine = new TaxLine
					{
						Id = shipment.Id + "&price",
						Code = shipment.ShipmentMethodCode,
						Name = shipment.ShipmentMethodCode,
						TaxType = shipment.TaxType,
						Amount = shipment.ShippingPrice
					};
					retVal.Lines.Add(priceTaxLine);

					if (shipment.DeliveryAddress != null)
					{
						//*** alex fix shipping address & customerId to the taxevalcontext
						retVal.Address = shipment.DeliveryAddress;
						retVal.Address.AddressType = shipment.DeliveryAddress.AddressType;
					}

					retVal.Customer = new Contact
					{
						Id = cart.CustomerId,
						Name = cart.CustomerName
					};
					//*** end alex fix shipping address & customerId to the taxevalcontext
				}
			}

			return retVal;
		}
	}
}
