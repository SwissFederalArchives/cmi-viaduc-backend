using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CMI.Access.Harvest.ScopeArchiv.DataSets;
using CMI.Contract.Common;
using CMI.Contract.Harvest;
using CMI.Utilities.Security;
using Devart.Data.Oracle;
using Serilog;

namespace CMI.Access.Harvest.ScopeArchiv
{
    /// <summary>
    ///     The AIS data provider class presents various methods to get data from
    ///     the scopeArchiv database.
    /// </summary>
    public class AISDataProvider : IAISDataProvider
    {
        private const int vrzngEnhtHierarchieBzhngTypId = 10;
        private static readonly ApplicationSettings applicationSettings;

        static AISDataProvider()
        {
            applicationSettings = new ApplicationSettings();
        }

        private static OrderDetailData ConvertDataSetRowToOrderDetailData(DataRow row)
        {
            return new OrderDetailData
            {
                Id = row["vrzng_enht_id"].ToString(),
                Title = row["vrzng_enht_titel"].ToString(),
                ReferenceCode = row["sgntr_cd"].ToString(),
                Level = row["vrzng_enht_entrg_typ_nm"].ToString(),
                CreationPeriod = row["zt_raum_txt"].ToString(),
                BeginStandardDate = row["bgn_dt_stnd"].ToString(),
                BeginApproxIndicator = Convert.ToBoolean(row["bgn_circa_ind"]),
                EndStandardDate = row["end_dt_stnd"].ToString(),
                EndApproxIndicator = Convert.ToBoolean(row["end_circa_ind"]),
                DateOperatorId = row["dt_oprtr_id"] != null ? Convert.ToInt32(row["dt_oprtr_id"]) : (int?) null,
                DossierCode = row["dossierCode"].ToString(),
                FormerDossierCode = row["formerDossierCode"].ToString(),
                Zusatzkomponente = row["zusatzkomponente"].ToString(),
                Form = row["form"].ToString(),
                WithinRemark = row["darin"].ToString()
            };
        }

        #region Mutation Table related

        /// <summary>
        ///     Gets the pending mutations from the AIS.
        /// </summary>
        /// <returns>A list with the records that need to be synced.</returns>
        public List<MutationRecord> GetPendingMutations()
        {
            var ds = GetDataSetFromSql<DataSet>(SqlStatements.SqlMutationsRecords);

            var list = (from r in ds.Tables[0].AsEnumerable()
                select new MutationRecord
                {
                    MutationId = (int) r.Field<double>("mttn_id"),
                    ArchiveRecordId = r.Field<double>("gsft_obj_id").ToString(CultureInfo.InvariantCulture),
                    Action = r.Field<string>("aktn")
                }).ToList();

            return list;
        }
        

        /// <summary>
        ///     Updates the mutation status of a mutation record in the AIS.
        /// </summary>
        /// <param name="info">Info object with detailed information about the new status</param>
        /// <returns>The number of affected records.</returns>
        public int UpdateMutationStatus(MutationStatusInfo info)
        {
            int affectedRecords;

            using (var cn = AISConnection.GetConnection())
            {
                var sw = new Stopwatch();
                var sql = new StringBuilder();
                try
                {
                    sql.Append("Update tbk_viaduc_mttn set aktn_stts = :stts");

                    // If we have a completed or failed attempt, update the counter
                    if (info.NewStatus == ActionStatus.SyncCompleted || info.NewStatus == ActionStatus.SyncFailed)
                    {
                        sql.Append(", SYNC_ANZ_VRSCH = nvl(SYNC_ANZ_VRSCH, 0) + 1");
                    }

                    sql.Append(" where mttn_id = :mttn_id ");

                    // If the status udpate is only allowed from a specific existing status, 
                    // add the required where clause.
                    if (info.ChangeFromStatus.HasValue)
                    {
                        sql.Append(" and aktn_stts = :sttsFrom ");
                    }

                    using (var cmd = new OracleCommand(sql.ToString(), cn))
                    {
                        cmd.Parameters.AddWithValue("mttn_id", info.MutationId);
                        cmd.Parameters.AddWithValue("stts", info.NewStatus);
                        if (info.ChangeFromStatus.HasValue)
                        {
                            cmd.Parameters.AddWithValue("sttsFrom", info.ChangeFromStatus);
                        }

                        affectedRecords = cmd.ExecuteNonQuery();
                    }

                    LogExecutionTimes(sql.ToString(), sw);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, ex.Message);
                    throw;
                }
                finally
                {
                    cn.Close();
                }

                // Insert Log record into the log table              
                if (affectedRecords > 0)
                {
                    InsertMutationStatusLog(info);
                }
            }

            return affectedRecords;
        }

        public int BulkUpdateMutationStatus(List<MutationStatusInfo> infos)
        {
            int affectedRecords;

            // Target status must be the same for all elements
            var statusGroup = infos.GroupBy(g => g.NewStatus).ToList();
            if (statusGroup.Count > 1)
            {
                throw new ArgumentException("All elements in the list must have the same NewStatus value.");
            }

            using (var cn = AISConnection.GetConnection())
            {
                var sw = new Stopwatch();
                var sql = new StringBuilder();
                try
                {
                    sql.Append("Update tbk_viaduc_mttn set aktn_stts = :stts");

                    // If we have a completed or failed attempt, update the counter
                    if (statusGroup.First().Key == ActionStatus.SyncCompleted || statusGroup.First().Key == ActionStatus.SyncFailed)
                    {
                        sql.Append(", SYNC_ANZ_VRSCH = nvl(SYNC_ANZ_VRSCH, 0) + 1");
                    }

                    sql.Append(" where mttn_id = :mttn_id ");

                    // If the status udpate is only allowed from a specific existing status, 
                    // add the required where clause.
                    if (statusGroup.First().Count(s => s.ChangeFromStatus.HasValue) > 0)
                    {
                        sql.Append(" and aktn_stts = :sttsFrom ");
                    }

                    using (var cmd = new OracleCommand(sql.ToString(), cn))
                    {
                        cmd.Parameters.Add("mttn_id", OracleDbType.Long);
                        cmd.Parameters["mttn_id"].Value = statusGroup.First().Select(s => s.MutationId).ToArray();
                        cmd.Parameters.Add("stts", OracleDbType.Integer);
                        cmd.Parameters["stts"].Value = statusGroup.First().Select(s => (int) s.NewStatus).ToArray();
                        if (statusGroup.First().Count(s => s.ChangeFromStatus.HasValue) > 0)
                        {
                            cmd.Parameters.AddWithValue("sttsFrom", OracleDbType.Integer);
                            cmd.Parameters["sttsFrom"].Value = statusGroup.First()
                                .Select(s => s.ChangeFromStatus.HasValue ? (int) s.ChangeFromStatus.Value : -1).ToArray();
                        }

                        affectedRecords = cmd.ExecuteArray(infos.Count);
                    }

                    LogExecutionTimes(sql.ToString(), sw);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, ex.Message);
                    throw;
                }
                finally
                {
                    cn.Close();
                }

                // Insert Log record into the log table              
                if (affectedRecords > 0)
                {
                    InsertMutationStatusLog(infos);
                }
            }

            return affectedRecords;
        }

        /// <summary>
        ///     Resets the failed sync operations to the initial status.
        /// </summary>
        /// <param name="maxRetries">Maximum number of times a failed operation is reset.</param>
        /// <returns>Number of affected records</returns>
        public int ResetFailedSyncOperations(int maxRetries)
        {
            return ExecuteSql(SqlStatements.ResetFailedOperations, new[]
            {
                new OracleParameter {ParameterName = "maxNumberOfRetries", Value = maxRetries}
            });
        }


        /// <summary>
        ///     Inserts a new status record into the log table
        /// </summary>
        /// <param name="info">The information to log</param>
        /// <returns>Number of affected records</returns>
        // ReSharper disable once UnusedMethodReturnValue.Local
        private int InsertMutationStatusLog(MutationStatusInfo info)
        {
            var error = string.IsNullOrEmpty(info.ErrorMessage)
                ? null
                : info.ErrorMessage + Environment.NewLine + Environment.NewLine + info.StackTrace;
            return ExecuteSql(SqlStatements.SqlUpdateMutationActionLog, new[]
            {
                new OracleParameter {ParameterName = "mttn_id", Value = info.MutationId},
                new OracleParameter {ParameterName = "aktn_stts_hist", Value = info.NewStatus.ToString()},
                new OracleParameter {ParameterName = "error_grnd", Value = error}
            });
        }

        // ReSharper disable once UnusedMethodReturnValue.Local
        private int InsertMutationStatusLog(List<MutationStatusInfo> infos)
        {
            var errors = infos.Select(info =>
                string.IsNullOrEmpty(info.ErrorMessage) ? null : info.ErrorMessage + Environment.NewLine + Environment.NewLine + info.StackTrace);

            var paramMttnId = new OracleParameter
                {ParameterName = "mttn_id", OracleDbType = OracleDbType.Long, Value = infos.Select(i => i.MutationId).ToArray()};
            var paramAktnSttsHist = new OracleParameter
                {ParameterName = "aktn_stts_hist", OracleDbType = OracleDbType.VarChar, Value = infos.Select(i => i.NewStatus.ToString()).ToArray()};
            var paraErrorGrnd = new OracleParameter {ParameterName = "error_grnd", OracleDbType = OracleDbType.Integer, Value = errors.ToArray()};

            return ExecuteBatchSql(SqlStatements.SqlUpdateMutationActionLog, new[]
            {
                paramMttnId, paramAktnSttsHist, paraErrorGrnd
            }, infos.Count);
        }

        #endregion

        #region Unit of Description related

        /// <summary>
        ///     Gets the detailed information about one units of description
        /// </summary>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public ArchiveRecordDataSet.ArchiveRecordRow GetArchiveRecordRow(long recordId)
        {
            var ds = GetDataSetFromSql<ArchiveRecordDataSet>(SqlStatements.SqlArchiveRecordSelect, new[]
            {
                new OracleParameter {ParameterName = "vrzng_enht_id", Value = recordId}
            });

            if (!ds.ArchiveRecord.Any())
            {
                Console.Out.WriteLineAsync($"Should have received a record for id {recordId}");
            }

            return ds.ArchiveRecord.FirstOrDefault();
        }


        /// <summary>
        ///     Loads all the detail data records for an archive record.
        /// </summary>
        /// <param name="recordId">The record identifier.</param>
        /// <returns>DetailDataDataSet.</returns>
        public DetailDataDataSet LoadDetailData(long recordId)
        {
            return GetDataSetFromSql<DetailDataDataSet>(SqlStatements.SqlDataElementsSelect, new[]
            {
                new OracleParameter {ParameterName = "vrzng_enht_id", Value = recordId}
            });
        }

        /// <summary>
        ///     Loads all the archive record data that is required for an order
        /// </summary>
        /// <param name="recordId">The record identifier.</param>
        /// <returns>DetailDataDataSet.</returns>
        public OrderDetailData LoadOrderDetailData(long recordId)
        {
            var data = GetDataSetFromSql<DataSet>(SqlStatements.OrderDetailDataSelect, new[]
            {
                new OracleParameter {ParameterName = "vrzng_enht_id", Value = recordId}
            });
            if (data.Tables[0].Rows.Count == 0)
            {
                return null;
            }

            var row = data.Tables[0].Rows[0];
            return ConvertDataSetRowToOrderDetailData(row);
        }

        /// <summary>
        ///     Loads the node context of a specific record
        /// </summary>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public NodeContext LoadNodeContext(long recordId)
        {
            var retVal = new NodeContext();
            using (var cn = AISConnection.GetConnection())
            {
                var sw = new Stopwatch();

                try
                {
                    using (var cmd = new OracleCommand(SqlStatements.SqlNodeContext, cn))
                    {
                        // Command is Stored Procedure
                        cmd.CommandType = CommandType.StoredProcedure;


                        // Parameter setzen
                        cmd.Parameters.Add(new OracleParameter("pi_gsft_obj_bzhng_typ_id", OracleDbType.Integer)
                        {
                            Direction = ParameterDirection.Input,
                            Value = vrzngEnhtHierarchieBzhngTypId
                        });

                        cmd.Parameters.Add(new OracleParameter("pi_item_id", OracleDbType.Integer)
                        {
                            Direction = ParameterDirection.Input,
                            Value = recordId
                        });

                        cmd.Parameters.Add(new OracleParameter("pio_pred_id", OracleDbType.Integer)
                        {
                            Direction = ParameterDirection.Output,
                            Value = -1
                        });

                        cmd.Parameters.Add(new OracleParameter("pio_succ_id", OracleDbType.Integer)
                        {
                            Direction = ParameterDirection.Output,
                            Value = -1
                        });

                        cmd.Parameters.Add(new OracleParameter("pio_parent_id", OracleDbType.Integer)
                        {
                            Direction = ParameterDirection.Output,
                            Value = -1
                        });

                        cmd.Parameters.Add(new OracleParameter("pio_first_child_id", OracleDbType.Integer)
                        {
                            Direction = ParameterDirection.Output,
                            Value = -1
                        });
                        cmd.ExecuteNonQuery();

                        // Set the return values
                        retVal.ParentArchiveRecordId = Convert.ToInt32(cmd.Parameters["pio_parent_id"].Value).ToString();
                        retVal.FirstChildArchiveRecordId = Convert.ToInt32(cmd.Parameters["pio_first_child_id"].Value).ToString();
                        retVal.NextArchiveRecordId = Convert.ToInt32(cmd.Parameters["pio_succ_id"].Value).ToString();
                        retVal.PreviousArchiveRecordId = Convert.ToInt32(cmd.Parameters["pio_pred_id"].Value).ToString();
                        retVal.ArchiveRecordId = recordId.ToString();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, ex.Message);
                    throw;
                }
                finally
                {
                    cn.Close();
                    LogExecutionTimes(SqlStatements.SqlNodeContext, sw);
                }
            }

            return retVal;
        }

        /// <summary>
        ///     Loads the information for the archive plan.
        /// </summary>
        /// <param name="recordIdList">A list with primary keys of the archive records.</param>
        /// <returns>ArchivePlanInfoDataSet.</returns>
        public ArchivePlanInfoDataSet LoadArchivePlanInfo(long[] recordIdList)
        {
            var ids = string.Join(",", recordIdList);
            return GetDataSetFromSql<ArchivePlanInfoDataSet>(string.Format(SqlStatements.SqlArchivePlanInfo, ids));
        }

        /// <summary>
        ///     Loads information about a node in the archive plan
        /// </summary>
        /// <param name="recordId">The primary key of the archive record.</param>
        /// <returns>NodeInfoDataSet.</returns>
        public NodeInfoDataSet LoadNodeInfo(long recordId)
        {
            return GetDataSetFromSql<NodeInfoDataSet>(SqlStatements.SqlArchiveRecordNodeInfo, new[]
            {
                new OracleParameter {ParameterName = "vrzng_enht_id", Value = recordId}
            });
        }

        public List<string> LoadMetadataSecurityTokens(long recordId)
        {
            var retVal = new List<string>();

            var securityInfo = GetDataSetFromSql<DataSet>(SqlStatements.GetArchiveRecordSecurityInfo, new[]
            {
                new OracleParameter {ParameterName = "vrzng_enht_id", Value = recordId}
            });

            // We should have received exatly one record
            Debug.Assert(securityInfo.Tables[0].Rows.Count == 1, "securityInfo.Tables[0].Rows.Count == 1");
            var row = securityInfo.Tables[0].Rows[0];

            var accessTokens = row["access_tokens"] == DBNull.Value ? null : (string) row["access_tokens"];
            Log.Debug("Found the following metadata access tokens for archive record with id {recordId}: {accessTokens}", recordId, accessTokens);

            // accessTokens is a comma seperated list with the access Tokens:
            if (accessTokens != null)
            {
                var tokens = accessTokens.Split(',').Select(s => s.Trim().ToUpper());
                retVal.AddRange(tokens);
            }

            return retVal;
        }

        public List<string> LoadFieldSecurityTokens(long recordId)
        {
            var retVal = new List<string>();

            var securityInfo = GetDataSetFromSql<DataSet>(SqlStatements.GetArchiveRecordFieldSecurityInfo, new[]
            {
                new OracleParameter {ParameterName = "vrzng_enht_id", Value = recordId}
            });

            // We should have received exatly one record
            Debug.Assert(securityInfo.Tables[0].Rows.Count == 1, "securityInfo.Tables[0].Rows.Count == 1");
            var row = securityInfo.Tables[0].Rows[0];

            var securityTokens = row["access_tokens"] == DBNull.Value ? null : (string)row["access_tokens"];
            Log.Debug("Found the following security access tokens for archive record with id {recordId}: {securityTokens}", recordId, securityTokens);

            // accessTokens is a comma seperated list with the access Tokens:
            if (securityTokens != null)
            {
                var tokens = securityTokens.Split(',').Select(s => s.Trim().ToUpper());
                retVal.AddRange(tokens);
            }

            return retVal;
        }

        public PrimaryDataSecurityTokenResult LoadPrimaryDataSecurityTokens(long recordId)
        {
            var retVal = new PrimaryDataSecurityTokenResult();

            var securityInfo = GetDataSetFromSql<DataSet>(SqlStatements.GetArchiveRecordPrimaryDataSecurityInfo, new[]
            {
                new OracleParameter {ParameterName = "vrzng_enht_id", Value = recordId}
            });

            // We should have received exatly one record
            Debug.Assert(securityInfo.Tables[0].Rows.Count == 1, "securityInfo.Tables[0].Rows.Count == 1");
            var row = securityInfo.Tables[0].Rows[0];

            var downloadAccessTokens = row["download_tkn_list"].ToString();
            Log.Debug("Found the following download access tokens for archive record with id {recordId}: {downloadAccessTokens}", recordId,
                downloadAccessTokens);
            if (!string.IsNullOrEmpty(downloadAccessTokens))
            {
                var list = downloadAccessTokens.Split(',').Select(s => s.Trim().ToUpper());
                foreach (var item in list)
                {
                    retVal.DownloadAccessTokens.Add(item);
                }
            }

            var fulltextAccessTokens = row["fulltext_tkn_list"].ToString();
            Log.Debug("Found the following fulltext access tokens for archive record with id {recordId}: {fulltextAccessTokens}", recordId,
                fulltextAccessTokens);
            if (!string.IsNullOrEmpty(fulltextAccessTokens))
            {
                var list = fulltextAccessTokens.Split(',').Select(s => s.Trim().ToUpper());
                foreach (var item in list)
                {
                    retVal.FulltextAccessTokens.Add(item);
                }
            }

            return retVal;
        }

        public int InitiateFullResync()
        {
            try
            {
                return ExecuteSql(SqlStatements.InitiateFullResync);
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                return -1;
            }
        }

        public HarvestStatusInfo GetHarvestStatusInfo(QueryDateRange dataRange)
        {
            var retVal = new HarvestStatusInfo
            {
                NumberOfRecordsCurrentlySyncing = 0,
                NumberOfRecordsWaitingForSync = 0,
                NumberOfRecordsWithSyncFailure = 0,
                NumberOfRecordsWithSyncSuccess = 0
            };

            try
            {
                var ds = GetDataSetFromSql<DataSet>(SqlStatements.HarvestStatusInfo, new[]
                {
                    new OracleParameter("fromDate", OracleDbType.Date) {Value = dataRange.From},
                    new OracleParameter("toDate", OracleDbType.Date) {Value = dataRange.To}
                });

                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    switch ((int) row["aktn_stts"])
                    {
                        case (int) ActionStatus.WaitingForSync:
                            if (row["counttype"].ToString().ToLowerInvariant() == "total")
                            {
                                retVal.TotalNumberOfRecordsWaitingForSync = (int) (decimal) row["cnt"];
                            }
                            else
                            {
                                retVal.NumberOfRecordsWaitingForSync = (int) (decimal) row["cnt"];
                            }

                            break;
                        case (int) ActionStatus.SyncInProgress:
                            if (row["counttype"].ToString().ToLowerInvariant() == "total")
                            {
                                retVal.TotalNumberOfRecordsCurrentlySyncing = (int) (decimal) row["cnt"];
                            }
                            else
                            {
                                retVal.NumberOfRecordsCurrentlySyncing = (int) (decimal) row["cnt"];
                            }

                            break;
                        case (int) ActionStatus.SyncCompleted:
                            if (row["counttype"].ToString().ToLowerInvariant() == "total")
                            {
                                retVal.TotalNumberOfRecordsWithSyncSuccess = (int) (decimal) row["cnt"];
                            }
                            else
                            {
                                retVal.NumberOfRecordsWithSyncSuccess = (int) (decimal) row["cnt"];
                            }

                            break;
                        case (int) ActionStatus.SyncFailed:
                            if (row["counttype"].ToString().ToLowerInvariant() == "total")
                            {
                                retVal.TotalNumberOfRecordsWithSyncFailure = (int) (decimal) row["cnt"];
                            }
                            else
                            {
                                retVal.NumberOfRecordsWithSyncFailure = (int) (decimal) row["cnt"];
                            }

                            break;
                        case (int) ActionStatus.SyncAborted:
                            // Not relevant for us here
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                retVal = new HarvestStatusInfo
                {
                    NumberOfRecordsCurrentlySyncing = -1,
                    NumberOfRecordsWaitingForSync = -1,
                    NumberOfRecordsWithSyncFailure = -1,
                    NumberOfRecordsWithSyncSuccess = -1
                };
            }

            return retVal;
        }

        public HarvestLogInfoResult GetHarvestLogInfo(HarvestLogInfoRequest request)
        {
            var retVal = new HarvestLogInfoResult();
            var sql = SqlStatements.HarvestLogInfo;

            sql = string.Format(sql,
                string.IsNullOrEmpty(request.ArchiveRecordIdFilter) ? "" : " AND m.gsft_obj_id = :gsft_obj_id ",
                request.ActionStatusFilterList.Any()
                    ? $" AND aktn_stts in ({string.Join(",", request.ActionStatusFilterList.Select(a => ((int) a).ToString()))}) "
                    : "",
                "" /* no sort order yet */);


            using (var cn = AISConnection.GetConnection())
            {
                var dateRange = new QueryDateRange(request.DateRangeFilter);
                var cmd = new OracleCommand(sql, cn);
                cmd.Parameters.Add("cur1", OracleDbType.Cursor, ParameterDirection.Output);
                cmd.Parameters.Add("cur2", OracleDbType.Cursor, ParameterDirection.Output);
                cmd.Parameters.AddWithValue("dateFrom", dateRange.From);
                cmd.Parameters.AddWithValue("dateTo", dateRange.To);
                cmd.Parameters.AddWithValue("recordFrom", request.PageSize * (request.Page - 1));
                cmd.Parameters.AddWithValue("recordTo", request.PageSize * request.Page);
                if (!string.IsNullOrEmpty(request.ArchiveRecordIdFilter))
                {
                    cmd.Parameters.AddWithValue("gsft_obj_id", request.ArchiveRecordIdFilter);
                }

                var ds = new DataSet();
                var da = new OracleDataAdapter(cmd);
                da.TableMappings.Add("Table", "ResultCount");
                da.TableMappings.Add("Table1", "LogInfo");
                da.Fill(ds);

                var dsDetail = new DataSet();
                if (ds.Tables["LogInfo"].Rows.Count > 0)
                {
                    var ids = string.Join(",", ds.Tables["LogInfo"].AsEnumerable().Select(t => (int) t.Field<double>("mttn_id")));
                    dsDetail = GetDataSetFromSql<DataSet>(string.Format(SqlStatements.HarvestLogInfoDetail, ids));
                }

                retVal.TotalResultSetSize = (int) (decimal) ds.Tables["ResultCount"].Rows[0][0];
                retVal.ResultSet = (from r in ds.Tables["LogInfo"].AsEnumerable()
                        select HarvestLogInfo(r, dsDetail)
                    ).ToList();
            }

            return retVal;
        }

        /// <summary>
        ///     Loads all the fond links
        ///     (Ordnungskomponente-Link)
        /// </summary>
        /// <returns>List&lt;FondLink&gt;.</returns>
        public List<FondLink> LoadFondLinks()
        {
            var ds = GetDataSetFromSql<DataSet>(SqlStatements.FondsOverviewList);
            var items = from r in ds.Tables[0].AsEnumerable()
                select new FondLink {HierarchyPath = r.Field<string>("hrch_pfad"), LinkName = CleanFondName(r.Field<string>("memo_txt"))};

            return items.ToList();
        }

        /// <summary>
        ///     Returns the linked accession (Ablieferung) to an archive record.
        ///     Every item that can be ordered has (or should have) a link the the accession.
        ///     If the link element cannot be found null is returned.
        /// </summary>
        /// <param name="recordId">The record identifier.</param>
        /// <returns>AccessionDataSet.AcessionRecordRow.</returns>
        public AccessionDataSet.AcessionRecordRow GetLinkedAccessionToArchiveRecord(long recordId)
        {
            // Get the data element for the accession link
            var table = GetDetailDataForElement(recordId, (int) ScopeArchivDatenElementId.AblieferungLink);
            if (table != null && table.Rows.Count > 0)
            {
                var accessionId = table.AsEnumerable().First().VRKNP_GSFT_OBJ_ID;
                var ds = GetDataSetFromSql<AccessionDataSet>(SqlStatements.GetAccession, new[]
                {
                    new OracleParameter("ablfr_id", OracleDbType.Integer) {Value = accessionId}
                });
                if (ds.AcessionRecord.Count == 1)
                {
                    return ds.AcessionRecord[0];
                }
            }

            return null;
        }

        /// <summary>
        ///     Gets all the detail data rows for a specific record and data element.
        ///     It is possible that no rows are returned, only exactly one row, or even multiple rows.
        ///     The returned elements can be ordered using the elmnt_sqnz_nr.
        /// </summary>
        /// <param name="recordId">The archive record identifier.</param>
        /// <param name="dataElementId">The data element identifier.</param>
        /// <returns>DetailDataDataSet.DetailDataRow.</returns>
        public DetailDataDataSet.DetailDataDataTable GetDetailDataForElement(long recordId, int dataElementId)
        {
            var ds = GetDataSetFromSql<DetailDataDataSet>(SqlStatements.GetDetailDataForDataElement, new[]
            {
                new OracleParameter("gsft_obj_id", OracleDbType.Integer) {Value = recordId},
                new OracleParameter("daten_elmnt_id", OracleDbType.Integer) {Value = dataElementId}
            });

            return ds.DetailData;
        }

        /// <summary>
        ///     Gets the name of the business object (Geschäftsobjekt Kurzname).
        /// </summary>
        /// <param name="recordId">The record identifier.</param>
        /// <returns>System.String.</returns>
        public string GetBusinessObjectIdName(long recordId)
        {
            return ExecuteSqlScalar<string>("select gsft_obj_kurz_nm from tbs_gsft_obj where gsft_obj_id = :gsft_obj_id", new[]
            {
                new OracleParameter("gsft_obj_id", OracleDbType.Integer) {Value = recordId}
            });
        }

        /// <summary>
        ///     Gets a list with record ids for the children of an archive record.
        /// </summary>
        /// <param name="recordId">The record identifier.</param>
        /// <returns>List&lt;System.Int64&gt;.</returns>
        public List<OrderDetailData> GetChildrenRecordOrderDetailDataForArchiveRecord(long recordId)
        {
            var ds = GetDataSetFromSql<DataSet>(SqlStatements.OrderDetailDataSelectForChildRecords, new[]
            {
                new OracleParameter {ParameterName = "vrzng_enht_id", Value = recordId}
            });

            var data = ds.Tables[0].AsEnumerable().Select(ConvertDataSetRowToOrderDetailData);

            return data.ToList();
        }

        /// <summary>
        ///     Gets a list with archive record ids that are found in the same container.
        /// </summary>
        /// <param name="containerId">The container identifier.</param>
        /// <returns>List&lt;System.Int64&gt;.</returns>
        public List<OrderDetailData> GetArchiveRecordOrderDetailDataForContainer(long containerId)
        {
            var ds = GetDataSetFromSql<DataSet>(SqlStatements.OrderDetailDataSelectForContainer, new[]
            {
                new OracleParameter {ParameterName = "bhltn_id", Value = containerId}
            });

            var data = ds.Tables[0].AsEnumerable().Select(ConvertDataSetRowToOrderDetailData);

            return data.ToList();
        }

        public string GetDbVersion()
        {
            return ExecuteSqlScalar<string>("select optn_vchr from tba_optn where optn_id = :optn_id", new[]
            {
                new OracleParameter("optn_id", OracleDbType.Integer) {Value = 33}
            });
        }

        private string CleanFondName(string text)
        {
            // Removes the prefix and the trailing text 
            // e.g. CH-BAR*/614 Münzwesen, Edelmetalle (Gliederungseinheit) ==> CH-BAR*/614 Münzwesen, Edelmetalle
            var pattern = @"(^.*\*/)(.*)(\s+\(\w+\)\s*)+$";
            var myRegex = new Regex(pattern, RegexOptions.IgnoreCase);
            return myRegex.Replace(text, "$2");
        }

        #endregion

        #region Related objects to Unit of Description

        /// <summary>
        ///     Loads all the containers to an archive record.
        /// </summary>
        /// <param name="recordId">The record identifier.</param>
        /// <returns>ContainerDataSet.</returns>
        public ContainerDataSet LoadContainers(long recordId)
        {
            return GetDataSetFromSql<ContainerDataSet>(SqlStatements.SqlArchiveRecordContainers, new[]
            {
                new OracleParameter {ParameterName = "vrzng_enht_id", Value = recordId}
            });
        }

        /// <summary>
        ///     Loads the descriptors for an archive record
        /// </summary>
        public DescriptorDataSet LoadDescriptors(long recordId)
        {
            var exludedIds = applicationSettings.ExcludedThesaurenIds;
            var sql = string.Format(SqlStatements.SqlArchiveRecordDescriptors, string.Join(",", exludedIds ?? new[] {-1}));
            var descriptorDataSet =  GetDataSetFromSql<DescriptorDataSet>(sql, new[]
            {
                new OracleParameter {ParameterName = "vrzng_enht_id", Value = recordId}
            });
            return descriptorDataSet;
        }

        /// <summary>
        ///     Gets all the references to an archive record
        /// </summary>
        /// <param name="recordId">The record identifier.</param>
        /// <returns>ReferencesDataSet.</returns>
        public ReferencesDataSet LoadReferences(long recordId)
        {
            return GetDataSetFromSql<ReferencesDataSet>(SqlStatements.SqlArchiveRecordReferences, new[]
            {
                new OracleParameter {ParameterName = "gsft_obj_id", Value = recordId}
            });
        }

        #endregion

        #region private Methods

        private HarvestLogInfo HarvestLogInfo(DataRow r, DataSet dsDetail)
        {
            return new HarvestLogInfo
            {
                MutationId = (int)r.Field<double>("mttn_id"),
                ArchiveRecordId = r.Field<double>("gsft_obj_id").ToString("F0"),
                ArchiveRecordIdName = r.Field<string>("gsft_obj_kurz_nm"),
                ActionName = r.Field<string>("aktn"),
                CurrentStatus = (ActionStatus)r.Field<int>("aktn_stts"),
                CreationDate = r.Field<DateTime>("erfsg_dt"),
                LastChangeDate = r.Field<DateTime>("mttn_dt"),
                NumberOfSyncRetries = r.Field<int>("sync_anz_vrsch"),
                Details = GetLogDetails(dsDetail, (int)r.Field<double>("mttn_id"))
            };
        }
        private List<HarvestLogInfoDetail> GetLogDetails(DataSet dsDetail, int parentRecord)
        {
            // If there is no data, then return.
            if (dsDetail.Tables.Count == 0)
            {
                return new List<HarvestLogInfoDetail>();
            }

            var result = from d in dsDetail.Tables[0].AsEnumerable()
                where (int)d.Field<double>("mttn_id") == parentRecord
                select new HarvestLogInfoDetail
                {
                    MutationActionDetailId = (int)d.Field<double>("mttn_aktn_id"),
                    MutationId = parentRecord,
                    ActionStatus = (ActionStatus)Enum.Parse(typeof(ActionStatus), d.Field<string>("AKTN_STTS_HIST")),
                    ActionDate = d.Field<DateTime>("AKTN_STTS_DT"),
                    ErrorReason = d.Field<string>("ERROR_GRND")
                };
            return result.ToList();
        }

        #endregion

        #region Basic ADO helper methods

        /// <summary>
        ///     Gets a generic DataSet given an SQL statement and optional parameters.
        /// </summary>
        /// <typeparam name="T">A DataSet or a typed DataSet</typeparam>
        /// <param name="sql">The sql statement to execute</param>
        /// <param name="parameters">An array or OracleParameters if needed.</param>
        /// <returns>The populated DataSet</returns>
        private T GetDataSetFromSql<T>(string sql, OracleParameter[] parameters = null) where T : DataSet, new()
        {
            var ds = new T();
            using (var cn = AISConnection.GetConnection())
            {
                var sw = new Stopwatch();
                sw.Start();
                try
                {
                    using (var cmd = new OracleCommand(sql, cn))
                    {
                        if (parameters != null)
                        {
                            foreach (var parameter in parameters)
                            {
                                cmd.Parameters.Add(parameter);
                            }
                        }

                        var da = new OracleDataAdapter(cmd);
                        if (ds.Tables.Count > 0)
                        {
                            da.Fill(ds.Tables[0]);
                        }
                        else
                        {
                            da.Fill(ds);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, ex.Message);
                    throw;
                }
                finally
                {
                    cn.Close();
                    LogExecutionTimes(sql, sw);
                }
            }

            return ds;
        }

        /// <summary>
        ///     Executes a simple SQL statement against the db.
        /// </summary>
        /// <param name="sql">The SQL.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The number of affected records</returns>
        private int ExecuteSql(string sql, OracleParameter[] parameters = null)
        {
            using (var cn = AISConnection.GetConnection())
            {
                var sw = new Stopwatch();
                sw.Start();

                try
                {
                    using (var cmd = new OracleCommand(sql, cn))
                    {
                        if (parameters != null)
                        {
                            foreach (var parameter in parameters)
                            {
                                cmd.Parameters.Add(parameter);
                            }
                        }

                        return cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, ex.Message);
                    throw;
                }
                finally
                {
                    cn.Close();
                    LogExecutionTimes(sql, sw);
                }
            }
        }

        /// <summary>
        ///     Executes a simple SQL statement against the db for one or more values.
        /// </summary>
        /// <param name="sql">The SQL.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="elementCount">The number of elements in the batch.</param>
        /// <returns>The number of affected records</returns>
        private int ExecuteBatchSql(string sql, OracleParameter[] parameters, int elementCount)
        {
            using (var cn = AISConnection.GetConnection())
            {
                var sw = new Stopwatch();
                sw.Start();

                try
                {
                    using (var cmd = new OracleCommand(sql, cn))
                    {
                        if (parameters != null)
                        {
                            foreach (var parameter in parameters)
                            {
                                cmd.Parameters.Add(parameter);
                            }
                        }

                        return cmd.ExecuteArray(elementCount);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, ex.Message);
                    throw;
                }
                finally
                {
                    cn.Close();
                    LogExecutionTimes(sql, sw);
                }
            }
        }

        /// <summary>
        ///     Executes a simple SQL statement against the db.
        /// </summary>
        /// <param name="sql">The SQL.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The number of affected records</returns>
        private T ExecuteSqlScalar<T>(string sql, OracleParameter[] parameters = null)
        {
            using (var cn = AISConnection.GetConnection())
            {
                var sw = new Stopwatch();
                sw.Start();

                try
                {
                    using (var cmd = new OracleCommand(sql, cn))
                    {
                        if (parameters != null)
                        {
                            foreach (var parameter in parameters)
                            {
                                cmd.Parameters.Add(parameter);
                            }
                        }

                        var obj = cmd.ExecuteScalar();
                        return (T) obj;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, ex.Message);
                    throw;
                }
                finally
                {
                    cn.Close();
                    LogExecutionTimes(sql, sw);
                }
            }
        }

        /// <summary>
        ///     Logs the execution times of the SQL statements.
        ///     Statements with execution time larger than 50ms are put as informaion, statements with more than 100ms as warnings.
        /// </summary>
        /// <param name="sql">The SQL statement in question.</param>
        /// <param name="sw">The running stopwatch.</param>
        private static void LogExecutionTimes(string sql, Stopwatch sw)
        {
            var executionTime = sw.ElapsedMilliseconds;
            sw.Stop();
            if (applicationSettings.OutputSQLExecutionTimes)
            {
                if (executionTime < 500)
                {
                    Log.Verbose("Execution time of sql statement took {ExecutionTime}. Statement hash {Hash}. Statement: {Sql}", executionTime,
                        HashUtilities.GetMd5Hash(sql), sql);
                }
                else if (executionTime >= 500 && executionTime < 999)
                {
                    Log.Information("Execution time of sql statement took {ExecutionTime}. Statement hash {Hash}. Statement: {Sql}", executionTime,
                        HashUtilities.GetMd5Hash(sql), sql);
                }
                else if (executionTime >= 1000)
                {
                    Log.Warning("Execution time of sql statement took {ExecutionTime}. Statement hash {Hash}. Statement: {Sql}", executionTime,
                        HashUtilities.GetMd5Hash(sql), sql);
                }
            }
        }

        #endregion
    }
}