using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Open.Disposable
{
	[TestClass]
	public class ObjectPoolSmokeTests
	{
		class IdContainer
		{
			public int ID;
		}

		[TestMethod]
		public void OptimisticArrayObjectPool_FactoryTest()
		{
			int i = 0;
			var pool = OptimisticArrayObjectPool.Create(() => new IdContainer { ID = ++i });
			Assert.AreEqual(1, pool.Take().ID);
		}

	}
}
