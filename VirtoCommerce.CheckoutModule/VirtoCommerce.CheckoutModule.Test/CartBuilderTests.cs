using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CacheManager.Core;
using Moq;
using VirtoCommerce.CartModule.Data.Repositories;
using VirtoCommerce.CartModule.Data.Services;
using VirtoCommerce.CheckoutModule.Data.Builders;
using VirtoCommerce.CoreModule.Data.Repositories;
using VirtoCommerce.CoreModule.Data.Services;
using VirtoCommerce.Domain.Cart.Events;
using VirtoCommerce.Domain.Cart.Services;
using VirtoCommerce.Domain.Catalog.Services;
using VirtoCommerce.Domain.Commerce.Services;
using VirtoCommerce.Domain.Marketing.Services;
using VirtoCommerce.Domain.Store.Services;
using VirtoCommerce.MarketingModule.Data.Repositories;
using VirtoCommerce.MarketingModule.Data.Services;
using VirtoCommerce.Platform.Core.DynamicProperties;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.Serialization;
using VirtoCommerce.Platform.Data.DynamicProperties;
using VirtoCommerce.Platform.Data.Infrastructure;
using VirtoCommerce.Platform.Data.Infrastructure.Interceptors;
using VirtoCommerce.Platform.Data.Repositories;
using VirtoCommerce.StoreModule.Data.Repositories;
using VirtoCommerce.StoreModule.Data.Services;
using VirtoCommerce.Platform.Data.Serialization;
using Xunit;

namespace VirtoCommerce.CheckoutModule.Test
{
	public class CartBuilderTests
	{
		[Fact]
		public void GetOrCreateNewTransientCartTest()
		{
			//Assign
			var builder = GetCartBuilder();

			//Act
			builder.GetOrCreateNewTransientCart("Clothing", Guid.NewGuid().ToString(), "CustomerName", "USD", "ENG");
			builder.Save();

			//Assert
			Assert.NotNull(builder.Cart);
		}

		private ICartBuilder GetCartBuilder()
		{
			var storeService = GetStoreService();
			var shoppingCartService = GetShoppingCartService();
			var shoppingCartSearchService = GetShoppingCartSearchService();
			var marketingPromoEvaluator = GetMarketingPromoEvaluator();
			var builder = new CartBuilder(storeService, shoppingCartService, shoppingCartSearchService, marketingPromoEvaluator);

			return builder;
		}

		private ICacheManager<object> GetCacheManager()
		{
			return new Mock<ICacheManager<object>>().Object;
		}

		private IPromotionService GetPromotionService()
		{
			Func<IMarketingRepository> repository = () => new MarketingRepositoryImpl("VirtoCommerce", new EntityPrimaryKeyGeneratorInterceptor(), new AuditableInterceptor(null));

			var promotionExtensionManager = new DefaultMarketingExtensionManagerImpl();
			
			return new PromotionServiceImpl(repository, promotionExtensionManager, GetExpressionSerializer(), GetCacheManager());
		}

		private IExpressionSerializer GetExpressionSerializer()
		{
			return new XmlExpressionSerializer();
		}

		private IMarketingPromoEvaluator GetMarketingPromoEvaluator()
		{
			return new DefaultPromotionEvaluatorImpl(GetPromotionService(), GetCacheManager());
		}

		private IShoppingCartSearchService GetShoppingCartSearchService()
		{
			Func<ICartRepository> repositoryFactory = () => new CartRepositoryImpl("VirtoCommerce", new AuditableInterceptor(null), new EntityPrimaryKeyGeneratorInterceptor());
			return new ShoppingCartSearchServiceImpl(repositoryFactory);
		}

		private IShoppingCartService GetShoppingCartService()
		{
			Func<ICartRepository> repositoryFactory = () => new CartRepositoryImpl("VirtoCommerce", new AuditableInterceptor(null), new EntityPrimaryKeyGeneratorInterceptor());
			return new ShoppingCartServiceImpl(repositoryFactory, new Mock<IEventPublisher<CartChangeEvent>>().Object, new Mock<IItemService>().Object, GetDynamicPropertyService());
		}

		private ICommerceService GetCommerceService()
		{
			Func<IСommerceRepository> repositoryFactory = () => new CommerceRepositoryImpl("VirtoCommerce", new EntityPrimaryKeyGeneratorInterceptor(), new AuditableInterceptor(null));
			return new CommerceServiceImpl(repositoryFactory);
		}

		private IDynamicPropertyService GetDynamicPropertyService()
		{
			Func<IPlatformRepository> platformRepositoryFactory = () => new PlatformRepository("VirtoCommerce", new EntityPrimaryKeyGeneratorInterceptor(), new AuditableInterceptor(null));
			return new DynamicPropertyService(platformRepositoryFactory);
		}

		private IStoreService GetStoreService()
		{
			Func<IStoreRepository> repositoryFactory = () => new StoreRepositoryImpl("VirtoCommerce", new EntityPrimaryKeyGeneratorInterceptor(), new AuditableInterceptor(null));
			return new StoreServiceImpl(repositoryFactory, GetCommerceService(), null, GetDynamicPropertyService(), null, null, null, null);
		}
	}
}
