using System;
using CMI.Contract.Order;
using Shouldly;
using NUnit.Framework;

namespace CMI.Manager.Order.Tests
{
    [TestFixture]
    public class OrderStateTests
    {
        [Test]
        public void For_every_OrderStateInternal_there_should_be_a_corresponding_AuftragStatus()
        {
            foreach (OrderStatesInternal osi in Enum.GetValues(typeof(OrderStatesInternal)))
            {
                var status = AuftragStatusRepo.GetStatus(osi);
                status.ShouldNotBeNull("every OrderStatesInternal must have a corresponding AuftragStatus.");
                status.OrderStateInternal.ShouldBe(osi, "the status code must match.");
            }
        }
    }
}