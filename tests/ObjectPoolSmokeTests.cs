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
			var i = 0;
			var pool = OptimisticArrayObjectPool.Create(() => new IdContainer { ID = ++i });
			Assert.AreEqual(1, pool.Take().ID);
		}

		[TestMethod]
		public void ListPool_RecycleTest()
		{
			var pool = ListPool<int>.Shared;
			var list = pool.Take();
			list.Add(1);
			pool.Give(list);
			Assert.AreEqual(list, pool.Take());
			Assert.AreEqual(0, list.Count);
		}

	}
}
