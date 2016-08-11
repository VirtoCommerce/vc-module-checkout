using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Omu.ValueInjecter;
using VirtoCommerce.CheckoutModule.Data.Model;
using VirtoCommerce.Domain.Cart.Model;

namespace VirtoCommerce.CheckoutModule.Data.Converters
{
	public static class ShipmentConverter
	{
		public static Shipment ToShipmentModel(this ShipmentUpdateModel updateModel, string currency)
		{
			var shipmentModel = new Shipment
			{
				Currency = currency
			};

			shipmentModel.InjectFrom<NullableAndEnumValueInjecter>(updateModel);

			return shipmentModel;
		}
	}
}
