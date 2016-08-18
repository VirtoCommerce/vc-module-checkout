using System;
using System.Linq;
using VirtoCommerce.Platform.Core.Modularity;
using Microsoft.Practices.Unity;
using VirtoCommerce.CheckoutModule.Data.Builders;

namespace VirtoCommerce.CheckoutModule.Web
{
	public class Module : ModuleBase
	{
		private readonly IUnityContainer _container;

		public Module(IUnityContainer container)
		{
			_container = container;
		}

		#region IModule Members

		public override void SetupDatabase()
		{
		}

		public override void Initialize()
		{
			_container.RegisterType<ICartValidator, CartValidator>();
			_container.RegisterType<ICartBuilder, CartBuilder>();
		}

		#endregion
	}
}