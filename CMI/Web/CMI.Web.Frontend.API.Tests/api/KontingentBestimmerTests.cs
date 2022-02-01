using System;
using System.Collections.Generic;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Contract.Order;
using CMI.Web.Frontend.api;
using CMI.Web.Frontend.ParameterSettings;
using FluentAssertions;
using NUnit.Framework;

namespace CMI.Web.Frontend.API.Tests.api
{
    [TestFixture]
    public class KontingentBestimmerTests
    {
        [Test]
        public void BestimmeKontingent_Von_Oe1_Should_Throw_NotImplementedException()
        {
            // arrange
            var setting = new DigitalisierungsbeschraenkungSettings();
            var orderings = new List<Ordering>();
            var sut = new KontingentBestimmer(setting);

            // act
            var action = new Action(() =>
            {
                sut.BestimmeKontingent(orderings,
                    new User
                    {
                        RolePublicClient = AccessRoles.RoleOe1,
                        Access = new UserAccess("1", AccessRoles.RoleOe1, "oe1", null, false)
                    });
            });

            // assert
            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void BestimmeKontingent_Digitalisierungsbeschraekung_Should_Come_From_Settings()
        {
            // arrange
            var setting = new DigitalisierungsbeschraenkungSettings {DigitalisierungsbeschraenkungOe2 = 2};

            var orderings = new List<Ordering>();
            var sut = new KontingentBestimmer(setting);

            // act
            var result = sut.BestimmeKontingent(orderings, CreateOe2User());

            // assert
            result.Digitalisierungesbeschraenkung.Should().Be(setting.DigitalisierungsbeschraenkungOe2);
        }



        [Test]
        public void BestimmeKontingent_AktiveAuftraege_Should_BeCalculatedFromOrderings_When_Ordering_Is_Digitalisierung()
        {
            // arrange
            var setting = new DigitalisierungsbeschraenkungSettings();
            var itemsFromOrdering = new List<OrderItem>
            {
                new OrderItem
                {
                    Status = OrderStatesInternal.FreigabePruefen // Zählt
                },
                new OrderItem
                {
                    Status = OrderStatesInternal.FuerDigitalisierungBereit // Zählt
                },
                new OrderItem
                {
                    Status = OrderStatesInternal.Abgebrochen // Zählt nicht
                },
                new OrderItem
                {
                    Status = OrderStatesInternal.DigitalisierungAbgebrochen // Zählt nicht
                },
                new OrderItem
                {
                    Status = OrderStatesInternal.ZumReponierenBereit // Zählt nicht
                },
                new OrderItem
                {
                    Status = OrderStatesInternal.Abgeschlossen // Zählt nicht
                }
            };

            var orderings = new List<Ordering>
            {
                new Ordering
                {
                    Type = OrderType.Digitalisierungsauftrag,
                    Items = itemsFromOrdering.ToArray()
                }
            };

            var sut = new KontingentBestimmer(setting);

            // act
            var result = sut.BestimmeKontingent(orderings,CreateOe2User());

            // assert
            result.AktiveDigitalisierungsauftraege.Should().Be(2);
        }

        [TestCase(OrderType.Bestellkorb)]
        [TestCase(OrderType.Einsichtsgesuch)]
        [TestCase(OrderType.Lesesaalausleihen)]
        [TestCase(OrderType.Verwaltungsausleihe)]
        [Test]
        public void BestimmeKontingent_AktiveAuftraege_Should_Not_BeCalculatedFromOrderings_When_Ordering_Is_Not_Digitalisierung(OrderType orderType)
        {
            // arrange
            var setting = new DigitalisierungsbeschraenkungSettings();
            var itemsFromOrdering = new List<OrderItem>
            {
                new OrderItem
                {
                    Status = OrderStatesInternal.FreigabePruefen
                },
                new OrderItem
                {
                    Status = OrderStatesInternal.Ausgeliehen
                }
            };

            var orderings = new List<Ordering>
            {
                new Ordering
                {
                    Type = orderType,
                    Items = itemsFromOrdering.ToArray()
                }
            };

            var sut = new KontingentBestimmer(setting);

            // act
            var result = sut.BestimmeKontingent(orderings, CreateOe2User());

            // assert
            result.AktiveDigitalisierungsauftraege.Should().Be(0);
        }


        [Test]
        public void BestimmeKontingent_Bestellkontingent_Should_Be_Correct_When_Less_Orderings_Than_Threshold()
        {
            // arrange
            var setting = new DigitalisierungsbeschraenkungSettings {DigitalisierungsbeschraenkungOe2 = 4, DigitalisierungsbeschraenkungBar = 500};

            var itemsFromOrdering = new List<OrderItem>
            {
                new OrderItem
                {
                    Status = OrderStatesInternal.FreigabePruefen // Zählt
                },
                new OrderItem
                {
                    Status = OrderStatesInternal.FuerDigitalisierungBereit // Zählt
                },
                new OrderItem
                {
                    Status = OrderStatesInternal.Abgebrochen // Zählt nicht
                },
                new OrderItem
                {
                    Status = OrderStatesInternal.DigitalisierungAbgebrochen // Zählt nicht
                },
                new OrderItem
                {
                    Status = OrderStatesInternal.ZumReponierenBereit // Zählt nicht
                },
                new OrderItem
                {
                    Status = OrderStatesInternal.Abgeschlossen // Zählt nicht
                }
            };
            var orderings = new List<Ordering>
            {
                new Ordering
                {
                    Type = OrderType.Digitalisierungsauftrag, // zählt
                    Items = itemsFromOrdering.ToArray()
                },
                new Ordering
                {
                    Type = OrderType.Einsichtsgesuch, // zählt nicht
                    Items = itemsFromOrdering.ToArray()
                }
            };

            var sut = new KontingentBestimmer(setting);

            // act
            var result = sut.BestimmeKontingent(orderings, CreateOe2User());

            // assert
            result.AktiveDigitalisierungsauftraege.Should().Be(2);
            result.Digitalisierungesbeschraenkung.Should().Be(setting.DigitalisierungsbeschraenkungOe2);
            result.Bestellkontingent.Should().Be(setting.DigitalisierungsbeschraenkungOe2 - result.AktiveDigitalisierungsauftraege);
        }

        [Test]
        public void BestimmeKontingent_Bestellkontingent_Should_Be_Zero_When_More_Orderings_Than_Threshold()
        {
            // arrange
            var setting = new DigitalisierungsbeschraenkungSettings {DigitalisierungsbeschraenkungOe2 = 1};

            var itemsFromOrdering = new List<OrderItem>
            {
                new OrderItem
                {
                    Status = OrderStatesInternal.FreigabePruefen // Zählt
                },
                new OrderItem
                {
                    Status = OrderStatesInternal.FuerDigitalisierungBereit // Zählt
                },
                new OrderItem
                {
                    Status = OrderStatesInternal.Abgebrochen // Zählt nicht
                },
                new OrderItem
                {
                    Status = OrderStatesInternal.DigitalisierungAbgebrochen // Zählt nicht
                },
                new OrderItem
                {
                    Status = OrderStatesInternal.ZumReponierenBereit // Zählt nicht
                },
                new OrderItem
                {
                    Status = OrderStatesInternal.Abgeschlossen // Zählt nicht
                }
            };
            var orderings = new List<Ordering>
            {
                new Ordering
                {
                    Type = OrderType.Digitalisierungsauftrag, // zählt
                    Items = itemsFromOrdering.ToArray()
                },
                new Ordering
                {
                    Type = OrderType.Einsichtsgesuch, // zählt nicht
                    Items = itemsFromOrdering.ToArray()
                }
            };

            var sut = new KontingentBestimmer(setting);

            // act
            var result = sut.BestimmeKontingent(orderings, CreateOe2User());

            // assert
            result.AktiveDigitalisierungsauftraege.Should().Be(2);
            result.Digitalisierungesbeschraenkung.Should().Be(setting.DigitalisierungsbeschraenkungOe2);
            result.Bestellkontingent.Should().Be(0);
        }

        [Test]
        public void BestimmeKontingent_Bestellkontingent_Should_Be_Zero_When_Orderings_Equals_Threshold()
        {
            // arrange
            var setting = new DigitalisierungsbeschraenkungSettings {DigitalisierungsbeschraenkungOe2 = 2};

            var itemsFromOrdering = new List<OrderItem>
            {
                new OrderItem
                {
                    Status = OrderStatesInternal.FreigabePruefen // Zählt
                },
                new OrderItem
                {
                    Status = OrderStatesInternal.FuerDigitalisierungBereit // Zählt
                },
                new OrderItem
                {
                    Status = OrderStatesInternal.Abgebrochen // Zählt nicht
                },
                new OrderItem
                {
                    Status = OrderStatesInternal.DigitalisierungAbgebrochen // Zählt nicht
                },
                new OrderItem
                {
                    Status = OrderStatesInternal.ZumReponierenBereit // Zählt nicht
                },
                new OrderItem
                {
                    Status = OrderStatesInternal.Abgeschlossen // Zählt nicht
                }
            };
            var orderings = new List<Ordering>
            {
                new Ordering
                {
                    Type = OrderType.Digitalisierungsauftrag, // zählt
                    Items = itemsFromOrdering.ToArray()
                },
                new Ordering
                {
                    Type = OrderType.Einsichtsgesuch, // zählt nicht
                    Items = itemsFromOrdering.ToArray()
                }
            };

            var sut = new KontingentBestimmer(setting);

            // act
            var result = sut.BestimmeKontingent(orderings, CreateOe2User());

            // assert
            result.AktiveDigitalisierungsauftraege.Should().Be(2);
            result.Digitalisierungesbeschraenkung.Should().Be(setting.DigitalisierungsbeschraenkungOe2);
            result.Bestellkontingent.Should().Be(0);
        }

        [Test]
        public void BestimmeKontingent_Bestellkontingent_Should_Be_IntMaxValue_When_User_DisablesThreshold_Ends_Tomorrow()
        {
            // arrange
            var setting = new DigitalisierungsbeschraenkungSettings {DigitalisierungsbeschraenkungBar = 1};

            var itemsFromOrdering = new List<OrderItem>
            {
                new OrderItem
                {
                    Status = OrderStatesInternal.FreigabePruefen // Zählt
                },
                new OrderItem
                {
                    Status = OrderStatesInternal.FuerDigitalisierungBereit // Zählt
                }
            };
            var orderings = new List<Ordering>
            {
                new Ordering
                {
                    Type = OrderType.Digitalisierungsauftrag, // zählt
                    Items = itemsFromOrdering.ToArray()
                }
            };

            var user = new User
            {
                DigitalisierungsbeschraenkungAufgehobenBis = DateTime.Now.AddDays(1)
            };

            var sut = new KontingentBestimmer(setting);

            // act
            var result = sut.BestimmeKontingent(orderings, user);

            // assert
            result.Bestellkontingent.Should().Be(int.MaxValue);
        }


        [Test]
        public void BestimmeKontingent_Bestellkontingent_Should_Be_IntMaxValue_When_User_DisablesThreshold_Ends_Today()
        {
            // arrange
            var setting = new DigitalisierungsbeschraenkungSettings {DigitalisierungsbeschraenkungBar = 1};

            var itemsFromOrdering = new List<OrderItem>
            {
                new OrderItem
                {
                    Status = OrderStatesInternal.FreigabePruefen // Zählt
                },
                new OrderItem
                {
                    Status = OrderStatesInternal.FuerDigitalisierungBereit // Zählt
                }
            };
            var orderings = new List<Ordering>
            {
                new Ordering
                {
                    Type = OrderType.Digitalisierungsauftrag, // zählt
                    Items = itemsFromOrdering.ToArray()
                }
            };

            var user = new User
            {
                DigitalisierungsbeschraenkungAufgehobenBis = DateTime.Now.AddHours(1)
            };

            var sut = new KontingentBestimmer(setting);

            // act
            var result = sut.BestimmeKontingent(orderings, user);

            // assert
            result.Bestellkontingent.Should().Be(int.MaxValue);
        }

        [Test]
        public void BestimmeKontingent_Bestellkontingent_Should_Be_Zero_When_User_DisablesThreshold_Ends_Yesterday()
        {
            // arrange
            var setting = new DigitalisierungsbeschraenkungSettings {DigitalisierungsbeschraenkungOe2 = 1};

            var itemsFromOrdering = new List<OrderItem>
            {
                new OrderItem
                {
                    Status = OrderStatesInternal.FreigabePruefen // Zählt
                },
                new OrderItem
                {
                    Status = OrderStatesInternal.FuerDigitalisierungBereit // Zählt
                }
            };
            var orderings = new List<Ordering>
            {
                new Ordering
                {
                    Type = OrderType.Digitalisierungsauftrag, // zählt
                    Items = itemsFromOrdering.ToArray()
                }
            };

            var user = CreateOe2User();
            user.DigitalisierungsbeschraenkungAufgehobenBis = DateTime.Now.AddDays(-1);
            var sut = new KontingentBestimmer(setting);

            // act
            var result = sut.BestimmeKontingent(orderings, user);

            // assert
            result.Bestellkontingent.Should().Be(0);
        }

        private static User CreateOe2User()
        {
            return new User
            {
                RolePublicClient = AccessRoles.RoleOe2,
                Access = new UserAccess("2", AccessRoles.RoleOe2, "oe2", null, false)
            };

        }
    }
}