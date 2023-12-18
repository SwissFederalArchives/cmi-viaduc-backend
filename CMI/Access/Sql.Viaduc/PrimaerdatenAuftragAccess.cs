using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Transactions;
using CMI.Contract.Common;
using Serilog;

namespace CMI.Access.Sql.Viaduc
{
    public interface IPrimaerdatenAuftragAccess
    {
        Task<int> CreateOrUpdateAuftrag(PrimaerdatenAuftrag auftrag);
        Task<PrimaerdatenAuftrag> GetPrimaerdatenAuftrag(int primaerdatenAuftragId, bool loadLogEntries = false, bool skipMaxVarCharFields = false);
        Task<int> UpdateStatus(PrimaerdatenAuftragLog statusLog, int verarbeitungsKanal = 0);
        Task<PrimaerdatenAuftragStatusInfo> GetLaufendenAuftrag(int veId, AufbereitungsArtEnum aufbereitungsArt);
        Task<Dictionary<int, int>> GetCurrentWorkload(AufbereitungsArtEnum aufbereitungsArt);

        /// <summary>
        ///     Liefert eine Liste mit den nächsten Aufträgen.
        /// </summary>
        /// <param name="aufbereitungsArt">Die Art der Aufbereitung (Sync oder Download)</param>
        /// <param name="priorisierungsKategorien">Welche Priorisierungskategorien geholt werden sollen.</param>
        /// <param name="anzahlJobs">Wieviele Aufträge geholt werden sollen.</param>
        /// <returns>Eine Liste mit der PrimaerdatenAuftragId der nächsten Aufträge</returns>
        Task<List<int>> GetNextJobsForChannel(AufbereitungsArtEnum aufbereitungsArt, int[] priorisierungsKategorien, int anzahlJobs,
            int[] primaerdatenAuftragIdsToExclude);

        Task DeleteOldDownloadAndSyncRecords(int olderThanXDays);
    }

    public class PrimaerdatenAuftragAccess : DataAccess, IPrimaerdatenAuftragAccess
    {
        private readonly string connectionString;

        public PrimaerdatenAuftragAccess(string connectionString)
        {
            this.connectionString = connectionString;
        }


        public async Task<int> CreateOrUpdateAuftrag(PrimaerdatenAuftrag auftrag)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                if (auftrag.PrimaerdatenAuftragId > 0)
                {
                    await UpdateAuftrag(auftrag, connection);
                    return auftrag.PrimaerdatenAuftragId;
                }

                var auftragId = await CreateAuftrag(auftrag, connection);
                await InsertAuftragLog(new PrimaerdatenAuftragLog
                {
                    PrimaerdatenAuftragId = auftragId,
                    Service = auftrag.Service,
                    Status = AufbereitungsStatusEnum.Registriert
                }, connection);
                return auftragId;
            }
        }

        public async Task<PrimaerdatenAuftrag> GetPrimaerdatenAuftrag(int primaerdatenAuftragId, bool loadLogEntries = false, bool skipMaxVarCharFields = false)
        {
            PrimaerdatenAuftrag retVal;
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                retVal = await GetPrimaerdatenAuftragInternal(primaerdatenAuftragId, connection, loadLogEntries, skipMaxVarCharFields);
            }

            return retVal;
        }

        public async Task<int> UpdateStatus(PrimaerdatenAuftragLog statusLog, int verarbeitungsKanal = 0)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                // Get Auftrag
                var auftrag = await GetPrimaerdatenAuftragInternal(statusLog.PrimaerdatenAuftragId, connection, false, true);

                // Update the status only, if it is not already erledigt
                if (auftrag.Status != AufbereitungsStatusEnum.AuftragErledigt)
                {
                    auftrag.Status = statusLog.Status;
                }

                auftrag.Service = statusLog.Service;
                auftrag.ErrorText = statusLog.ErrorText;

                if (verarbeitungsKanal > 0)
                {
                    auftrag.Verarbeitungskanal = verarbeitungsKanal;
                }

                // If the status is "erledigt" then add other important info.
                if (auftrag.Status == AufbereitungsStatusEnum.AuftragErledigt)
                {
                    auftrag.Abgeschlossen = true;
                    auftrag.AbgeschlossenAm = DateTime.Now;
                }

                auftrag.ModifiedOn = DateTime.Now;
                await UpdateAuftrag(auftrag, connection, true);

                // Update log status
                var logId = await InsertAuftragLog(statusLog, connection);

                return logId;
            }
        }

        public async Task<PrimaerdatenAuftragStatusInfo> GetLaufendenAuftrag(int veId, AufbereitungsArtEnum aufbereitungsArt)
        {
            PrimaerdatenAuftragStatusInfo retVal = null;
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText =
                        "SELECT primaerdatenAuftragId, status, service, veId, CreatedOn, ModifiedOn, GeschaetzteAufbereitungszeit, AufbereitungsArt " +
                        "FROM PrimaerdatenAuftrag WHERE veId = @veId and abgeschlossen = 0 and aufbereitungsArt = @aufbereitungsArt";
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "veId",
                        Value = veId,
                        SqlDbType = SqlDbType.Int
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "aufbereitungsArt",
                        Value = aufbereitungsArt.ToString(),
                        SqlDbType = SqlDbType.NVarChar
                    });

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            await reader.ReadAsync();
                            retVal = PrimaerdatenAuftragStatusInfoFromReader(reader);
                        }
                    }
                }
            }

            return retVal;
        }

        /// <summary>
        ///     Liefert wieviele Aufträge in welchem Kanal drin stecken.
        ///     Dabei ist zu berücksichtigen, dass der REpository Service am "arbeiten" ist
        ///     solange der Status "PaketTransferiert" nicht erreicht ist.
        /// </summary>
        /// <param name="aufbereitungsArt"></param>
        /// <returns></returns>
        public async Task<Dictionary<int, int>> GetCurrentWorkload(AufbereitungsArtEnum aufbereitungsArt)
        {
            var retVal = new Dictionary<int, int>();
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var cmd = connection.CreateCommand())
                {
                    // with(nolock) is required as the grouping operator places a lock on the table.
                    // This then conflicts with the insert statements that are used on the same table and uses transactions.
                    cmd.CommandText = @"
                            SELECT Verarbeitungskanal, COUNT(primaerdatenAuftragId) AS Anzahl FROM PrimaerdatenAuftrag with(nolock) WHERE Abgeschlossen = 0 AND 
                            (Status = @Status1 OR Status = @Status2 OR Status = @Status3) AND AufbereitungsArt = @Aufbereitungsart AND Service = @Service
                            GROUP BY Verarbeitungskanal";
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "Status1",
                        Value = AufbereitungsStatusEnum.AuftragGestartet,
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "Status2",
                        Value = AufbereitungsStatusEnum.PrimaerdatenExtrahiert,
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "Status3",
                        Value = AufbereitungsStatusEnum.ZipDateiErzeugt,
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "Aufbereitungsart",
                        Value = aufbereitungsArt.ToString(),
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "Service",
                        Value = AufbereitungsServices.AssetService,
                        SqlDbType = SqlDbType.NVarChar
                    });

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            retVal.Add(Convert.ToInt32(reader["Verarbeitungskanal"]), Convert.ToInt32(reader["Anzahl"]));
                        }
                    }

                    // Make sure we get results for all channels
                    if (!retVal.ContainsKey(1))
                    {
                        retVal.Add(1, 0);
                    }

                    if (!retVal.ContainsKey(2))
                    {
                        retVal.Add(2, 0);
                    }

                    if (!retVal.ContainsKey(3))
                    {
                        retVal.Add(3, 0);
                    }

                    if (!retVal.ContainsKey(4))
                    {
                        retVal.Add(4, 0);
                    }
                }
            }

            return retVal;
        }

        public async Task<List<int>> GetNextJobsForChannel(AufbereitungsArtEnum aufbereitungsArt, int[] priorisierungsKategorien, int anzahlJobs,
            int[] primaerdatenAuftragIdsToExclude)
        {
            var retVal = new List<int>();
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = $@"
                                SELECT TOP {anzahlJobs} PrimaerdatenAuftragId, VeId FROM PrimaerdatenAuftrag 
                                WHERE AufbereitungsArt = @Aufbereitungsart AND 
                                Status = @Status AND Abgeschlossen = 0 AND PriorisierungsKategorie IN ({string.Join(",", priorisierungsKategorien)}) ";
                    if (primaerdatenAuftragIdsToExclude.Length > 0)
                    {
                        cmd.CommandText += $@" AND PrimaerdatenAuftragId NOT IN ({string.Join(",", primaerdatenAuftragIdsToExclude)}) ";
                    }

                    cmd.CommandText += " ORDER BY PriorisierungsKategorie, CreatedOn; ";
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "Status",
                        Value = AufbereitungsStatusEnum.Registriert,
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "Aufbereitungsart",
                        Value = aufbereitungsArt.ToString(),
                        SqlDbType = SqlDbType.NVarChar
                    });

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            retVal.Add(Convert.ToInt32(reader["PrimaerdatenAuftragId"]));
                        }
                    }
                }
            }

            return retVal;
        }

        public async Task DeleteOldDownloadAndSyncRecords(int olderThanXDays)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = $@"DELETE FROM PrimaerdatenAuftrag WHERE ModifiedOn < DATEADD(day, -{olderThanXDays}, GETDATE())";

                    try
                    {
                        await cmd.ExecuteScalarAsync();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Unexpected error when cleaning up Table PrimaerdatenAuftrag.");
                        throw;
                    }
                }
            }
        }

        private async Task<PrimaerdatenAuftrag> GetPrimaerdatenAuftragInternal(int primaerdatenAuftragId, SqlConnection connection,
            bool loadLogEntries = false, bool skipMaxVarCharFields = false)
        {
            PrimaerdatenAuftrag retVal = null;
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @$"SELECT PrimaerdatenAuftragId, AufbereitungsArt, GroesseInBytes, Verarbeitungskanal, Status, Service, 
                                            PackageId, VeId, Abgeschlossen, AbgeschlossenAm, 
                                            {(skipMaxVarCharFields ? "'' as Workload, '' as PackageMetadata," : "Workload, PackageMetadata,")}
                                            GeschaetzteAufbereitungszeit, ErrorText, CreatedOn, ModifiedOn, PriorisierungsKategorie 
                                    FROM PrimaerdatenAuftrag WHERE primaerdatenAuftragId = @primaerdatenAuftragId ";
                cmd.Parameters.Add(new SqlParameter
                {
                    ParameterName = "primaerdatenAuftragId",
                    Value = primaerdatenAuftragId,
                    SqlDbType = SqlDbType.Int
                });

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        await reader.ReadAsync();
                        retVal = PrimaerdatenAuftragFromReader(reader);
                    }
                }
            }

            if (loadLogEntries && retVal != null)
            {
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM PrimaerdatenAuftragLog WHERE primaerdatenAuftragId = @primaerdatenAuftragId ";
                    cmd.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "primaerdatenAuftragId",
                        Value = primaerdatenAuftragId,
                        SqlDbType = SqlDbType.Int
                    });

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (reader.HasRows)
                        {
                            await reader.ReadAsync();
                            retVal.PrimaerdatenAuftragLogs.Add(PrimaerdatenAuftragLogFromReader(reader));
                        }
                    }
                }
            }

            return retVal;
        }


        private async Task<int> CreateAuftrag(PrimaerdatenAuftrag auftrag, SqlConnection connection)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
	INSERT INTO PrimaerdatenAuftrag (AufbereitungsArt, GroesseInBytes, Verarbeitungskanal, PriorisierungsKategorie, Status, Service, PackageId, PackageMetadata, VeId, Abgeschlossen, AbgeschlossenAm, GeschaetzteAufbereitungszeit, ErrorText, Workload, CreatedOn, ModifiedOn)
    OUTPUT INSERTED.PrimaerdatenAuftragId 
	Values(@AufbereitungsArt, @GroesseInBytes, @Verarbeitungskanal, @PriorisierungsKategorie, @Status, @Service, @PackageId, @PackageMetadata, @VeId, @Abgeschlossen, @AbgeschlossenAm, @GeschaetzteAufbereitungszeit, @ErrorText, @Workload, @CreatedOn, @ModifiedOn)
                ";

                #region parameters

                AppendParametersForAuftrag(auftrag, cmd);

                cmd.Parameters.Add(new SqlParameter
                {
                    ParameterName = "CreatedOn",
                    Value = DateTime.Now,
                    SqlDbType = SqlDbType.DateTime2
                });
                cmd.Parameters.Add(new SqlParameter
                {
                    ParameterName = "ModifiedOn",
                    Value = DBNull.Value,
                    SqlDbType = SqlDbType.DateTime2
                });

                #endregion

                try
                {
                    return Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Unexpected error when creating PrimaerdatenAuftrag for archive record with id {archiveRecordId}.", auftrag.VeId);
                    throw;
                }
            }
        }

        private async Task<int> UpdateAuftrag(PrimaerdatenAuftrag auftrag, SqlConnection connection, bool skipMaxVarcharFields = false)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
	UPDATE PrimaerdatenAuftrag 
	SET    AufbereitungsArt = @AufbereitungsArt, GroesseInBytes = @GroesseInBytes, Verarbeitungskanal = @Verarbeitungskanal, 
           PriorisierungsKategorie = @PriorisierungsKategorie, Status = @Status, Service = @Service, PackageId = @PackageId, 
           {0}
           VeId = @VeId, Abgeschlossen = @Abgeschlossen, AbgeschlossenAm = @AbgeschlossenAm, 
           GeschaetzteAufbereitungszeit = @GeschaetzteAufbereitungszeit, ErrorText = @ErrorText, 
           ModifiedOn = @ModifiedOn
	WHERE  PrimaerdatenAuftragId = @PrimaerdatenAuftragId                ";

                // We have noticed that the fields PackageMetadata and Workload can contain large amounts of data (70 MB and more)
                // in those cases updating those fields have led to timeout problems.
                // Not really sure if those fields are the cause, but in any case, we don't need to update those fields in any case
                cmd.CommandText = string.Format(cmd.CommandText, skipMaxVarcharFields ? "" : "PackageMetadata = @PackageMetadata, Workload = @Workload, ");

                #region parameters

                AppendParametersForAuftrag(auftrag, cmd, skipMaxVarcharFields);

                cmd.Parameters.Add(new SqlParameter
                {
                    ParameterName = "ModifiedOn",
                    Value = DateTime.Now,
                    SqlDbType = SqlDbType.DateTime2
                });
                cmd.Parameters.Add(new SqlParameter
                {
                    ParameterName = "PrimaerdatenAuftragId",
                    Value = auftrag.PrimaerdatenAuftragId,
                    SqlDbType = SqlDbType.Int
                });

                #endregion

                try
                {
                    var recordsAffected = await cmd.ExecuteNonQueryAsync();
                    return recordsAffected;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Unexpected error when updating PrimaerdatenAuftrag with id {PrimaerdatenAuftragId} ",
                        auftrag.PrimaerdatenAuftragId);
                    throw;
                }
            }
        }

        private async Task<int> InsertAuftragLog(PrimaerdatenAuftragLog auftragLog, SqlConnection connection)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
	INSERT INTO PrimaerdatenAuftragLog (PrimaerdatenAuftragId, Status, Service, ErrorText)
    OUTPUT INSERTED.PrimaerdatenAuftragLogId
	SELECT @PrimaerdatenAuftragId, @Status, @Service, @ErrorText
                ";

                #region parameters

                cmd.Parameters.Add(new SqlParameter
                {
                    ParameterName = "PrimaerdatenAuftragId",
                    Value = auftragLog.PrimaerdatenAuftragId,
                    SqlDbType = SqlDbType.Int
                });
                cmd.Parameters.Add(new SqlParameter
                {
                    ParameterName = "Status",
                    Value = auftragLog.Status.ToString().ToDbParameterValue(),
                    SqlDbType = SqlDbType.NVarChar
                });
                cmd.Parameters.Add(new SqlParameter
                {
                    ParameterName = "Service",
                    Value = auftragLog.Service.ToString().ToDbParameterValue(),
                    SqlDbType = SqlDbType.NVarChar
                });
                cmd.Parameters.Add(new SqlParameter
                {
                    ParameterName = "ErrorText",
                    Value = auftragLog.ErrorText.ToDbParameterValue(),
                    SqlDbType = SqlDbType.NVarChar
                });

                #endregion

                return Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }
        }

        private PrimaerdatenAuftrag PrimaerdatenAuftragFromReader(SqlDataReader reader)
        {
            // reader.PopulateProperties ist nicht möglich wegen string to enum 
            return new PrimaerdatenAuftrag
            {
                PrimaerdatenAuftragId = Convert.ToInt32(reader["PrimaerdatenAuftragId"]),
                AufbereitungsArt = (AufbereitungsArtEnum) Enum.Parse(typeof(AufbereitungsArtEnum), reader["AufbereitungsArt"] as string ?? throw new InvalidOperationException()),
                GroesseInBytes = reader["GroesseInBytes"] == DBNull.Value ? null : (long?) Convert.ToInt64(reader["GroesseInBytes"]),
                Verarbeitungskanal = reader["Verarbeitungskanal"] == DBNull.Value ? null : (int?) Convert.ToInt32(reader["Verarbeitungskanal"]),
                PriorisierungsKategorie = reader["PriorisierungsKategorie"] == DBNull.Value
                    ? null
                    : (int?) Convert.ToInt32(reader["PriorisierungsKategorie"]),
                Status = (AufbereitungsStatusEnum) Enum.Parse(typeof(AufbereitungsStatusEnum), reader["Status"] as string ?? throw new InvalidOperationException()),
                Service = (AufbereitungsServices) Enum.Parse(typeof(AufbereitungsServices), reader["Service"] as string ?? throw new InvalidOperationException()),
                PackageId = reader["PackageId"] as string,
                PackageMetadata = reader["PackageMetadata"] as string,
                VeId = Convert.ToInt32(reader["VeId"]),
                Abgeschlossen = Convert.ToBoolean(reader["Abgeschlossen"]),
                AbgeschlossenAm = reader["AbgeschlossenAm"] == DBNull.Value ? null : (DateTime?) Convert.ToDateTime(reader["AbgeschlossenAm"]),
                GeschaetzteAufbereitungszeit = reader["GeschaetzteAufbereitungszeit"] == DBNull.Value
                    ? null
                    : (int?) Convert.ToInt32(reader["GeschaetzteAufbereitungszeit"]),
                ErrorText = reader["ErrorText"] as string,
                Workload = reader["Workload"] as string,
                CreatedOn = Convert.ToDateTime(reader["CreatedOn"]),
                ModifiedOn = reader["ModifiedOn"] == DBNull.Value ? null : (DateTime?) Convert.ToDateTime(reader["ModifiedOn"])
            };
        }


        private PrimaerdatenAuftragStatusInfo PrimaerdatenAuftragStatusInfoFromReader(SqlDataReader reader)
        {
            // reader.PopulateProperties ist nicht möglich wegen string to enum 
            return new PrimaerdatenAuftragStatusInfo
            {
                PrimaerdatenAuftragId = Convert.ToInt32(reader["PrimaerdatenAuftragId"]),
                AufbereitungsArt = (AufbereitungsArtEnum) Enum.Parse(typeof(AufbereitungsArtEnum), reader["AufbereitungsArt"] as string ?? throw new InvalidOperationException()),
                Status = (AufbereitungsStatusEnum) Enum.Parse(typeof(AufbereitungsStatusEnum), reader["Status"] as string ?? throw new InvalidOperationException()),
                Service = (AufbereitungsServices) Enum.Parse(typeof(AufbereitungsServices), reader["Service"] as string ?? throw new InvalidOperationException()),
                VeId = Convert.ToInt32(reader["VeId"]),
                GeschaetzteAufbereitungszeit = reader["GeschaetzteAufbereitungszeit"] == DBNull.Value
                    ? null
                    : (int?) Convert.ToInt32(reader["GeschaetzteAufbereitungszeit"]),
                CreatedOn = Convert.ToDateTime(reader["CreatedOn"]),
                ModifiedOn = reader["ModifiedOn"] == DBNull.Value ? null : (DateTime?) Convert.ToDateTime(reader["ModifiedOn"])
            };
        }

        private PrimaerdatenAuftragLog PrimaerdatenAuftragLogFromReader(SqlDataReader reader)
        {
            // reader.PopulateProperties ist nicht möglich wegen string to enum 
            return new PrimaerdatenAuftragLog
            {
                PrimaerdatenAuftragLogId = Convert.ToInt32(reader["PrimaerdatenAuftragLogId"]),
                PrimaerdatenAuftragId = Convert.ToInt32(reader["PrimaerdatenAuftragId"]),
                Status = (AufbereitungsStatusEnum) Enum.Parse(typeof(AufbereitungsStatusEnum), reader["Status"] as string ?? throw new InvalidOperationException()),
                Service = (AufbereitungsServices) Enum.Parse(typeof(AufbereitungsServices), reader["Service"] as string ?? throw new InvalidOperationException()),
                ErrorText = reader["ErrorText"] as string,
                CreatedOn = Convert.ToDateTime(reader["CreatedOn"])
            };
        }

        private static void AppendParametersForAuftrag(PrimaerdatenAuftrag auftrag, SqlCommand cmd, bool skipMaxVarcharFields = false)
        {
            cmd.Parameters.Add(new SqlParameter
            {
                ParameterName = "AufbereitungsArt",
                Value = auftrag.AufbereitungsArt.ToString().ToDbParameterValue(),
                SqlDbType = SqlDbType.NVarChar
            });
            cmd.Parameters.Add(new SqlParameter
            {
                ParameterName = "GroesseInBytes",
                Value = auftrag.GroesseInBytes.ToDbParameterValue(),
                SqlDbType = SqlDbType.BigInt
            });
            cmd.Parameters.Add(new SqlParameter
            {
                ParameterName = "Verarbeitungskanal",
                Value = auftrag.Verarbeitungskanal.ToDbParameterValue(),
                SqlDbType = SqlDbType.Int
            });
            cmd.Parameters.Add(new SqlParameter
            {
                ParameterName = "PriorisierungsKategorie",
                Value = auftrag.PriorisierungsKategorie.ToDbParameterValue(),
                SqlDbType = SqlDbType.Int
            });
            cmd.Parameters.Add(new SqlParameter
            {
                ParameterName = "Status",
                Value = auftrag.Status.ToString().ToDbParameterValue(),
                SqlDbType = SqlDbType.NVarChar
            });
            cmd.Parameters.Add(new SqlParameter
            {
                ParameterName = "Service",
                Value = auftrag.Service.ToString().ToDbParameterValue(),
                SqlDbType = SqlDbType.NVarChar
            });
            cmd.Parameters.Add(new SqlParameter
            {
                ParameterName = "PackageId",
                Value = auftrag.PackageId.ToDbParameterValue(),
                SqlDbType = SqlDbType.NVarChar
            });
            cmd.Parameters.Add(new SqlParameter
            {
                ParameterName = "VeId",
                Value = auftrag.VeId.ToDbParameterValue(),
                SqlDbType = SqlDbType.Int
            });
            cmd.Parameters.Add(new SqlParameter
            {
                ParameterName = "Abgeschlossen",
                Value = auftrag.Abgeschlossen.ToDbParameterValue(),
                SqlDbType = SqlDbType.Bit
            });
            cmd.Parameters.Add(new SqlParameter
            {
                ParameterName = "AbgeschlossenAm",
                Value = auftrag.AbgeschlossenAm.ToDbParameterValue(),
                SqlDbType = SqlDbType.DateTime2
            });
            cmd.Parameters.Add(new SqlParameter
            {
                ParameterName = "GeschaetzteAufbereitungszeit",
                Value = auftrag.GeschaetzteAufbereitungszeit.ToDbParameterValue(),
                SqlDbType = SqlDbType.Int
            });
            cmd.Parameters.Add(new SqlParameter
            {
                ParameterName = "ErrorText",
                Value = auftrag.ErrorText.ToDbParameterValue(),
                SqlDbType = SqlDbType.NVarChar
            });
            if (!skipMaxVarcharFields)
            {
                cmd.Parameters.Add(new SqlParameter
                {
                    ParameterName = "Workload",
                    Value = auftrag.Workload.ToDbParameterValue(),
                    SqlDbType = SqlDbType.NVarChar
                });
                cmd.Parameters.Add(new SqlParameter
                {
                    ParameterName = "PackageMetadata",
                    Value = auftrag.PackageMetadata.ToDbParameterValue(),
                    SqlDbType = SqlDbType.NVarChar
                });
            }

        }
    }
}