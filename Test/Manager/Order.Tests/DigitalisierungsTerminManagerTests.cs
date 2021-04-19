using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Order;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;

namespace CMI.Manager.Order.Tests
{
    [TestFixture]
    public class DigitalisierungsTerminManagerTests
    {
        [Test]
        public void GetNextPossibleTermin_Should_Respect_Kontingent_Test_1()
        {
            // arrange
            var sut = new DigitalisierungsTerminManager(null);
            var orderDate = CreateDate("26.11.2019 11:54:40");
            var expected = CreateDate("02.12.2019 08:25:07");

            var kontingent = new DigitalisierungsKontingent
            {
                AnzahlAuftraege = 1,
                InAnzahlTagen = 5
            };

            var nextTermine = new List<DigitalisierungsTermin>
            {
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("25.11.2019 08:25:07")}
            };

            // act
            var result = sut.GetNextPossibleTermin(orderDate, nextTermine, kontingent);

            // assert
            result.Should().Be(expected);
        }

        [Test]
        public void GetNextPossibleTermin_Should_Respect_Kontingent_Test_2_1()
        {
            // arrange
            var sut = new DigitalisierungsTerminManager(null);
            var orderDate1 = CreateDate("03.12.2019 15:17:34");
            var expected = CreateDate("11.12.2019 10:03:06");

            var kontingent = new DigitalisierungsKontingent
            {
                AnzahlAuftraege = 1,
                InAnzahlTagen = 5
            };

            var nextTermine = new List<DigitalisierungsTermin>
            {
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("27.11.2019 10:03:06")},
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("04.12.2019 10:03:06")}
            };

            // act
            var result = sut.GetNextPossibleTermin(orderDate1, nextTermine, kontingent);

            // assert
            result.Should().Be(expected);
        }


        [Test]
        public void GetNextPossibleTermin_Should_Respect_Kontingent_Test_2_2()
        {
            // arrange
            var sut = new DigitalisierungsTerminManager(null);
            var orderDate1 = CreateDate("03.12.2019 15:17:34");
            var expected = CreateDate("18.12.2019 10:03:06");

            var kontingent = new DigitalisierungsKontingent
            {
                AnzahlAuftraege = 1,
                InAnzahlTagen = 5
            };

            var nextTermine = new List<DigitalisierungsTermin>
            {
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("27.11.2019 10:03:06")},
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("04.12.2019 10:03:06")},
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("11.12.2019 10:03:06")}
            };

            // act
            var result = sut.GetNextPossibleTermin(orderDate1, nextTermine, kontingent);

            // assert
            result.Should().Be(expected);
        }

        [Test]
        public void GetNextPossibleTermin_Should_Respect_Kontingent_Test_3_1()
        {
            // arrange
            var sut = new DigitalisierungsTerminManager(null);
            var orderDate1 = CreateDate("23.11.2019 11:54:40");
            var expected = CreateDate("25.11.2019 00:00:00");

            var kontingent = new DigitalisierungsKontingent
            {
                AnzahlAuftraege = 1,
                InAnzahlTagen = 5
            };

            var nextTermine = new List<DigitalisierungsTermin>
            {
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("15.11.2019 23:18:59")}
            };

            // act
            var result = sut.GetNextPossibleTermin(orderDate1, nextTermine, kontingent);

            // assert
            result.Should().Be(expected);
        }

        [Test]
        public void GetNextPossibleTermin_Should_Respect_Kontingent_Test_3_2()
        {
            // arrange
            var sut = new DigitalisierungsTerminManager(null);
            var orderDate1 = CreateDate("23.11.2019 11:54:40");
            var expected = CreateDate("02.12.2019 00:00:00");

            var kontingent = new DigitalisierungsKontingent
            {
                AnzahlAuftraege = 1,
                InAnzahlTagen = 5
            };

            var nextTermine = new List<DigitalisierungsTermin>
            {
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("15.11.2019 23:18:59")},
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("2019.11.25 00:00:00")}
            };

            // act
            var result = sut.GetNextPossibleTermin(orderDate1, nextTermine, kontingent);

            // assert
            result.Should().Be(expected);
        }

        [Test]
        public void GetNextPossibleTermin_Should_Respect_Kontingent_Test_4()
        {
            // arrange
            var sut = new DigitalisierungsTerminManager(null);
            var orderDate1 = CreateDate("31.10.2019 06:53:28");
            var expected = CreateDate("15.11.2019 04:11:37");

            var kontingent = new DigitalisierungsKontingent
            {
                AnzahlAuftraege = 1,
                InAnzahlTagen = 12
            };

            var nextTermine = new List<DigitalisierungsTermin>
            {
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("10.10.2019 01:05:52")},
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("30.10.2019 04:11:37")}
            };

            // act
            var result = sut.GetNextPossibleTermin(orderDate1, nextTermine, kontingent);

            // assert
            result.Should().Be(expected);
        }

        [Test]
        public void GetNextPossibleTermin_Should_Respect_Kontingent_Test_4_2()
        {
            // arrange
            var sut = new DigitalisierungsTerminManager(null);
            var orderDate1 = CreateDate("31.10.2019 06:53:28");
            var expected = CreateDate("03.12.2019 04:11:37");

            var kontingent = new DigitalisierungsKontingent
            {
                AnzahlAuftraege = 1,
                InAnzahlTagen = 12
            };

            var nextTermine = new List<DigitalisierungsTermin>
            {
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("10.10.2019 01:05:52")},
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("30.10.2019 04:11:37")},
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("15.11.2019 04:11:37")}
            };

            // act
            var result = sut.GetNextPossibleTermin(orderDate1, nextTermine, kontingent);

            // assert
            result.Should().Be(expected);
        }

        [Test]
        public void GetNextPossibleTermin_Should_Respect_Kontingent_Test_4_3()
        {
            // arrange
            var sut = new DigitalisierungsTerminManager(null);
            var orderDate1 = CreateDate("31.10.2019 06:53:28");
            var expected = CreateDate("19.12.2019 04:11:37");

            var kontingent = new DigitalisierungsKontingent
            {
                AnzahlAuftraege = 1,
                InAnzahlTagen = 12
            };

            var nextTermine = new List<DigitalisierungsTermin>
            {
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("10.10.2019 01:05:52")},
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("30.10.2019 04:11:37")},
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("15.11.2019 04:11:37")},
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("03.12.2019 04:11:37")}
            };

            // act
            var result = sut.GetNextPossibleTermin(orderDate1, nextTermine, kontingent);

            // assert
            result.Should().Be(expected);
        }

        [Test]
        public void GetNextPossibleTermin_Should_Respect_Kontingent_Test_5()
        {
            // arrange
            var sut = new DigitalisierungsTerminManager(null);
            var orderDate1 = CreateDate("26.11.2019 08:54:40");
            var expected = CreateDate("26.11.2019 08:54:40");

            var kontingent = new DigitalisierungsKontingent
            {
                AnzahlAuftraege = 3,
                InAnzahlTagen = 1
            };

            var nextTermine = new List<DigitalisierungsTermin>
            {
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("20.11.2019 16:25:07")},
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("26.11.2019 08:37:45")}
            };

            // act
            var result = sut.GetNextPossibleTermin(orderDate1, nextTermine, kontingent);

            // assert
            result.Should().Be(expected);
        }

        [Test]
        public void GetNextPossibleTermin_Should_Respect_Kontingent_Test_5_2()
        {
            // arrange
            var sut = new DigitalisierungsTerminManager(null);
            var orderDate1 = CreateDate("26.11.2019 08:54:40");
            var expected = CreateDate("26.11.2019 08:54:40");

            var kontingent = new DigitalisierungsKontingent
            {
                AnzahlAuftraege = 3,
                InAnzahlTagen = 1
            };

            var nextTermine = new List<DigitalisierungsTermin>
            {
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("20.11.2019 16:25:07")},
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("26.11.2019 08:37:45")},
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("26.11.2019 08:54:40")}
            };

            // act
            var result = sut.GetNextPossibleTermin(orderDate1, nextTermine, kontingent);

            // assert
            result.Should().Be(expected);
        }

        [Test]
        public void GetNextPossibleTermin_Should_Respect_Kontingent_Test_5_3()
        {
            // arrange
            var sut = new DigitalisierungsTerminManager(null);
            var orderDate1 = CreateDate("26.11.2019 08:54:40");
            var expected = CreateDate("27.11.2019 08:37:45");

            var kontingent = new DigitalisierungsKontingent
            {
                AnzahlAuftraege = 3,
                InAnzahlTagen = 1
            };

            var nextTermine = new List<DigitalisierungsTermin>
            {
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("20.11.2019 16:25:07")},
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("26.11.2019 08:37:45")},
                new DigitalisierungsTermin {AnzahlAuftraege = 2, Termin = CreateDate("26.11.2019 08:54:40")}
            };

            // act
            var result = sut.GetNextPossibleTermin(orderDate1, nextTermine, kontingent);

            // assert
            result.Should().Be(expected);
        }

        [Test]
        public void GetNextPossibleTermin_Should_Respect_Kontingent_Test_6()
        {
            // arrange
            var sut = new DigitalisierungsTerminManager(null);
            var orderDate1 = CreateDate("07.12.2019 19:25:24");
            var expected = CreateDate("09.12.2019 00:00:00");

            var kontingent = new DigitalisierungsKontingent
            {
                AnzahlAuftraege = 3,
                InAnzahlTagen = 1
            };

            var nextTermine = new List<DigitalisierungsTermin>
            {
                new DigitalisierungsTermin {AnzahlAuftraege = 3, Termin = CreateDate("06.12.2019 21:11:04")}
            };

            // act
            var result = sut.GetNextPossibleTermin(orderDate1, nextTermine, kontingent);

            // assert
            result.Should().Be(expected);
        }

        [Test]
        public void GetNextPossibleTermin_Should_Respect_Kontingent_Test_7()
        {
            // arrange
            var sut = new DigitalisierungsTerminManager(null);
            var orderDate1 = CreateDate("12.12.2019 18:52:47");
            var expected = CreateDate("13.12.2019 14:16:09");

            var kontingent = new DigitalisierungsKontingent
            {
                AnzahlAuftraege = 2,
                InAnzahlTagen = 1
            };

            var nextTermine = new List<DigitalisierungsTermin>
            {
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("11.12.2019 12:45:29")},
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("12.12.2019 14:16:09")},
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("12.12.2019 15:18:01")}
            };

            // act
            var result = sut.GetNextPossibleTermin(orderDate1, nextTermine, kontingent);

            // assert
            result.Should().Be(expected);
        }

        [Test]
        public void GetNextPossibleTermin_Should_Respect_Kontingent_Test_7_1()
        {
            // arrange
            var sut = new DigitalisierungsTerminManager(null);
            var orderDate1 = CreateDate("12.12.2019 18:52:47");
            var expected = CreateDate("13.12.2019 15:18:01");

            var kontingent = new DigitalisierungsKontingent
            {
                AnzahlAuftraege = 2,
                InAnzahlTagen = 1
            };

            var nextTermine = new List<DigitalisierungsTermin>
            {
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("11.12.2019 12:45:29")},
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("12.12.2019 14:16:09")},
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("12.12.2019 15:18:01")},
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("13.12.2019 14:16:09")}
            };

            // act
            var result = sut.GetNextPossibleTermin(orderDate1, nextTermine, kontingent);

            // assert
            result.Should().Be(expected);
        }

        [Test]
        public void GetNextPossibleTermin_Should_Respect_Kontingent_Test_7_2()
        {
            // arrange
            var sut = new DigitalisierungsTerminManager(null);
            var orderDate1 = CreateDate("12.12.2019 18:52:47");
            var expected = CreateDate("16.12.2019 14:16:09");

            var kontingent = new DigitalisierungsKontingent
            {
                AnzahlAuftraege = 2,
                InAnzahlTagen = 1
            };

            var nextTermine = new List<DigitalisierungsTermin>
            {
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("11.12.2019 12:45:29")},
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("12.12.2019 14:16:09")},
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("12.12.2019 15:18:01")},
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("13.12.2019 14:16:09")},
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("13.12.2019 15:18:01")}
            };

            // act
            var result = sut.GetNextPossibleTermin(orderDate1, nextTermine, kontingent);

            // assert
            result.Should().Be(expected);
        }

        [Test]
        public void GetNextPossibleTermin_Should_Respect_Kontingent_Test_8()
        {
            // arrange
            var sut = new DigitalisierungsTerminManager(null);
            var orderDate1 = CreateDate("20.12.2019 23:24:51");
            var expected = CreateDate("20.12.2019 23:24:51");

            var kontingent = new DigitalisierungsKontingent
            {
                AnzahlAuftraege = 2,
                InAnzahlTagen = 1
            };

            var nextTermine = new List<DigitalisierungsTermin>();

            // act
            var result = sut.GetNextPossibleTermin(orderDate1, nextTermine, kontingent);

            // assert
            result.Should().Be(expected);
        }

        [Test]
        public void GetNextPossibleTermin_Should_Respect_Kontingent_Test_8_1()
        {
            // arrange
            var sut = new DigitalisierungsTerminManager(null);
            var orderDate1 = CreateDate("20.12.2019 23:24:51");
            var expected = CreateDate("20.12.2019 23:24:51");

            var kontingent = new DigitalisierungsKontingent
            {
                AnzahlAuftraege = 2,
                InAnzahlTagen = 1
            };

            var nextTermine = new List<DigitalisierungsTermin>
            {
                new DigitalisierungsTermin {AnzahlAuftraege = 1, Termin = CreateDate("20.12.2019 23:24:51")}
            };

            // act
            var result = sut.GetNextPossibleTermin(orderDate1, nextTermine, kontingent);

            // assert
            result.Should().Be(expected);
        }

        [Test]
        public void GetNextPossibleTermin_Should_Respect_Kontingent_Test_8_2()
        {
            // arrange
            var sut = new DigitalisierungsTerminManager(null);
            var orderDate1 = CreateDate("20.12.2019 23:24:51");
            var expected = CreateDate("23.12.2019 23:24:51");

            var kontingent = new DigitalisierungsKontingent
            {
                AnzahlAuftraege = 2,
                InAnzahlTagen = 1
            };

            var nextTermine = new List<DigitalisierungsTermin>
            {
                new DigitalisierungsTermin {AnzahlAuftraege = 2, Termin = CreateDate("20.12.2019 23:24:51")}
            };

            // act
            var result = sut.GetNextPossibleTermin(orderDate1, nextTermine, kontingent);

            // assert
            result.Should().Be(expected);
        }

        [Test]
        public async Task GetRecalcedTermin_Should_Respect_Kontingent_Test_1()
        {
            // arrange
            var data = new Dictionary<int, OrderItem>
            {
                [1] = new OrderItem
                {
                    Id = 1,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Amt,
                    TerminDigitalisierung = CreateDate("25.11.2019 07:16:30"),
                    Status = OrderStatesInternal.Abgeschlossen
                },
                [2] = new OrderItem
                {
                    Id = 2,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Amt,
                    TerminDigitalisierung = CreateDate("29.11.2019 16:25:36"),
                    Status = OrderStatesInternal.Ausgeliehen
                },
                [3] = new OrderItem
                {
                    Id = 3,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Amt,
                    TerminDigitalisierung = CreateDate("06.12.2019 08:37:45"),
                    Status = OrderStatesInternal.FuerDigitalisierungBereit
                },
                [4] = new OrderItem
                {
                    Id = 4,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Amt,
                    TerminDigitalisierung = CreateDate("12.12.2019 08:37:45"),
                    Status = OrderStatesInternal.FuerDigitalisierungBereit
                }
            };
            var digipoolAccess = new MockDigipoolAccess(data);
            var sut = new DigitalisierungsTerminManager(digipoolAccess);

            var neuesKontingent = new DigitalisierungsKontingent
            {
                AnzahlAuftraege = 1,
                InAnzahlTagen = 10
            };

            // act
            await sut.RecalcTermine(DigitalisierungsKategorie.Amt, neuesKontingent);

            // assert
            var digipool = await digipoolAccess.GetDigipool();
            digipool[0].TerminDigitalisierung.Should().Be(CreateDate("13.12.2019 16:25:36"));
            digipool[1].TerminDigitalisierung.Should().Be(CreateDate("27.12.2019 16:25:36"));
        }


        [Test]
        public async Task GetRecalcedTermin_Should_Respect_Kontingent_Test_2()
        {
            // arrange
            var data = new Dictionary<int, OrderItem>
            {
                [1] = new OrderItem
                {
                    Id = 1,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Amt,
                    TerminDigitalisierung = CreateDate("25.11.2019 00:00:00"),
                    Status = OrderStatesInternal.Abgebrochen
                },
                [2] = new OrderItem
                {
                    Id = 2,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Amt,
                    TerminDigitalisierung = CreateDate("29.11.2019 00:00:00"),
                    Status = OrderStatesInternal.FuerDigitalisierungBereit
                },
                [3] = new OrderItem
                {
                    Id = 3,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Amt,
                    TerminDigitalisierung = CreateDate("05.12.2019 08:37:45"),
                    Status = OrderStatesInternal.DigitalisierungExtern
                }
            };
            var digipoolAccess = new MockDigipoolAccess(data);
            var sut = new DigitalisierungsTerminManager(digipoolAccess);

            var neuesKontingent = new DigitalisierungsKontingent
            {
                AnzahlAuftraege = 1,
                InAnzahlTagen = 2
            };

            // act
            await sut.RecalcTermine(DigitalisierungsKategorie.Amt, neuesKontingent);

            // assert
            var digipool = await digipoolAccess.GetDigipool();
            digipool[0].TerminDigitalisierung.Should().Be(CreateDate("29.11.2019 00:00:00"));
        }


        [Test]
        public async Task GetRecalcedTermin_Should_Respect_Kontingent_Test_3()
        {
            // arrange
            var data = new Dictionary<int, OrderItem>
            {
                [1] = new OrderItem
                {
                    Id = 1,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Amt,
                    TerminDigitalisierung = CreateDate("07.11.2019 13:47:10"),
                    Status = OrderStatesInternal.ZumReponierenBereit
                },
                [2] = new OrderItem
                {
                    Id = 2,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Amt,
                    TerminDigitalisierung = CreateDate("13.11.2019 13:47:10"),
                    Status = OrderStatesInternal.FuerDigitalisierungBereit
                },
                [3] = new OrderItem
                {
                    Id = 3,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Amt,
                    TerminDigitalisierung = CreateDate("19.12.2019 13:47:10"),
                    Status = OrderStatesInternal.FuerDigitalisierungBereit
                },
                [4] = new OrderItem
                {
                    Id = 4,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Amt,
                    TerminDigitalisierung = CreateDate("25.12.2019 13:47:10"),
                    Status = OrderStatesInternal.FuerDigitalisierungBereit
                },
                [5] = new OrderItem
                {
                    Id = 5,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Amt,
                    TerminDigitalisierung = CreateDate("29.12.2019 13:47:10"),
                    Status = OrderStatesInternal.FuerDigitalisierungBereit
                }
            };
            var digipoolAccess = new MockDigipoolAccess(data);
            var sut = new DigitalisierungsTerminManager(digipoolAccess);

            var neuesKontingent = new DigitalisierungsKontingent
            {
                AnzahlAuftraege = 2,
                InAnzahlTagen = 1
            };

            // act
            await sut.RecalcTermine(DigitalisierungsKategorie.Amt, neuesKontingent);

            // assert
            var digipool = await digipoolAccess.GetDigipool();
            digipool[0].TerminDigitalisierung.Should().Be(CreateDate("13.11.2019 13:47:10"));
            digipool[1].TerminDigitalisierung.Should().Be(CreateDate("13.11.2019 13:47:10"));
            digipool[2].TerminDigitalisierung.Should().Be(CreateDate("14.11.2019 13:47:10"));
            digipool[3].TerminDigitalisierung.Should().Be(CreateDate("14.11.2019 13:47:10"));
        }

        [Test]
        public async Task GetRecalcedTermin_Should_Respect_Kontingent_Test_4()
        {
            // arrange
            var data = new Dictionary<int, OrderItem>
            {
                [1] = new OrderItem
                {
                    Id = 1,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Amt,
                    TerminDigitalisierung = CreateDate("17.11.2019 09:19:58"),
                    Status = OrderStatesInternal.Abgeschlossen
                },
                [2] = new OrderItem
                {
                    Id = 2,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Amt,
                    TerminDigitalisierung = CreateDate("17.12.2019 13:25:36"),
                    Status = OrderStatesInternal.Ausgeliehen
                }
            };
            var digipoolAccess = new MockDigipoolAccess(data);
            var sut = new DigitalisierungsTerminManager(digipoolAccess);

            var neuesKontingent = new DigitalisierungsKontingent
            {
                AnzahlAuftraege = 3,
                InAnzahlTagen = 1
            };

            // act
            await sut.RecalcTermine(DigitalisierungsKategorie.Amt, neuesKontingent);

            // assert
            var items = await digipoolAccess.GetLatestDigitalisierungsTermine(null, DateTime.MinValue, DigitalisierungsKategorie.Amt);
            items[0].Termin.Should().Be(CreateDate("17.11.2019 09:19:58"), "Keine Anpassung, da kein Auftrag im Status 'für digitalisierung bereit'");
            items[1].Termin.Should().Be(CreateDate("17.12.2019 13:25:36"), "Keine Anpassung, da kein Auftrag im Status 'für digitalisierung bereit'");
        }


        [Test]
        public async Task GetRecalcedTermin_Should_Respect_Kontingent_Test_5()
        {
            // arrange
            var data = new Dictionary<int, OrderItem>
            {
                [1] = new OrderItem
                {
                    Id = 1,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Amt,
                    TerminDigitalisierung = CreateDate("05.12.2019 21:33:49"),
                    Status = OrderStatesInternal.ZumReponierenBereit
                },
                [2] = new OrderItem
                {
                    Id = 2,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Amt,
                    TerminDigitalisierung = CreateDate("09.12.2019 11:28:36"),
                    Status = OrderStatesInternal.FuerDigitalisierungBereit
                },
                [3] = new OrderItem
                {
                    Id = 3,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Amt,
                    TerminDigitalisierung = CreateDate("10.12.2019 12:07:29"),
                    Status = OrderStatesInternal.FuerDigitalisierungBereit
                },
                [4] = new OrderItem
                {
                    Id = 4,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Amt,
                    TerminDigitalisierung = CreateDate("10.12.2019 17:38:14"),
                    Status = OrderStatesInternal.DigitalisierungAbgebrochen
                }
            };
            var digipoolAccess = new MockDigipoolAccess(data);
            var sut = new DigitalisierungsTerminManager(digipoolAccess);

            var neuesKontingent = new DigitalisierungsKontingent
            {
                AnzahlAuftraege = 4,
                InAnzahlTagen = 1
            };

            // act
            await sut.RecalcTermine(DigitalisierungsKategorie.Amt, neuesKontingent);

            // assert
            var digipool = await digipoolAccess.GetDigipool();
            digipool[0].TerminDigitalisierung.Should().Be(CreateDate("09.12.2019 11:28:36"));
            digipool[1].TerminDigitalisierung.Should().Be(CreateDate("09.12.2019 11:28:36"));
        }

        [Test]
        public async Task GetRecalcedTermin_Should_Respect_Kontingent_Test_Own()
        {
            // arrange
            var data = new Dictionary<int, OrderItem>
            {
                [1] = new OrderItem
                {
                    Id = 1,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Termin,
                    TerminDigitalisierung = CreateDate("16.01.2020 09:59:27"),
                    Status = OrderStatesInternal.FuerDigitalisierungBereit
                },
                [2] = new OrderItem
                {
                    Id = 2,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Termin,
                    TerminDigitalisierung = CreateDate("16.01.2020 09:59:27"),
                    Status = OrderStatesInternal.FuerDigitalisierungBereit
                },
                [3] = new OrderItem
                {
                    Id = 3,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Termin,
                    TerminDigitalisierung = CreateDate("16.01.2020 09:59:27"),
                    Status = OrderStatesInternal.FuerDigitalisierungBereit
                },
                [4] = new OrderItem
                {
                    Id = 4,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Termin,
                    TerminDigitalisierung = CreateDate("16.01.2020 09:59:27"),
                    Status = OrderStatesInternal.FuerDigitalisierungBereit
                },
                [5] = new OrderItem
                {
                    Id = 5,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Oeffentlichkeit,
                    TerminDigitalisierung = CreateDate("08.05.2019 11:48:06"),
                    Status = OrderStatesInternal.FuerDigitalisierungBereit
                },
                [6] = new OrderItem
                {
                    Id = 6,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Amt,
                    TerminDigitalisierung = CreateDate("08.05.2019 11:34:11"),
                    Status = OrderStatesInternal.FuerAushebungBereit
                },
                [7] = new OrderItem
                {
                    Id = 7,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Amt,
                    TerminDigitalisierung = CreateDate("07.05.2019 13:39:49"),
                    Status = OrderStatesInternal.AushebungsauftragErstellt
                }
            };
            var digipoolAccess = new MockDigipoolAccess(data);
            var sut = new DigitalisierungsTerminManager(digipoolAccess);

            var neuesKontingent = new DigitalisierungsKontingent
            {
                AnzahlAuftraege = 2,
                InAnzahlTagen = 1
            };

            // act
            await sut.RecalcTermine(DigitalisierungsKategorie.Termin, neuesKontingent);

            // assert
            var digipool = await digipoolAccess.GetDigipool();
            var json = JsonConvert.SerializeObject(digipool);

            digipool[0].TerminDigitalisierung.Should().Be(CreateDate("16.01.2020 09:59:27"));
            digipool[1].TerminDigitalisierung.Should().Be(CreateDate("16.01.2020 09:59:27"));
            digipool[2].TerminDigitalisierung.Should().Be(CreateDate("17.01.2020 09:59:27"));
            digipool[3].TerminDigitalisierung.Should().Be(CreateDate("17.01.2020 09:59:27"));
        }

        [Test]
        public async Task GetRecalcedTermin_Should_Respect_Kontingent_Test_Own_2()
        {
            // arrange
            var data = new Dictionary<int, OrderItem>
            {
                [1] = new OrderItem
                {
                    Id = 1,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Termin,
                    TerminDigitalisierung = CreateDate("11.02.2019 00:00:00"),
                    Status = OrderStatesInternal.FuerDigitalisierungBereit
                },
                [2] = new OrderItem
                {
                    Id = 2,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Termin,
                    TerminDigitalisierung = CreateDate("11.02.2019 00:00:00"),
                    Status = OrderStatesInternal.FuerDigitalisierungBereit
                },
                [3] = new OrderItem
                {
                    Id = 3,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Termin,
                    TerminDigitalisierung = CreateDate("13.02.2019 00:00:00"),
                    Status = OrderStatesInternal.FuerDigitalisierungBereit
                },
                [4] = new OrderItem
                {
                    Id = 4,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Termin,
                    TerminDigitalisierung = CreateDate("13.02.2019 00:00:00"),
                    Status = OrderStatesInternal.FuerDigitalisierungBereit
                },
                [5] = new OrderItem
                {
                    Id = 5,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Termin,
                    TerminDigitalisierung = CreateDate("13.02.2019 00:00:00"),
                    Status = OrderStatesInternal.FuerDigitalisierungBereit
                },
                [6] = new OrderItem
                {
                    Id = 6,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Termin,
                    TerminDigitalisierung = CreateDate("13.02.2019 00:00:00"),
                    Status = OrderStatesInternal.FuerDigitalisierungBereit
                },
                [7] = new OrderItem
                {
                    Id = 7,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Termin,
                    TerminDigitalisierung = CreateDate("13.02.2019 00:00:00"),
                    Status = OrderStatesInternal.FuerDigitalisierungBereit
                }
            };
            var digipoolAccess = new MockDigipoolAccess(data);
            var sut = new DigitalisierungsTerminManager(digipoolAccess);

            var neuesKontingent = new DigitalisierungsKontingent
            {
                AnzahlAuftraege = 2,
                InAnzahlTagen = 1
            };

            // act
            await sut.RecalcTermine(DigitalisierungsKategorie.Termin, neuesKontingent);

            // assert
            var digipool = await digipoolAccess.GetDigipool();
            var json = JsonConvert.SerializeObject(digipool);

            digipool[0].TerminDigitalisierung.Should().Be(CreateDate("11.02.2019 00:00:00"));
            digipool[1].TerminDigitalisierung.Should().Be(CreateDate("11.02.2019 00:00:00"));
            digipool[2].TerminDigitalisierung.Should().Be(CreateDate("12.02.2019 00:00:00"));
            digipool[3].TerminDigitalisierung.Should().Be(CreateDate("12.02.2019 00:00:00"));
            digipool[4].TerminDigitalisierung.Should().Be(CreateDate("13.02.2019 00:00:00"));
            digipool[5].TerminDigitalisierung.Should().Be(CreateDate("13.02.2019 00:00:00"));
            digipool[6].TerminDigitalisierung.Should().Be(CreateDate("14.02.2019 00:00:00"));
        }

        [Test]
        public async Task GetRecalcedTermin_Should_Respect_Kontingent_Test_Own_MultipleUsers()
        {
            // arrange
            var data = new Dictionary<int, OrderItem>
            {
                [1] = new OrderItem
                {
                    Id = 1,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Termin,
                    TerminDigitalisierung = CreateDate("16.01.2020 09:59:27"),
                    Status = OrderStatesInternal.FuerDigitalisierungBereit,
                    SachbearbeiterId = "1"
                },
                [2] = new OrderItem
                {
                    Id = 2,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Termin,
                    TerminDigitalisierung = CreateDate("16.01.2020 09:59:27"),
                    Status = OrderStatesInternal.FuerDigitalisierungBereit,
                    SachbearbeiterId = "1"
                },
                [3] = new OrderItem
                {
                    Id = 3,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Termin,
                    TerminDigitalisierung = CreateDate("16.01.2020 09:59:27"),
                    Status = OrderStatesInternal.FuerDigitalisierungBereit,
                    SachbearbeiterId = "1"
                },
                [4] = new OrderItem
                {
                    Id = 4,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Termin,
                    TerminDigitalisierung = CreateDate("16.01.2020 09:59:27"),
                    Status = OrderStatesInternal.FuerDigitalisierungBereit,
                    SachbearbeiterId = "2"
                },
                [5] = new OrderItem
                {
                    Id = 5,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Termin,
                    TerminDigitalisierung = CreateDate("16.01.2020 09:59:27"),
                    Status = OrderStatesInternal.FuerDigitalisierungBereit,
                    SachbearbeiterId = "2"
                },
                [6] = new OrderItem
                {
                    Id = 6,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Termin,
                    TerminDigitalisierung = CreateDate("16.01.2020 09:59:27"),
                    Status = OrderStatesInternal.FuerDigitalisierungBereit,
                    SachbearbeiterId = "2"
                },
                [7] = new OrderItem
                {
                    Id = 7,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Oeffentlichkeit,
                    TerminDigitalisierung = CreateDate("08.05.2019 11:48:06"),
                    Status = OrderStatesInternal.FuerDigitalisierungBereit,
                    SachbearbeiterId = "2"
                },
                [8] = new OrderItem
                {
                    Id = 8,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Amt,
                    TerminDigitalisierung = CreateDate("08.05.2019 11:34:11"),
                    Status = OrderStatesInternal.FuerAushebungBereit,
                    SachbearbeiterId = "3"
                },
                [9] = new OrderItem
                {
                    Id = 9,
                    DigitalisierungsKategorie = DigitalisierungsKategorie.Amt,
                    TerminDigitalisierung = CreateDate("07.05.2019 13:39:49"),
                    Status = OrderStatesInternal.AushebungsauftragErstellt,
                    SachbearbeiterId = "3"
                }
            };

            var digipoolAccess = new MockDigipoolAccess(data);
            var sut = new DigitalisierungsTerminManager(digipoolAccess);

            var neuesKontingent = new DigitalisierungsKontingent
            {
                AnzahlAuftraege = 2,
                InAnzahlTagen = 1
            };

            // act
            await sut.RecalcTermine(DigitalisierungsKategorie.Termin, neuesKontingent);

            // assert
            var digipool = await digipoolAccess.GetDigipool();
            var json = JsonConvert.SerializeObject(digipool);

            digipool[0].TerminDigitalisierung.Should().Be(CreateDate("16.01.2020 09:59:27"));
            digipool[0].UserId.Should().Be("1");
            digipool[1].TerminDigitalisierung.Should().Be(CreateDate("16.01.2020 09:59:27"));
            digipool[1].UserId.Should().Be("1");
            digipool[2].TerminDigitalisierung.Should().Be(CreateDate("17.01.2020 09:59:27"));
            digipool[2].UserId.Should().Be("1");

            digipool[3].TerminDigitalisierung.Should().Be(CreateDate("16.01.2020 09:59:27"));
            digipool[3].UserId.Should().Be("2");
            digipool[4].TerminDigitalisierung.Should().Be(CreateDate("16.01.2020 09:59:27"));
            digipool[4].UserId.Should().Be("2");
            digipool[5].TerminDigitalisierung.Should().Be(CreateDate("17.01.2020 09:59:27"));
            digipool[5].UserId.Should().Be("2");
        }

        internal DateTime CreateDate(string date)
        {
            return DateTime.Parse(date);
        }
    }
}