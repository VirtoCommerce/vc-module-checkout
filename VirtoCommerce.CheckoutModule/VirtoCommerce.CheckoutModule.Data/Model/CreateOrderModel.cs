using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.Domain.Payment.Model;

namespace VirtoCommerce.CheckoutModule.Data.Model
{
	public class CreateOrderModel
	{
		public CartContext CartContext { get; set; }

		public BankCardInfo BankCardInfo { get; set; }
	}
}
