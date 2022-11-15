using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using CMI.Contract.Order;

namespace CMI.Access.Sql.Viaduc
{
    public class OrderDataAccess : DataAccess, IOrderDataAccess
    {
        // async lock: Instantiate a Singleton of the Semaphore with a value of 1. This means that only 1 thread can be granted access at a time.
        private static readonly SemaphoreSlim lockSemaphore = new SemaphoreSlim(1, 1);
        private readonly string connectionString;

        public OrderDataAccess(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task<OrderItem> AddToBasket(OrderingIndexSnapshot indexSnapshot, string userId)
        {
            var basketId = await CreateBasketIfNecessary(userId);

            var orderItemId = await AddToBasket(indexSnapshot, basketId);

            return await GetOrderItem(orderItemId);
        }

        public async Task<OrderItem> AddToBasket(string bestand, string ablieferung, string behaeltnisNummer, string archivNummer,
            string aktenzeichen, string dossiertitel, string zeitraumDossier, string userId)
        {
            var basketId = await CreateBasketIfNecessary(userId);
            var signatur = GetFormOrderSignatur(bestand, ablieferung, behaeltnisNummer, archivNummer);

            var orderItemId = await AddToBasket(bestand, ablieferung, behaeltnisNummer, archivNummer, aktenzeichen, dossiertitel, zeitraumDossier,
                signatur, basketId);

            return new OrderItem
            {
                Id = orderItemId,
                Comment = string.Empty,
                Bestand = bestand,
                Ablieferung = ablieferung,
                BehaeltnisNummer = behaeltnisNummer,
                ArchivNummer = archivNummer,
                Dossiertitel = dossiertitel,
                ZeitraumDossier = zeitraumDossier,
                Signatur = signatur
            };
        }

        public async Task RemoveFromBasket(int orderItemId, string userId)
        {
            await EnsureLegalUser(orderItemId, userId);

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM OrderItem WHERE ID = @p1";
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "p1",
                        Value = orderItemId,
                        SqlDbType = SqlDbType.Int
                    });

                    await cmd.ExecuteScalarAsync();
                }
            }
        }

        public async Task UpdateBewilligung(int orderItemId, DateTime? bewilligungsDatum, string userId)
        {
            await EnsureLegalUser(orderItemId, userId);

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "UPDATE OrderItem SET BewilligungsDatum = (@p1) WHERE ID = (@p2)";
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "p1",
                        Value = ToDb(bewilligungsDatum),
                        SqlDbType = SqlDbType.DateTime
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "p2",
                        Value = orderItemId,
                        SqlDbType = SqlDbType.Int
                    });

                    await cmd.ExecuteScalarAsync();
                }
            }
        }

        public async Task UpdateComment(int orderItemId, string comment, string userId)
        {
            await EnsureLegalUser(orderItemId, userId);

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "UPDATE OrderItem SET Comment = (@p1) WHERE ID = (@p2)";
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "p1",
                        Value = ToDb(comment),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "p2",
                        Value = orderItemId,
                        SqlDbType = SqlDbType.Int
                    });

                    await cmd.ExecuteScalarAsync();
                }
            }
        }

        public async Task UpdateBenutzungskopie(int orderItemId, bool? benutzungskopie)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "UPDATE OrderItem SET Benutzungskopie = (@p1) WHERE ID = (@p2)";
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "p1",
                        Value = ToDb(benutzungskopie),
                        SqlDbType = SqlDbType.Bit
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "p2",
                        Value = orderItemId,
                        SqlDbType = SqlDbType.Int
                    });

                    await cmd.ExecuteScalarAsync();
                }
            }
        }

        public async Task UpdateBenutzungskopieStatus(int orderItemId, GebrauchskopieStatus gebrauchskopieStatus)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "UPDATE OrderItem SET GebrauchskopieStatus = (@p1) WHERE ID = (@p2)";
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "p1",
                        Value = gebrauchskopieStatus,
                        SqlDbType = SqlDbType.Int
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "p2",
                        Value = orderItemId,
                        SqlDbType = SqlDbType.Int
                    });

                    await cmd.ExecuteScalarAsync();
                }
            }
        }

        public async Task UpdateReason(int orderItemId, int? reasonId, bool hasPersonendaten, string userId)
        {
            await EnsureLegalUser(orderItemId, userId);

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "UPDATE OrderItem SET Reason = (@p1), HasPersonendaten = (@p3) WHERE ID = (@p2)";
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "p1",
                        Value = reasonId.HasValue ? (object) reasonId.Value : DBNull.Value,
                        SqlDbType = SqlDbType.Int
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "p2",
                        Value = orderItemId,
                        SqlDbType = SqlDbType.Int
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "p3",
                        Value = hasPersonendaten ? 1 : 0,
                        SqlDbType = SqlDbType.Bit
                    });

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task UpdateOrderDetail(UpdateOrderDetailData data)
        {
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    using (var cmd = connection.CreateCommand())
                    {
                        var item = data.OrderItem;
                        cmd.CommandText = "UPDATE OrderItem SET Comment = @p1, BewilligungsDatum = @p2, HasPersonendaten = @p3, " +
                                          "Reason = @p4, DigitalisierungsKategorie = @p5, TerminDigitalisierung = @p6, InternalComment = @p7, " +
                                          "Ausleihdauer = @p10, AnzahlMahnungen = @p11, MahndatumInfo = @p12, GebrauchskopieStatus = @p13 " +
                                          "WHERE Id = @p8";

                        cmd.AddParameter("p1", SqlDbType.NVarChar, ToDb(item.Comment));
                        cmd.AddParameter("p2", SqlDbType.DateTime, ToDb(item.BewilligungsDatum));
                        cmd.AddParameter("p3", SqlDbType.Bit, ToDb(item.HasPersonendaten));
                        cmd.AddParameter("p4", SqlDbType.Int, ToDb(item.Reason));
                        cmd.AddParameter("p5", SqlDbType.Int, ToDb(item.DigitalisierungsKategorie));
                        cmd.AddParameter("p6", SqlDbType.DateTime, ToDb(item.TerminDigitalisierung));
                        cmd.AddParameter("p7", SqlDbType.NVarChar, ToDb(item.InternalComment));
                        cmd.AddParameter("p10", SqlDbType.Int, ToDb(item.Ausleihdauer));
                        cmd.AddParameter("p11", SqlDbType.Int, ToDb(item.AnzahlMahnungen));
                        cmd.AddParameter("p12", SqlDbType.NVarChar, ToDb(item.MahndatumInfo));
                        cmd.AddParameter("p13", SqlDbType.Int, ToDb(item.GebrauchskopieStatus));

                        cmd.AddParameter("p8", SqlDbType.Int, ToDb(item.Id));

                        await cmd.ExecuteNonQueryAsync();
                    }

                    using (var cmd = connection.CreateCommand())
                    {
                        var ordering = data.Ordering;
                        cmd.CommandText =
                            "UPDATE Ordering SET UserId = @p1, ArtDerArbeit = @p2, LesesaalDate = @p3, BegruendungEinsichtsgesuch = @p4, " +
                            "HasEigenePersonendaten = @p5, PersonenbezogeneNachforschung = @p6 WHERE Id = @p7";

                        cmd.AddParameter("p1", SqlDbType.NVarChar, ToDb(ordering.UserId));
                        cmd.AddParameter("p2", SqlDbType.Int, ToDb(ordering.ArtDerArbeit));
                        cmd.AddParameter("p3", SqlDbType.Date, ToDb(ordering.LesesaalDate));
                        cmd.AddParameter("p4", SqlDbType.NVarChar, ToDb(ordering.BegruendungEinsichtsgesuch));
                        cmd.AddParameter("p5", SqlDbType.Bit, ToDb(ordering.HasEigenePersonendaten));
                        cmd.AddParameter("p6", SqlDbType.Bit, ToDb(ordering.PersonenbezogeneNachforschung));

                        cmd.AddParameter("p7", SqlDbType.Int, ToDb(ordering.Id));

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                scope.Complete();
            }
        }

        public async Task<IEnumerable<OrderItem>> GetBasket(string userId)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM OrderItem WHERE OrderItem.OrderId IN " +
                                      "(SELECT ID FROM Ordering WHERE UserId = @p1 AND Type = @p2)";
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "p1",
                        Value = userId,
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "p2",
                        Value = (int) OrderType.Bestellkorb,
                        SqlDbType = SqlDbType.Int
                    });

                    var orderItems = new List<OrderItem>();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var item = OrderItemFromReader(reader);
                            orderItems.Add(item);
                        }
                    }

                    return orderItems;
                }
            }
        }

        public async Task<int> CreateOrderFromBasket(OrderCreationRequest orderCreationRequest)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var orderingId = await GetBasketId(orderCreationRequest.CurrentUserId, connection);
                var updateRequestParams = new UpdateOrderingParams
                {
                    ArtDerArbeit = orderCreationRequest.ArtDerArbeit,
                    BegruendungEinsichtsgesuch = orderCreationRequest.BegruendungEinsichtsgesuch,
                    Comment = orderCreationRequest.Comment,
                    LesesaalDate = orderCreationRequest.LesesaalDate,
                    Type = orderCreationRequest.Type,
                    UserId = orderCreationRequest.CurrentUserId,
                    PersonenbezogeneNachforschung = orderCreationRequest.PersonenbezogeneNachforschung,
                    HasEigenePersonendaten = orderCreationRequest.HasEigenePersonendaten
                };

                await UpdateOrderingFromBasket(updateRequestParams, connection);

                if (orderCreationRequest.OrderItemIdsToExclude.Count > 0)
                {
                    var basketId = await CreateBasket(orderCreationRequest.CurrentUserId, connection);

                    await MoveToBasket(basketId, orderCreationRequest.OrderItemIdsToExclude, connection);
                }

                return orderingId ?? -1;
            }
        }

        public async Task ChangeUserForOrdering(int orderingId, string newUserId)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText =
                        "UPDATE Ordering SET UserId = @p1, eingangsart = 1, RolePublicClient = (select rolePublicClient from ApplicationUser where id = @p1)  WHERE ID = @p2";

                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "p1",
                        Value = ToDb(newUserId),
                        SqlDbType = SqlDbType.VarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "p2",
                        Value = ToDb(orderingId),
                        SqlDbType = SqlDbType.Int
                    });

                    await cmd.ExecuteScalarAsync();
                }
            }
        }

        public async Task<IEnumerable<Ordering>> GetOrderings(string userId)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                var orderings = new List<Ordering>();
                var items = new List<OrderItem>();

                using (var cmd = connection.CreateCommand())
                {
                    // ToDo: Translate Art der Arbeit
                    cmd.CommandText = "SELECT ID, Type, UserId, Comment, ArtDerArbeit, lesesaalDate, OrderDate, " +
                                      "(SELECT Name_de FROM ArtDerArbeit WHERE ArtDerArbeit.ID = Ordering.ArtDerArbeit) ArtDerArbeit, " +
                                      "BegruendungEinsichtsgesuch, PersonenbezogeneNachforschung, HasEigenePersonendaten, RolePublicClient FROM Ordering WHERE UserId = @p1 AND Type <> @p2 ";

                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "p1",
                        Value = userId,
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "p2",
                        Value = (int) OrderType.Bestellkorb,
                        SqlDbType = SqlDbType.Int
                    });

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            orderings.Add(OrderingFromReader(reader));
                        }
                    }
                }

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM OrderItem WHERE OrderItem.OrderId IN " +
                                      "(SELECT ID FROM Ordering WHERE UserId = @p1 AND Type <> @p2 )";
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "p1",
                        Value = userId,
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "p2",
                        Value = (int) OrderType.Bestellkorb,
                        SqlDbType = SqlDbType.Int
                    });

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            items.Add(OrderItemFromReader(reader));
                        }
                    }
                }

                foreach (var ordering in orderings)
                {
                    ordering.Items = items.Where(i => i.OrderId == ordering.Id).ToArray();
                }

                return orderings;
            }
        }
        
        public async Task<List<PrimaerdatenAufbereitungItem>> GetPrimaerdatenaufbereitungItemsByDate(DateTime startTime, DateTime endTime)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var primaerdatenaufbereitungItems = new List<PrimaerdatenAufbereitungItem>();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT * " +
                                      "FROM " +
                                      "v_PrimaerdatenAufbereitung " +
                                      "WHERE AuftragErledigt between @startTime AND @endTime";

                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "startTime",
                        Value = startTime,
                        SqlDbType = SqlDbType.DateTime
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "endTime",
                        Value = endTime,
                        SqlDbType = SqlDbType.DateTime
                    });
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            primaerdatenaufbereitungItems.Add(PrimaerdatenAufbereitungItemFromReader(reader));
                        }
                    }
                }
                
                return primaerdatenaufbereitungItems;
            }
        }

        public async Task<Ordering> GetOrdering(int orderingId, bool includeOrderItems = true)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                var ordering = new Ordering();
                var items = new List<OrderItem>();

                using (var cmd = connection.CreateCommand())
                {
                    // ToDo: Translate Art der Arbeit
                    cmd.CommandText = "SELECT ID, Type, UserId, Comment, ArtDerArbeit, lesesaalDate, OrderDate, " +
                                      "(SELECT Name_de FROM ArtDerArbeit WHERE ArtDerArbeit.ID = Ordering.ArtDerArbeit) ArtDerArbeit, " +
                                      "BegruendungEinsichtsgesuch, PersonenbezogeneNachforschung, HasEigenePersonendaten, RolePublicClient FROM Ordering WHERE Id = @p1";

                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "p1",
                        Value = orderingId,
                        SqlDbType = SqlDbType.Int
                    });

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            ordering = OrderingFromReader(reader);
                        }
                    }
                }

                if (includeOrderItems)
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT * FROM OrderItem WHERE OrderItem.OrderId = @p1 ";
                        cmd.Parameters.Add(new SqlParameter
                        {
                            ParameterName = "p1",
                            Value = orderingId,
                            SqlDbType = SqlDbType.Int
                        });

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                items.Add(OrderItemFromReader(reader));
                            }
                        }
                    }

                    ordering.Items = items.Where(i => i.OrderId == ordering.Id).ToArray();
                }

                return ordering;
            }
        }

        public async Task<OrderItem> GetOrderItem(int orderItemId)
        {
            OrderItem retVal = null;
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM OrderItem WHERE ID = @p1 ";
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "p1",
                        Value = orderItemId,
                        SqlDbType = SqlDbType.Int
                    });

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            await reader.ReadAsync();
                            retVal = OrderItemFromReader(reader);
                        }
                    }
                }
            }

            return retVal;
        }

        public async Task<int> UpdateOrderItem(OrderItem orderItem)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"UPDATE OrderItem
	                                    SET    OrderId = @OrderId, Ve = @VeId, Reason = @Reason, Comment = @Comment, Status = @Status, Bestand = @Bestand, Ablieferung = @Ablieferung, 
                                               BehaeltnisNummer = @BehaeltnisNummer, Aktenzeichen = @Aktenzeichen, Dossiertitel = @Dossiertitel, ZeitraumDossier = @ZeitraumDossier, 
                                               HasPersonendaten = @HasPersonendaten, BewilligungsDatum = @BewilligungsDatum, ArchivNummer = @ArchivNummer, Standort = @Standort, 
                                               Signatur = @Signatur, Darin = @Darin, ZusaetzlicheInformationen = @ZusaetzlicheInformationen, Hierarchiestufe = @Hierarchiestufe, 
                                               Schutzfristverzeichnung = @Schutzfristverzeichnung, ZugaenglichkeitGemaessBga = @ZugaenglichkeitGemaessBga, Publikationsrechte = @Publikationsrechte, 
                                               Behaeltnistyp = @Behaeltnistyp, ZustaendigeStelle = @ZustaendigeStelle, IdentifikationDigitalesMagazin = @IdentifikationDigitalesMagazin, 
                                               ApproveStatus = @ApproveStatus, DigitalisierungsKategorie = @DigitalisierungsKategorie,
                                               TerminDigitalisierung = @TerminDigitalisierung, InternalComment = @InternalComment, DatumDesEntscheids = @DatumDesEntscheids, EntscheidGesuch = @EntscheidGesuch, 
                                               Ausgabedatum = @Ausgabedatum, Abschlussdatum = @Abschlussdatum, Abbruchgrund = @Abbruchgrund, DatumDerFreigabe = @DatumDerFreigabe, SachbearbeiterId = @SachbearbeiterId,
                                               AnzahlMahnungen = @AnzahlMahnungen, Ausleihdauer = @Ausleihdauer, MahndatumInfo = @MahndatumInfo, HasAufbereitungsfehler = @HasAufbereitungsfehler
	                                    WHERE  ID = @ID";

                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "OrderId",
                        Value = orderItem.OrderId,
                        SqlDbType = SqlDbType.Int
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "VeId",
                        Value = ToDb(orderItem.VeId),
                        SqlDbType = SqlDbType.Int
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "Reason",
                        Value = ToDb(orderItem.Reason),
                        SqlDbType = SqlDbType.Int
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "Comment",
                        Value = ToDb(orderItem.Comment),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "Status",
                        Value = ToDb(orderItem.Status),
                        SqlDbType = SqlDbType.Int
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "Bestand",
                        Value = ToDb(orderItem.Bestand),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "Ablieferung",
                        Value = ToDb(orderItem.Ablieferung),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "BehaeltnisNummer",
                        Value = ToDb(orderItem.BehaeltnisNummer),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "Aktenzeichen",
                        Value = ToDb(orderItem.Aktenzeichen),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "Dossiertitel",
                        Value = ToDb(orderItem.Dossiertitel),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "ZeitraumDossier",
                        Value = ToDb(orderItem.ZeitraumDossier),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "HasPersonendaten",
                        Value = ToDb(orderItem.HasPersonendaten),
                        SqlDbType = SqlDbType.Bit
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "BewilligungsDatum",
                        Value = ToDb(orderItem.BewilligungsDatum),
                        SqlDbType = SqlDbType.DateTime
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "ArchivNummer",
                        Value = ToDb(orderItem.ArchivNummer),
                        SqlDbType = SqlDbType.NVarChar
                    });

                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "Standort",
                        Value = ToDb(orderItem.Standort),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "Signatur",
                        Value = ToDb(!string.IsNullOrEmpty(orderItem.Signatur) ? orderItem.Signatur : GetFormOrderSignatur(orderItem)),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "Darin",
                        Value = ToDb(orderItem.Darin),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "ZusaetzlicheInformationen",
                        Value = ToDb(orderItem.ZusaetzlicheInformationen),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "Hierarchiestufe",
                        Value = ToDb(orderItem.Hierarchiestufe),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "Schutzfristverzeichnung",
                        Value = ToDb(orderItem.Schutzfristverzeichnung),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "ZugaenglichkeitGemaessBga",
                        Value = ToDb(orderItem.ZugaenglichkeitGemaessBga),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "Publikationsrechte",
                        Value = ToDb(orderItem.Publikationsrechte),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "Behaeltnistyp",
                        Value = ToDb(orderItem.Behaeltnistyp),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "ZustaendigeStelle",
                        Value = ToDb(orderItem.ZustaendigeStelle),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "IdentifikationDigitalesMagazin",
                        Value = ToDb(orderItem.IdentifikationDigitalesMagazin),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "ApproveStatus",
                        Value = ToDb(orderItem.ApproveStatus),
                        SqlDbType = SqlDbType.Int
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "DigitalisierungsKategorie",
                        Value = ToDb(orderItem.DigitalisierungsKategorie),
                        SqlDbType = SqlDbType.Int
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "TerminDigitalisierung",
                        Value = ToDb(orderItem.TerminDigitalisierung),
                        SqlDbType = SqlDbType.DateTime
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "InternalComment",
                        Value = ToDb(orderItem.InternalComment),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "ID",
                        Value = orderItem.Id,
                        SqlDbType = SqlDbType.Int
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "DatumDesEntscheids",
                        Value = ToDb(orderItem.DatumDesEntscheids),
                        SqlDbType = SqlDbType.DateTime
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "EntscheidGesuch",
                        Value = (int) orderItem.EntscheidGesuch,
                        SqlDbType = SqlDbType.Int
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "Ausgabedatum",
                        Value = ToDb(orderItem.Ausgabedatum),
                        SqlDbType = SqlDbType.DateTime
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "Abschlussdatum",
                        Value = ToDb(orderItem.Abschlussdatum),
                        SqlDbType = SqlDbType.DateTime
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "Abbruchgrund",
                        Value = ToDb((int) orderItem.Abbruchgrund),
                        SqlDbType = SqlDbType.Int
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "DatumDerFreigabe",
                        Value = ToDb(orderItem.DatumDerFreigabe),
                        SqlDbType = SqlDbType.DateTime2
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "SachbearbeiterId",
                        Value = ToDb(orderItem.SachbearbeiterId),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "AnzahlMahnungen",
                        Value = ToDb(orderItem.AnzahlMahnungen),
                        SqlDbType = SqlDbType.Int
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "Ausleihdauer",
                        Value = ToDb(orderItem.Ausleihdauer),
                        SqlDbType = SqlDbType.Int
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "MahndatumInfo",
                        Value = ToDb(orderItem.MahndatumInfo),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "HasAufbereitungsfehler",
                        Value = ToDb(orderItem.HasAufbereitungsfehler),
                        SqlDbType = SqlDbType.Bit
                    });
                    var recordsAffected = await cmd.ExecuteNonQueryAsync();
                    return recordsAffected;
                }
            }
        }

        public async Task AddStatusHistoryRecord(int orderItemId, OrderStatesInternal from, OrderStatesInternal to, string changedBy)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO [dbo].[StatusHistory] ([OrderItemId], [FromStatus], [ToStatus], [ChangedBy])
                            Values(@OrderItemId, @FromStatus, @ToStatus, @ChangedBy)";

                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "OrderItemId",
                        Value = ToDb(orderItemId),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "FromStatus",
                        Value = from,
                        SqlDbType = SqlDbType.Int
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "ToStatus",
                        Value = to,
                        SqlDbType = SqlDbType.Int
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "ChangedBy",
                        Value = ToDb(changedBy),
                        SqlDbType = SqlDbType.NVarChar
                    });

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task AddApproveHistoryRecord(int orderItemId, string approvedToUser, OrderType orderType, ApproveStatus from, ApproveStatus to,
            string changedBy)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText =
                        @"INSERT INTO [dbo].[ApproveStatusHistory] ([OrderItemId], [OrderType], [ApprovedTo], [ApprovedFrom], [ApproveFromStatus], [ApproveToStatus])
	                                    Values(@OrderItemId, @OrderType, @ApprovedTo, @ApprovedFrom, @ApproveFromStatus, @ApproveToStatus)";

                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "OrderItemId",
                        Value = ToDb(orderItemId),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "OrderType",
                        Value = orderType,
                        SqlDbType = SqlDbType.Int
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "ApprovedTo",
                        Value = ToDb(approvedToUser),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "ApprovedFrom",
                        Value = ToDb(changedBy),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "ApproveToStatus",
                        Value = to,
                        SqlDbType = SqlDbType.Int
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "ApproveFromStatus",
                        Value = ToDb(from),
                        SqlDbType = SqlDbType.Int
                    });

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<StatusHistory[]> GetStatusHistoryForOrderItem(int orderItemId)
        {
            var statusHistory = new List<StatusHistory>();

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM StatusHistory WHERE OrderItemId = @p1";
                    cmd.Parameters.AddWithValue("@p1", orderItemId);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            statusHistory.Add(StatusHistoryFromReader(reader));
                        }
                    }

                    return statusHistory.ToArray();
                }
            }
        }

        public async Task<List<Bestellhistorie>> GetOrderingHistoryForVe(int veId)
        {
            var orderingHistory = new List<Bestellhistorie>();

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT i.ID as AuftragsId,
                                        o.[Type] as AuftragsTyp,
                                        ApproveStatus as Freigabestatus,
                                        DatumDerFreigabe,
                                        EntscheidGesuch,
                                        IIF (DatumDesEntscheids IS NOT NULL, DatumDesEntscheids, BewilligungsDatum) as DatumDesEntscheids, 
                                        (SELECT u.FamilyName + ', ' + u.Firstname + IIF(u.Organization IS NOT NULL, ', ' + u.Organization, '') FROM ApplicationUser u WHERE u.ID = o.UserId) As Besteller,
                                        InternalComment as InterneBemerkung,
                                        (SELECT u.FamilyName + ', ' + u.Firstname FROM ApplicationUser u WHERE u.ID = i.SachbearbeiterId) As Sachbearbeiter
                                        FROM OrderItem i
                                        INNER JOIN Ordering o ON o.Id = i.OrderId
                                        WHERE Ve = @p1 AND (ApproveStatus > 0 OR EntscheidGesuch > 0) ORDER BY o.OrderDate DESC";

                    cmd.Parameters.AddWithValue("@p1", veId);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            orderingHistory.Add(BestellhistorieFromReader(reader));
                        }
                    }

                    return orderingHistory;
                }
            }
        }

        public async Task AddToOrderExecutedWaitList(int veId, string serializedMessage)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO OrderExecutedWaitList (VeId, SerializedMessage) VALUES (@veId, @message)";

                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "veId",
                        Value = veId,
                        SqlDbType = SqlDbType.Int
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "message",
                        Value = serializedMessage,
                        SqlDbType = SqlDbType.NVarChar
                    });
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task MarkOrderAsProcessedInWaitList(int waitListId)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText =
                        "Update OrderExecutedWaitList set processed = 1, processedDate = getdate() where OrderExecutedWaitListId = @waitListId";

                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "waitListId",
                        Value = waitListId,
                        SqlDbType = SqlDbType.Int
                    });

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<List<OrderExecutedWaitList>> GetVeFromOrderExecutedWaitList(int veId)
        {
            var waitList = new List<OrderExecutedWaitList>();

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "Select * from OrderExecutedWaitList where VeId = @veId and processed = 0";

                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "veId",
                        Value = veId,
                        SqlDbType = SqlDbType.Int
                    });

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            waitList.Add(OrderExecutedWaitListFromReader(reader));
                        }
                    }

                    return waitList;
                }
            }
        }

        public Task<List<OrderItemByUser>> GetOrderItemsByUser(int[] orderItemIds)
        {
            var retVal = new List<OrderItemByUser>();
            using (var context = new ViaducContext(connectionString))
            {
                var orderItemsByUser = context.OrderingFlatItem.Where(o => orderItemIds.Contains(o.ItemId)).GroupBy(o => o.UserId);
                foreach (var orderItemByUser in orderItemsByUser)
                {
                    var newUserItem = new OrderItemByUser
                    {
                        UserId = orderItemByUser.Key,
                        OrderItemIds = orderItemByUser.Select(o => o.ItemId).ToList()
                    };
                    retVal.Add(newUserItem);
                }
            }

            return Task.FromResult(retVal);
        }

        public async Task<OrderItem[]> FindOrderItems(int[] orderItemIds)
        {
            var orderItems = new List<OrderItem>();

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText =
                        $@"(SELECT * FROM OrderItem WHERE OrderItem.ID IN ({
                                string.Join(",", orderItemIds)
                            }))";

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            orderItems.Add(OrderItemFromReader(reader));
                        }
                    }

                    return orderItems.ToArray();
                }
            }
        }

        public async Task<DigipoolEntry[]> GetDigipool()
        {
            var digipool = new List<DigipoolEntry>();

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText =
                        $@"SELECT OrderItem.ID,
                          (SELECT IIF(FamilyName IS NOT NULL, FamilyName + ', ', '') + IIF(Firstname IS NOT NULL, Firstname, '') + IIF(Organization IS NOT NULL, ', ' + Organization, '') FROM ApplicationUser WHERE ApplicationUser.ID = Ordering.UserId) AS [User],
	                      Signatur, 
	                      DossierTitel,
	                      TerminDigitalisierung, 
	                      DigitalisierungsKategorie, 
                          OrderDate,
                          Ve,
                          Ordering.UserId as UserId,
                          Ordering.Comment AS OrderingComment,
                          OrderItem.Comment AS OrderItemComment,
	                      OrderItem.InternalComment,
                          OrderItem.ApproveStatus,
                          OrderItem.HasAufbereitungsfehler
                        FROM OrderItem
                        INNER JOIN Ordering ON OrderItem.OrderId = Ordering.ID
                        WHERE Status = {(int) OrderStatesInternal.FuerDigitalisierungBereit}";

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var digipoolEntry = new DigipoolEntry
                            {
                                OrderItemId = Convert.ToInt32(reader["ID"]),
                                User = reader["User"] as string,
                                UserId = reader["UserId"] as string,
                                Signatur = reader["Signatur"] as string,
                                DossierTitel = reader["DossierTitel"] as string,
                                TerminDigitalisierung = Convert.ToDateTime(reader["TerminDigitalisierung"]),
                                Digitalisierunskategorie = Convert.ToInt32(reader["DigitalisierungsKategorie"]),
                                OrderDate = Convert.ToDateTime(reader["OrderDate"]),
                                VeId = ToInt32Opt(reader["Ve"]),
                                OrderingComment = reader["OrderingComment"] as string,
                                OrderItemComment = reader["OrderItemComment"] as string,
                                InternalComment = reader["InternalComment"] as string,
                                ApproveStatus = (ApproveStatus) Convert.ToInt32(reader["ApproveStatus"]),
                                HasAufbereitungsfehler = Convert.ToBoolean(reader["HasAufbereitungsfehler"])
                            };

                            digipool.Add(digipoolEntry);
                        }
                    }

                    return digipool.ToArray();
                }
            }
        }

        public async Task<List<DigitalisierungsTermin>> GetLatestDigitalisierungsTermine(string userId, DateTime fromDate,
            DigitalisierungsKategorie kategorie)
        {
            var retValue = new List<DigitalisierungsTermin>();

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText =
                        $@"SELECT MAX(TerminDigitalisierung) AS Termin,  COUNT(*) as Count
                             FROM OrderItem
                             INNER JOIN Ordering ON OrderItem.OrderId = Ordering.ID
                             WHERE Status NOT IN ( {(int) OrderStatesInternal.Abgebrochen}, {(int) OrderStatesInternal.DigitalisierungAbgebrochen} ) AND 
                                   UserId = @UserId AND
                                   DigitalisierungsKategorie = @Kategorie AND
                                   TerminDigitalisierung IS NOT NULL
                             GROUP BY CAST(TerminDigitalisierung AS DATE)
                             HAVING CAST(TerminDigitalisierung AS DATE) >= @FromDate
                             ORDER BY CAST(TerminDigitalisierung AS DATE)";

                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "UserId",
                        Value = userId,
                        SqlDbType = SqlDbType.NVarChar
                    });

                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "FromDate",
                        Value = fromDate,
                        SqlDbType = SqlDbType.Date
                    });

                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "Kategorie",
                        Value = kategorie,
                        SqlDbType = SqlDbType.Int
                    });

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var termin = new DigitalisierungsTermin
                            {
                                Termin = Convert.ToDateTime(reader["Termin"]),
                                AnzahlAuftraege = Convert.ToInt32(reader["Count"])
                            };

                            retValue.Add(termin);
                        }
                    }

                    return retValue;
                }
            }
        }


        /// <summary>
        ///     Liest die Individuellen Access Tokens einer VE von der SQL Datenbank
        /// </summary>
        /// <param name="veId">Id der Ve</param>
        /// <param name="ignoreOrderItemId">
        ///     Hier kann eine OrderItemId angegeben werden, welche ignoriert wird. Ist wichtig beim Zurücksetzen
        ///     eines Auftrags, weil sonst die Berechtigung des zurückgesetzten Auftrags immer noch "zieht" und der Auftrag
        ///     automatisch freigegeben wird, obwohl er das nicht dürfte.
        /// </param>
        /// <returns></returns>
        public async Task<IndivTokens> GetIndividualAccessTokens(int veId, int ignoreOrderItemId = -1)
        {
            var downloadTokens = new HashSet<string>();
            var fulltextTokens = new HashSet<string>();
            var fieldAccessTokens = new HashSet<string>();

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.Parameters.Add(
                        new SqlParameter
                        {
                            ParameterName = "pVeId",
                            Value = veId,
                            SqlDbType = SqlDbType.Int
                        }
                    );

                    cmd.Parameters.Add(
                        new SqlParameter
                        {
                            ParameterName = "pIgnoreOrderItemId",
                            Value = ignoreOrderItemId,
                            SqlDbType = SqlDbType.Int
                        }
                    );

                    cmd.CommandText =
                        $@"select 
                           orderitem.ve as Ve, 
                           applicationUser.ID as UserId, 
                           applicationUser.ResearcherGroup as ResearcherGroup, 
                           OrderItem.ApproveStatus as ApproveStatus, 
                           OrderItem.EntscheidGesuch as EntscheidGesuch,
                           ordering.Type as Type
                        from orderitem 
                           inner join ordering on orderitem.OrderId = ordering.ID
	                       inner join applicationUser on ordering.UserId = ApplicationUser.ID
                        where 
                           orderItem.Ve = @pVeId
                           AND orderItem.Id <> @pIgnoreOrderItemId
                           AND (entscheidgesuch in ({(int) EntscheidGesuch.AuskunftsgesuchBewilligt}, {(int) EntscheidGesuch.EinsichtsgesuchBewilligt} ) OR ApproveStatus in ({(int) ApproveStatus.FreigegebenAusserhalbSchutzfrist}, {(int) ApproveStatus.FreigegebenInSchutzfrist}))";

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var orderType = (OrderType) Convert.ToInt32(reader["Type"]);
                            var freigabeStatus = (ApproveStatus) Convert.ToInt32(reader["ApproveStatus"]);
                            var entscheidGesuch = (EntscheidGesuch) Convert.ToInt32(reader["EntscheidGesuch"]);
                            var dds = Convert.ToInt32(reader["ResearcherGroup"]) == 1;
                            var userId = reader["UserId"] as string;

                            switch (orderType)
                            {
                                case OrderType.Einsichtsgesuch:
                                    switch (entscheidGesuch)
                                    {
                                        case EntscheidGesuch.AuskunftsgesuchBewilligt:
                                        case EntscheidGesuch.EinsichtsgesuchBewilligt:
                                            downloadTokens.Add(dds ? "DDS" : "EB_" + userId);
                                            fulltextTokens.Add(dds ? "DDS" : "EB_" + userId);
                                            fieldAccessTokens.Add(dds ? "DDS" : "EB_" + userId);
                                            break;
                                    }

                                    break;

                                case OrderType.Digitalisierungsauftrag:
                                case OrderType.Lesesaalausleihen:
                                case OrderType.Verwaltungsausleihe:
                                    switch (freigabeStatus)
                                    {
                                        case ApproveStatus.FreigegebenAusserhalbSchutzfrist:
                                            downloadTokens.Add(dds ? "DDS" : "FG_" + userId);
                                            break;
                                        case ApproveStatus.FreigegebenInSchutzfrist:
                                            downloadTokens.Add(dds ? "DDS" : "EB_" + userId);
                                            fulltextTokens.Add(dds ? "DDS" : "EB_" + userId);
                                            fieldAccessTokens.Add(dds ? "DDS" : "EB_" + userId);
                                            break;
                                    }

                                    break;
                            }
                        }
                    }
                }
            }

            return new IndivTokens(fulltextTokens.ToArray(), downloadTokens.ToArray(), fieldAccessTokens.ToArray());
        }

        public async Task<bool> IsUniqueVeInBasket(int veId, string userId)
        {
            if (veId == 0 || string.IsNullOrEmpty(userId))
            {
                return false;
            }

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                var basketId = await GetBasketId(userId, connection);
                if (!basketId.HasValue)
                {
                    return true;
                }

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM OrderItem WHERE OrderId = @p1 AND Ve = @p2";
                    cmd.Parameters.Add(
                        new SqlParameter
                        {
                            ParameterName = "p1",
                            Value = basketId,
                            SqlDbType = SqlDbType.Int
                        }
                    );
                    cmd.Parameters.Add(
                        new SqlParameter
                        {
                            ParameterName = "p2",
                            Value = veId,
                            SqlDbType = SqlDbType.Int
                        }
                    );

                    return Convert.ToInt32(await cmd.ExecuteScalarAsync()) == 0;
                }
            }
        }


        public async Task UpdateDigipool(List<int> orderItemIds, int? digitalisierungsKategorie, DateTime? terminDigitalisierung)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                if (digitalisierungsKategorie.HasValue && digitalisierungsKategorie.Value > 0)
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText =
                            $"UPDATE OrderItem SET DigitalisierungsKategorie = (@p1), HasAufbereitungsfehler = 0 WHERE Id IN ({string.Join(",", orderItemIds)}) ";
                        cmd.Parameters.Add(new SqlParameter
                        {
                            ParameterName = "p1",
                            Value = ToDb(digitalisierungsKategorie),
                            SqlDbType = SqlDbType.Int
                        });

                        await cmd.ExecuteScalarAsync();
                    }
                }

                if (terminDigitalisierung.HasValue)
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "UPDATE OrderItem SET TerminDigitalisierung = (@p1) " +
                                          $"WHERE Id IN (${string.Join(", ", orderItemIds)}) AND " +
                                          $"DigitalisierungsKategorie IN ({(int) DigitalisierungsKategorie.Spezial}, {(int) DigitalisierungsKategorie.Termin})";
                        cmd.Parameters.Add(new SqlParameter
                        {
                            ParameterName = "p1",
                            Value = ToDb(terminDigitalisierung),
                            SqlDbType = SqlDbType.DateTime
                        });

                        await cmd.ExecuteScalarAsync();
                    }
                }
            }
        }

        public async Task UpdateTermin(int orderItemId, DateTime terminDigitalisierung)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "UPDATE OrderItem SET TerminDigitalisierung = @p1 WHERE Id = @p2";
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "p1",
                        Value = ToDb(terminDigitalisierung),
                        SqlDbType = SqlDbType.DateTime
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "p2",
                        Value = ToDb(orderItemId),
                        SqlDbType = SqlDbType.Int
                    });

                    await cmd.ExecuteScalarAsync();
                }
            }
        }

        public async Task<bool> HasEinsichtsbewilligung(int veId)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.Parameters.Add(
                        new SqlParameter
                        {
                            ParameterName = "pVeId",
                            Value = veId,
                            SqlDbType = SqlDbType.Int
                        }
                    );

                    cmd.CommandText =
                        $@"select count(*)                           
                           from OrderItem 
                           where OrderItem.Ve = @pVeId AND
                                 Entscheidgesuch = {(int) EntscheidGesuch.EinsichtsgesuchBewilligt}";

                    var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                    return count != 0;
                }
            }
        }

        /// <returns>Ordering.ID</returns>
        private async Task<int> CreateBasketIfNecessary(string userId)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                await lockSemaphore.WaitAsync();
                try
                {
                    var basketId = await GetBasketId(userId, connection);

                    if (basketId == null)
                    {
                        basketId = await CreateBasket(userId, connection);
                    }

                    return (int) basketId;
                }
                finally
                {
                    lockSemaphore.Release();
                }
            }
        }

        /// <returns>Ordering.ID</returns>
        private async Task<int> CreateBasket(string userId, SqlConnection connection)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText =
                    "INSERT INTO Ordering (UserId, RolePublicClient) OUTPUT INSERTED.ID VALUES (@p1, (select rolePublicClient from ApplicationUser where id = @p1))";

                cmd.Parameters.Add(new SqlParameter
                {
                    ParameterName = "p1",
                    Value = userId,
                    SqlDbType = SqlDbType.NVarChar
                });

                return Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }
        }

        private async Task<int?> GetBasketId(string userId, SqlConnection connection)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT ID FROM Ordering WHERE UserId = @p1 AND Type = @p2 ";
                cmd.Parameters.Add(new SqlParameter
                {
                    ParameterName = "p1",
                    Value = userId,
                    SqlDbType = SqlDbType.NVarChar
                });
                cmd.Parameters.Add(new SqlParameter
                {
                    ParameterName = "p2",
                    Value = (int) OrderType.Bestellkorb,
                    SqlDbType = SqlDbType.Int
                });

                return (int?) await cmd.ExecuteScalarAsync();
            }
        }

        /// <returns>OrderItem.ID</returns>
        private async Task<int> AddToBasket(OrderingIndexSnapshot indexSnapshot, int basketId)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText =
                        "INSERT INTO OrderItem (OrderId, Ve, Status, BehaeltnisNummer, Dossiertitel, ZeitraumDossier, Standort, Signatur, Darin, ZusaetzlicheInformationen, Hierarchiestufe, Schutzfristverzeichnung, ZugaenglichkeitGemaessBGA, Publikationsrechte, Behaeltnistyp, ZustaendigeStelle, IdentifikationDigitalesMagazin, Aktenzeichen) OUTPUT INSERTED.ID " +
                        "VALUES (@basketId, " +
                        "@veId, " +
                        "0, " +
                        "@behaeltnisnr, " +
                        "@titel, " +
                        "@erstellungsZeitraum, " +
                        "@standort, " +
                        "@signatur, " +
                        "@darin, " +
                        "@zusaetzlicheInformationen, " +
                        "@hierarchieStufe, " +
                        "@schutzfristverzeichnung," +
                        "@zugaenglichkeitGemaessBga, " +
                        "@publikationsrechte, " +
                        "@behaeltnistyp, " +
                        "@zustaendigeStelle, " +
                        "@identifikationDigitalesMagazin, " +
                        "@aktenzeichen)";

                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "basketId",
                        Value = basketId,
                        SqlDbType = SqlDbType.Int
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "veId",
                        Value = ToDb(indexSnapshot.VeId),
                        SqlDbType = SqlDbType.Int
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "behaeltnisnr",
                        Value = ToDb(indexSnapshot.BehaeltnisCode),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "titel",
                        Value = ToDb(indexSnapshot.Dossiertitel),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "erstellungsZeitraum",
                        Value = ToDb(indexSnapshot.ZeitraumDossier),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "standort",
                        Value = ToDb(indexSnapshot.Standort),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "signatur",
                        Value = ToDb(indexSnapshot.Signatur),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "darin",
                        Value = ToDb(indexSnapshot.Darin),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "zusaetzlicheInformationen",
                        Value = ToDb(indexSnapshot.ZusaetzlicheInformationen),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "hierarchieStufe",
                        Value = ToDb(indexSnapshot.Hierarchiestufe),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "schutzfristverzeichnung",
                        Value = ToDb(indexSnapshot.Schutzfristverzeichnung),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "zugaenglichkeitGemaessBga",
                        Value = ToDb(indexSnapshot.ZugaenglichkeitGemaessBga),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "publikationsrechte",
                        Value = ToDb(indexSnapshot.Publikationsrechte),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "behaeltnistyp",
                        Value = ToDb(indexSnapshot.Behaeltnistyp),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "zustaendigeStelle",
                        Value = ToDb(indexSnapshot.ZustaendigeStelle),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "identifikationDigitalesMagazin",
                        Value = ToDb(indexSnapshot.IdentifikationDigitalesMagazin),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "aktenzeichen",
                        Value = ToDb(indexSnapshot.Aktenzeichen),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    return Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }
            }
        }

        /// <summary>
        ///     Formularbestellung (existiert nicht im Index)
        /// </summary>
        private async Task<int> AddToBasket(string bestand, string ablieferung, string behaeltnisNummer, string archivNummer, string aktenzeichen,
            string dossiertitel, string zeitraumDossier, string signatur, int basketId)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText =
                        "INSERT INTO OrderItem (OrderId, Bestand, Ablieferung, BehaeltnisNummer, ArchivNummer, Aktenzeichen, Dossiertitel, ZeitraumDossier, Status, Signatur) " +
                        "OUTPUT INSERTED.ID " +
                        "VALUES (@p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, 0, @p9)";
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "p1",
                        Value = basketId,
                        SqlDbType = SqlDbType.Int
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "p2",
                        Value = ToDb(bestand),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "p3",
                        Value = ToDb(ablieferung),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "p4",
                        Value = ToDb(behaeltnisNummer),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "p5",
                        Value = ToDb(archivNummer),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "p6",
                        Value = ToDb(aktenzeichen),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "p7",
                        Value = ToDb(dossiertitel),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "p8",
                        Value = ToDb(zeitraumDossier),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "p9",
                        Value = ToDb(!string.IsNullOrEmpty(signatur)
                            ? signatur
                            : GetFormOrderSignatur(bestand, ablieferung, behaeltnisNummer, archivNummer)),
                        SqlDbType = SqlDbType.NVarChar
                    });

                    return Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }
            }
        }

        private Ordering OrderingFromReader(SqlDataReader reader)
        {
            return new Ordering
            {
                Id = Convert.ToInt32(reader["ID"]),
                Type = (OrderType) Convert.ToInt32(reader["Type"]),
                Comment = reader["Comment"] as string,
                ArtDerArbeit = reader["ArtDerArbeit"] == DBNull.Value ? null : (int?) Convert.ToInt32(reader["ArtDerArbeit"]),
                LesesaalDate = reader["lesesaalDate"] == DBNull.Value ? null : (DateTime?) Convert.ToDateTime(reader["lesesaalDate"]),
                OrderDate = reader["OrderDate"] == DBNull.Value ? null : (DateTime?) Convert.ToDateTime(reader["OrderDate"]),
                BegruendungEinsichtsgesuch = reader["BegruendungEinsichtsgesuch"] as string,
                UserId = reader["UserId"] as string,
                PersonenbezogeneNachforschung = Convert.ToBoolean(reader["PersonenbezogeneNachforschung"]),
                HasEigenePersonendaten = Convert.ToBoolean(reader["HasEigenePersonendaten"]),
                RolePublicClient = reader["RolePublicClient"] as string
            };
        }

        private PrimaerdatenAufbereitungItem PrimaerdatenAufbereitungItemFromReader(SqlDataReader reader)
        {
            var primaerdatenAufbereitungItem = new PrimaerdatenAufbereitungItem();
            primaerdatenAufbereitungItem.OrderingType = reader["OrderingType"].GetValueOrNull<int>();
            primaerdatenAufbereitungItem.OrderItemId = reader["OrderItemId"].GetValueOrNull<int>();
            primaerdatenAufbereitungItem.Dossiertitel = reader["Dossiertitel"].ToString();
            primaerdatenAufbereitungItem.VeId = reader["VeId"].GetValueOrNull<int>();
            primaerdatenAufbereitungItem.Signatur = reader["Signatur"].ToString();
            primaerdatenAufbereitungItem.NeuEingegangen = reader["NeuEingegangen"].GetValueOrNull<DateTime>();
            primaerdatenAufbereitungItem.Ausgeliehen = reader["Ausgeliehen"].GetValueOrNull<DateTime>();
            primaerdatenAufbereitungItem.ZumReponierenBereit = reader["ZumReponierenBereit"].GetValueOrNull<DateTime>();
            primaerdatenAufbereitungItem.Abgeschlossen = reader["Abgeschlossen"].GetValueOrNull<DateTime>();
            primaerdatenAufbereitungItem.AufbereitungsArt = reader["AufbereitungsArt"].ToString(); 
            primaerdatenAufbereitungItem.AuftragErledigt = reader["AuftragErledigt"].GetValueOrNull<DateTime>();
            primaerdatenAufbereitungItem.DigitalisierungsKategorieId = reader["DigitalisierungsKategorieId"].GetValueOrNull<int>();
            primaerdatenAufbereitungItem.PrimaerdatenAuftragId = reader["PrimaerdatenAuftragId"].GetValueOrNull<int>();
            primaerdatenAufbereitungItem.GroesseInBytes = reader["GroesseInBytes"].GetValueOrNull<long>();
            primaerdatenAufbereitungItem.PackageMetadata = reader["PackageMetadata"].ToString();

            return primaerdatenAufbereitungItem;
        }

        private OrderItem OrderItemFromReader(SqlDataReader reader)
        {
            return new OrderItem
            {
                Id = Convert.ToInt32(reader["ID"]),
                VeId = ToInt32Opt(reader["Ve"]),
                Comment = reader["Comment"] as string,
                BewilligungsDatum = reader["BewilligungsDatum"] == DBNull.Value ? null : (DateTime?) Convert.ToDateTime(reader["BewilligungsDatum"]),
                Bestand = reader["Bestand"] as string,
                Ablieferung = reader["Ablieferung"] as string,
                BehaeltnisNummer = reader["BehaeltnisNummer"] as string,
                Dossiertitel = reader["Dossiertitel"] as string,
                ZeitraumDossier = reader["ZeitraumDossier"] as string,
                ArchivNummer = reader["ArchivNummer"] as string,
                OrderId = Convert.ToInt32(reader["OrderId"]),
                HasPersonendaten = Convert.ToBoolean(reader["HasPersonendaten"]),
                Status = (OrderStatesInternal) Convert.ToInt32(reader["Status"]),
                Darin = reader["Darin"] as string,
                ApproveStatus = (ApproveStatus) Convert.ToInt32(reader["ApproveStatus"]),
                Signatur = reader["Signatur"] as string,
                Standort = reader["Standort"] as string,
                Behaeltnistyp = reader["Behaeltnistyp"] as string,
                Hierarchiestufe = reader["Hierarchiestufe"] as string,
                IdentifikationDigitalesMagazin = reader["IdentifikationDigitalesMagazin"] as string,
                Publikationsrechte = reader["Publikationsrechte"] as string,
                Schutzfristverzeichnung = reader["Schutzfristverzeichnung"] as string,
                ZugaenglichkeitGemaessBga = reader["ZugaenglichkeitGemaessBga"] as string,
                ZusaetzlicheInformationen = reader["ZusaetzlicheInformationen"] as string,
                ZustaendigeStelle = reader["ZustaendigeStelle"] as string,
                Aktenzeichen = reader["Aktenzeichen"] as string,
                Reason = ToInt32Opt(reader["Reason"]),
                DigitalisierungsKategorie = ToEnum<DigitalisierungsKategorie>(reader["DigitalisierungsKategorie"]),
                TerminDigitalisierung = reader["TerminDigitalisierung"] == DBNull.Value
                    ? null
                    : (DateTime?) Convert.ToDateTime(reader["TerminDigitalisierung"]),
                InternalComment = reader["InternalComment"] as string,
                EntscheidGesuch = ToEnum<EntscheidGesuch>(reader["EntscheidGesuch"]),
                DatumDesEntscheids = reader["DatumDesEntscheids"] == DBNull.Value
                    ? null
                    : (DateTime?) Convert.ToDateTime(reader["DatumDesEntscheids"]),
                Ausgabedatum = reader["Ausgabedatum"] == DBNull.Value ? null : (DateTime?) Convert.ToDateTime(reader["Ausgabedatum"]),
                Abschlussdatum = reader["Abschlussdatum"] == DBNull.Value ? null : (DateTime?) Convert.ToDateTime(reader["Abschlussdatum"]),
                Abbruchgrund = ToEnum<Abbruchgrund>(reader["Abbruchgrund"]),
                Benutzungskopie = reader["Benutzungskopie"] as bool?,
                DatumDerFreigabe = reader["DatumDerFreigabe"] == DBNull.Value ? null : (DateTime?) Convert.ToDateTime(reader["DatumDerFreigabe"]),
                SachbearbeiterId = reader["SachbearbeiterId"] as string,
                AnzahlMahnungen = Convert.ToInt32(reader["AnzahlMahnungen"]),
                Ausleihdauer = Convert.ToInt32(reader["Ausleihdauer"]),
                MahndatumInfo = reader["MahndatumInfo"] as string,
                GebrauchskopieStatus = ToEnum<GebrauchskopieStatus>(reader["GebrauchskopieStatus"])
            };
        }


        private static async Task UpdateOrderingFromBasket(UpdateOrderingParams updateOrderingParams, SqlConnection connection)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText =
                    "UPDATE Ordering SET Type = @p1, Comment = @p2, ArtDerArbeit = @p3, lesesaalDate = @p4, OrderDate = @p5, BegruendungEinsichtsgesuch = @p6, " +
                    "HasEigenePersonendaten = @p7, PersonenbezogeneNachforschung = @p8 WHERE UserId = @p9 AND Type = @p10";

                cmd.Parameters.Add(new SqlParameter
                {
                    ParameterName = "p1",
                    Value = updateOrderingParams.Type,
                    SqlDbType = SqlDbType.Int
                });
                cmd.Parameters.Add(new SqlParameter
                {
                    ParameterName = "p2",
                    Value = ToDb(updateOrderingParams.Comment),
                    SqlDbType = SqlDbType.NVarChar
                });
                cmd.Parameters.Add(new SqlParameter
                {
                    ParameterName = "p3",
                    Value = ToDb(updateOrderingParams.ArtDerArbeit),
                    SqlDbType = SqlDbType.Int
                });
                cmd.Parameters.Add(new SqlParameter
                {
                    ParameterName = "p4",
                    Value = ToDb(updateOrderingParams.LesesaalDate),
                    SqlDbType = SqlDbType.Date
                });
                cmd.Parameters.Add(new SqlParameter
                {
                    ParameterName = "p5",
                    Value = DateTime.Now,
                    SqlDbType = SqlDbType.DateTime
                });
                cmd.Parameters.Add(new SqlParameter
                {
                    ParameterName = "p6",
                    Value = ToDb(updateOrderingParams.BegruendungEinsichtsgesuch),
                    SqlDbType = SqlDbType.NVarChar
                });
                cmd.Parameters.Add(new SqlParameter
                {
                    ParameterName = "p7",
                    Value = ToDb(updateOrderingParams.HasEigenePersonendaten),
                    SqlDbType = SqlDbType.Bit
                });
                cmd.Parameters.Add(new SqlParameter
                {
                    ParameterName = "p8",
                    Value = ToDb(updateOrderingParams.PersonenbezogeneNachforschung),
                    SqlDbType = SqlDbType.Bit
                });
                cmd.Parameters.Add(new SqlParameter
                {
                    ParameterName = "p9",
                    Value = updateOrderingParams.UserId,
                    SqlDbType = SqlDbType.NVarChar
                });
                cmd.Parameters.Add(new SqlParameter
                {
                    ParameterName = "p10",
                    Value = (int) OrderType.Bestellkorb,
                    SqlDbType = SqlDbType.Int
                });

                await cmd.ExecuteScalarAsync();
            }
        }

        private static async Task MoveToBasket(int basketId, List<int> orderItemIds, SqlConnection connection)
        {
            using (var cmd = connection.CreateCommand())
            {
                var asStringEnumerable = orderItemIds.Select(i => Convert.ToString(i));
                var asString = string.Join(",", asStringEnumerable);

                cmd.CommandText = $"UPDATE OrderItem SET OrderId = {basketId} WHERE ID IN ({asString})";

                await cmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        ///     Die Methode stellt sicher, dass nicht Daten von einem anderen Benutzer mutiert werden können. Die Methode wurde
        ///     erstellt,
        ///     weil auf das aufrufende API von aussen zugegriffen werden kann.
        /// </summary>
        private async Task EnsureLegalUser(int orderItemId, string executingUserId)
        {
            await EnsureLegalUser(new List<int> {orderItemId}, executingUserId);
        }

        private async Task EnsureLegalUser(IEnumerable<int> orderItemIds, string executingUserId)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT UserId FROM Ordering WHERE Ordering.ID IN " +
                                      $"(SELECT OrderId FROM OrderItem WHERE OrderItem.ID IN ({string.Join(",", orderItemIds)}))";

                    var result = await cmd.ExecuteScalarAsync();

                    if (result is string s && s != executingUserId)
                    {
                        throw new InvalidOperationException("The OrderItemId belongs not too the executing user");
                    }
                }
            }
        }

        private Bestellhistorie BestellhistorieFromReader(SqlDataReader reader)
        {
            var hist = new Bestellhistorie();
            reader.PopulateProperties(hist);
            return hist;
        }

        private StatusHistory StatusHistoryFromReader(SqlDataReader reader)
        {
            var hist = new StatusHistory();
            reader.PopulateProperties(hist);
            return hist;
        }

        private OrderExecutedWaitList OrderExecutedWaitListFromReader(SqlDataReader reader)
        {
            var hist = new OrderExecutedWaitList();
            reader.PopulateProperties(hist);
            return hist;
        }


        private static int? ToInt32Opt(object obj)
        {
            if (obj == DBNull.Value)
            {
                return null;
            }

            return Convert.ToInt32(obj);
        }

        private T ToEnum<T>(object obj) where T : struct, IConvertible
        {
            if (obj == DBNull.Value)
            {
                return default;
            }

            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("T must be an enumerated type");
            }

            var intValue = Convert.ToInt32(obj);

            if (!Enum.IsDefined(typeof(T), intValue))
            {
                throw new ArgumentException("The Int32 value " + intValue + " can not be converted into an enum of type " + typeof(T).FullName);
            }

            return (T) Enum.ToObject(typeof(T), intValue);
        }

        private string GetFormOrderSignatur(OrderItem orderItem)
        {
            return
                $"{orderItem.Bestand}#{orderItem.Ablieferung}, {(!string.IsNullOrEmpty(orderItem.BehaeltnisNummer) ? $"Bd. {orderItem.BehaeltnisNummer}" : $"Nr. {orderItem.ArchivNummer}")}";
        }

        private string GetFormOrderSignatur(string bestand, string ablieferung, string behaeltnisNummer, string archivNummer)
        {
            return $"{bestand}#{ablieferung}, {(!string.IsNullOrEmpty(behaeltnisNummer) ? $"Bd. {behaeltnisNummer}" : $"Nr. {archivNummer}")}";
        }
    }


    public class IndivTokens
    {
        public IndivTokens(string[] primaryDataFulltextAccessTokens, string[] primaryDataDownloadAccessTokens, string[] fieldAccessTokens)
        {
            PrimaryDataFulltextAccessTokens = primaryDataFulltextAccessTokens;
            PrimaryDataDownloadAccessTokens = primaryDataDownloadAccessTokens;
            FieldDataAccessTokens = fieldAccessTokens;
        }

        public string[] PrimaryDataFulltextAccessTokens { get; }

        public string[] PrimaryDataDownloadAccessTokens { get; }

        public string[] MetadataAccessTokens { get; }

        public string[] FieldDataAccessTokens { get; }
    }
}