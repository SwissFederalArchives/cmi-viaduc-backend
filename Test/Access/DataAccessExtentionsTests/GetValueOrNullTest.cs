using CMI.Access.Sql.Viaduc;
using NUnit.Framework;

namespace DataAccessExtentionsTests
{
    [TestFixture]
    public class GetValueOrNullTest
    {
        [Test]
        public void GetValueOrNullTest_LongToInt()
        {
            long longus = 25L;
            
            var result = DataAccessExtensions.GetValueOrNull<int>(longus);
            Assert.IsTrue(result == 25);
        }

        [Test]
        public void GetValueOrNullTest_IntToInt()
        {
            long intus = 25;

            var result = DataAccessExtensions.GetValueOrNull<int>(intus);
            Assert.IsTrue(result == 25);
        }

        [Test]
        public void GetValueOrNullTest_ShortToInt()
        {
            short shortus = 25;

            var result = DataAccessExtensions.GetValueOrNull<int>(shortus);
            Assert.IsTrue(result == 25);
        }

        [Test]
        public void GetValueOrNullTest_IntToShort()
        {
            int intus = 25;

            var result = DataAccessExtensions.GetValueOrNull<short>(intus);
            Assert.IsTrue(result == 25);
        }

        [Test]
        public void GetValueOrNullTest_DoubleToShort()
        {
            double intus = 25;

            var result = DataAccessExtensions.GetValueOrNull<short>(intus);
            Assert.IsTrue(result == 25);
        }

        [Test]
        public void GetValueOrNullTest_Null()
        {
            var result = DataAccessExtensions.GetValueOrNull<short>(null);
            Assert.IsTrue(result == null);
        }

        [Test]
        public void GetValueOrNullTest_ShortToShort()
        {
            short shortus = 25;

            var result = DataAccessExtensions.GetValueOrNull<short>(shortus);
            Assert.IsTrue(result == 25);
        }
    }
}

