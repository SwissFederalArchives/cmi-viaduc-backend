using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Transactions;
using Serilog;

namespace CMI.Access.Sql.Viaduc
{
    public class FavoriteDataAccess
    {
        private readonly string connectionString;

        public FavoriteDataAccess(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public IEnumerable<FavoriteList> GetAllLists(string uid)
        {
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText =
                        "SELECT ID, Name, Comment, (SELECT COUNT(*) FROM Favorite WHERE (Favorite.List=FavoriteList.ID)) as ChildCount " +
                        "FROM FavoriteList " +
                        "WHERE UserID = @p1";
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = uid,
                        ParameterName = "p1",
                        SqlDbType = SqlDbType.NVarChar
                    });

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            yield return new FavoriteList
                            {
                                Id = Convert.ToInt32(reader["ID"]),
                                Name = reader["Name"] as string,
                                Comment = reader["Comment"] as string,
                                NumberOfItems = Convert.ToInt32(reader["ChildCount"])
                            };
                        }
                    }
                }
            }
        }

        public FavoriteList GetList(string uid, int listId)
        {
            FavoriteList list = null;
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();

                if (!DoesListBelongToUser(uid, listId, cn))
                {
                    return null;
                }

                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText =
                        "SELECT ID, Name, Comment, (SELECT COUNT(*) FROM Favorite WHERE (Favorite.List=FavoriteList.ID)) as ChildCount " +
                        "FROM FavoriteList " +
                        "WHERE ID = @p1";
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = listId,
                        ParameterName = "p1",
                        SqlDbType = SqlDbType.NVarChar
                    });

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            list = new FavoriteList
                            {
                                Id = Convert.ToInt32(reader["ID"]),
                                Name = reader["Name"] as string,
                                Comment = reader["Comment"] as string,
                                NumberOfItems = Convert.ToInt32(reader["ChildCount"])
                            };
                        }
                    }
                }
            }

            return list;
        }

        public FavoriteList AddList(string uid, string listName, string comment)
        {
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO FavoriteList (Name, UserId, Comment) OUTPUT INSERTED.ID VALUES (@p1, @p2,@p3)";
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = listName,
                        ParameterName = "p1",
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = uid,
                        ParameterName = "p2",
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = comment.ToDbParameterValue(),
                        ParameterName = "p3",
                        SqlDbType = SqlDbType.NVarChar
                    });

                    var id = Convert.ToInt32(cmd.ExecuteScalar());

                    return new FavoriteList
                    {
                        Id = id,
                        Name = listName,
                        Comment = comment,
                        NumberOfItems = 0
                    };
                }
            }
        }

        public void RemoveList(string uid, int listId)
        {
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();

                if (!DoesListBelongToUser(uid, listId, cn))
                {
                    return;
                }

                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Favorite WHERE list = @p1;" +
                                      "DELETE FROM FavoriteList WHERE ID = @p1";
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = listId,
                        ParameterName = "p1",
                        SqlDbType = SqlDbType.Int
                    });
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public IFavorite AddFavorite(string uid, int listId, IFavorite favorite)
        {
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();

                if (!DoesListBelongToUser(uid, listId, cn))
                {
                    return null;
                }

                favorite.CreatedAt = DateTime.Now;

                if (favorite is VeFavorite veFavorite)
                {
                    favorite = CreateVeFavorite(listId, cn, veFavorite);
                }
                else if (favorite is SearchFavorite sFavorite)
                {
                    favorite = CreateSearchFavorite(listId, cn, sFavorite);
                }
                else
                {
                    throw new ArgumentOutOfRangeException("favorite", "This kind of favorite is not implemented yet.");
                }

                return favorite;
            }
        }

        private VeFavorite CreateVeFavorite(int listId, SqlConnection cn, VeFavorite veFavorite)
        {
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO Favorite (List, Ve, Kind, CreatedAt) OUTPUT INSERTED.ID VALUES (@p1, @p2, @p3, @p4)";
                cmd.Parameters.Add(new SqlParameter
                {
                    Value = listId,
                    ParameterName = "p1",
                    SqlDbType = SqlDbType.Int
                });
                cmd.Parameters.Add(new SqlParameter
                {
                    Value = veFavorite.VeId,
                    ParameterName = "p2",
                    SqlDbType = SqlDbType.Int
                });
                cmd.Parameters.Add(new SqlParameter
                {
                    Value = (int) FavoriteKind.Ve,
                    ParameterName = "p3",
                    SqlDbType = SqlDbType.TinyInt
                });
                cmd.Parameters.Add(new SqlParameter
                {
                    Value = veFavorite.CreatedAt,
                    ParameterName = "p4",
                    SqlDbType = SqlDbType.DateTime
                });

                var id = Convert.ToInt32(cmd.ExecuteScalar());
                veFavorite.Id = id;
            }

            return veFavorite;
        }

        private SearchFavorite CreateSearchFavorite(int listId, SqlConnection cn, SearchFavorite sFavorite)
        {
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO Favorite (List, Url, Title, Kind, CreatedAt) OUTPUT INSERTED.ID VALUES (@p1, @p2, @p3, @p4, @p5)";
                cmd.Parameters.Add(new SqlParameter
                {
                    Value = listId,
                    ParameterName = "p1",
                    SqlDbType = SqlDbType.Int
                });
                cmd.Parameters.Add(new SqlParameter
                {
                    Value = sFavorite.Url,
                    ParameterName = "p2",
                    SqlDbType = SqlDbType.NVarChar
                });
                cmd.Parameters.Add(new SqlParameter
                {
                    Value = sFavorite.Title,
                    ParameterName = "p3",
                    SqlDbType = SqlDbType.NVarChar
                });
                cmd.Parameters.Add(new SqlParameter
                {
                    Value = (int) FavoriteKind.Search,
                    ParameterName = "p4",
                    SqlDbType = SqlDbType.TinyInt
                });
                cmd.Parameters.Add(new SqlParameter
                {
                    Value = sFavorite.CreatedAt,
                    ParameterName = "p5",
                    SqlDbType = SqlDbType.DateTime
                });

                var id = Convert.ToInt32(cmd.ExecuteScalar());
                sFavorite.Id = id;
                return sFavorite;
            }
        }

        public void RemoveFavorite(string uid, int listId, int id)
        {
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();

                if (!DoesListBelongToUser(uid, listId, cn))
                {
                    return;
                }

                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Favorite WHERE List = @p1 AND ID = @p2";
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = listId,
                        ParameterName = "p1",
                        SqlDbType = SqlDbType.Int
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = id,
                        ParameterName = "p2",
                        SqlDbType = SqlDbType.Int
                    });

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public IEnumerable<int> GetListContainingFavorite(string uid, string url)
        {
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();

                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText =
                        "SELECT FavoriteList.ID FROM FavoriteList JOIN Favorite ON Favorite.List = Favoritelist.ID WHERE UserId = @p1 and Url=@p2";
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = uid,
                        ParameterName = "p1",
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = url,
                        ParameterName = "p2",
                        SqlDbType = SqlDbType.NVarChar
                    });

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            yield return Convert.ToInt32(reader[0]);
                        }
                    }
                }
            }
        }

        public IEnumerable<int> GetListsContainingVe(string uid, string veId)
        {
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();

                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText =
                        "SELECT FavoriteList.ID FROM FavoriteList JOIN Favorite ON Favorite.List = Favoritelist.ID WHERE UserId = @p1 and Ve=@p2";
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = uid,
                        ParameterName = "p1",
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = veId,
                        ParameterName = "p2",
                        SqlDbType = SqlDbType.Int
                    });

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            yield return Convert.ToInt32(reader[0]);
                        }
                    }
                }
            }
        }

        public IEnumerable<IFavorite> GetFavoritesContainedOnList(string uid, int listId)
        {
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();

                if (!DoesListBelongToUser(uid, listId, cn))
                {
                    yield break;
                }

                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "SELECT ID, Kind, Ve, Title, Url, CreatedAt FROM Favorite WHERE List = @p1";
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = listId,
                        ParameterName = "p1",
                        SqlDbType = SqlDbType.Int
                    });

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var kindValue = reader.GetValue(1);
                            var type = (FavoriteKind) Enum.Parse(typeof(FavoriteKind), kindValue.ToString());
                            var id = reader.GetInt32(0);
                            var createdAt = reader.GetDateTime(5);

                            if (type == FavoriteKind.Search)
                            {
                                yield return new SearchFavorite
                                {
                                    CreatedAt = createdAt,
                                    Kind = type,
                                    Id = id,
                                    Title = reader.GetString(3),
                                    Url = reader.GetString(4)
                                };
                            }
                            else
                            {
                                yield return new VeFavorite
                                {
                                    CreatedAt = createdAt,
                                    Kind = type,
                                    Id = id,
                                    VeId = reader.GetInt32(2)
                                };
                            }
                        }
                    }
                }
            }
        }

        public void RenameList(string uid, int listId, string newName)
        {
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();

                if (!DoesListBelongToUser(uid, listId, cn))
                {
                    return;
                }

                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE FavoriteList SET Name = @p1 WHERE ID = @p2";
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = newName,
                        ParameterName = "p1",
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = listId,
                        ParameterName = "p2",
                        SqlDbType = SqlDbType.Int
                    });

                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static bool DoesListBelongToUser(string uid, int listId, SqlConnection cn)
        {
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM FavoriteList WHERE UserId = @p1 AND ID = @p2";
                cmd.Parameters.Add(new SqlParameter
                {
                    Value = uid,
                    ParameterName = "p1",
                    SqlDbType = SqlDbType.NVarChar
                });
                cmd.Parameters.Add(new SqlParameter
                {
                    Value = listId,
                    ParameterName = "p2",
                    SqlDbType = SqlDbType.Int
                });

                return Convert.ToInt32(cmd.ExecuteScalar()) == 1;
            }
        }

        public PendingMigrationCheckResult HasPendingMigrations(string email)
        {
            var retVal = new PendingMigrationCheckResult();
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();

                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText =
                        "SELECT email, quelle, count(liste) as anzahl " +
                        "FROM tempMigrationWorkspace " +
                        "WHERE email = @p1 " +
                        "GROUP BY email, quelle";
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = email,
                        ParameterName = "p1",
                        SqlDbType = SqlDbType.NVarChar
                    });

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader["quelle"].ToString().Equals("local", StringComparison.InvariantCultureIgnoreCase))
                            {
                                retVal.PendingLocal = Convert.ToInt32(reader["anzahl"]);
                            }

                            if (reader["quelle"].ToString().Equals("public", StringComparison.InvariantCultureIgnoreCase))
                            {
                                retVal.PendingPublic = Convert.ToInt32(reader["anzahl"]);
                            }
                        }
                    }
                }
            }

            return retVal;
        }

        public bool MigrateFavorites(string uid, string userEmailAddress, string source)
        {
            try
            {
                Log.Information("Starting migration of favorites for {userEmailAddress}", userEmailAddress);
                using (var ts = new TransactionScope(TransactionScopeOption.Required, TimeSpan.FromMinutes(30)))
                {
                    using (var cn = new SqlConnection(connectionString))
                    {
                        // Read all favorites lists
                        var getListsSql = "SELECT email, quelle, liste, MAX(Kommentar) AS kommentar " +
                                          "FROM TempMigrationWorkspace " +
                                          "WHERE email = @email AND quelle = @source " +
                                          "GROUP BY email, quelle, liste";
                        var da = new SqlDataAdapter(getListsSql, cn);
                        da.SelectCommand.AddParameter("email", SqlDbType.NVarChar, userEmailAddress);
                        da.SelectCommand.AddParameter("source", SqlDbType.NVarChar, source);
                        var dt = new DataTable();
                        da.Fill(dt);

                        var existingLists = GetAllLists(uid).ToList();

                        foreach (var row in dt.AsEnumerable())
                        {
                            var listName = row["liste"].ToString();
                            var comment = row["kommentar"].ToString();
                            var newList = existingLists.FirstOrDefault(l => l.Name.Equals(listName, StringComparison.InvariantCultureIgnoreCase)) ??
                                          AddList(uid, listName, comment);

                            if (newList != null)
                            {
                                Log.Information("Copy details to favorite list <{Name}>", newList.Name);
                                // Import the VE to this list
                                MigrateFavoritesDetails(newList, uid, userEmailAddress, source);
                            }
                        }
                    }

                    // Delete the data from the table, so user cannot import again.
                    DeleteMigrationData(userEmailAddress, source);

                    ts.Complete();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpexted error during favorite migration.");
            }

            return false;
        }

        private void DeleteMigrationData(string userEmailAddress, string source)
        {
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                var cmd = new SqlCommand("Delete from TempMigrationWorkspace where email = @email and quelle = @source", cn);
                cmd.AddParameter("email", SqlDbType.NVarChar, userEmailAddress);
                cmd.AddParameter("source", SqlDbType.NVarChar, source);
                cmd.ExecuteNonQuery();
            }
        }

        private void MigrateFavoritesDetails(FavoriteList favoriteList, string uid, string userEmailAddress, string source)
        {
            using (var cn = new SqlConnection(connectionString))
            {
                // Read all favorites lists
                var getListsSql = "SELECT email, quelle, liste, verzeichnungseinheit_id " +
                                  "FROM TempMigrationWorkspace " +
                                  "WHERE email = @email AND quelle = @source and liste = @liste";
                var da = new SqlDataAdapter(getListsSql, cn);
                da.SelectCommand.AddParameter("email", SqlDbType.NVarChar, userEmailAddress);
                da.SelectCommand.AddParameter("source", SqlDbType.NVarChar, source);
                da.SelectCommand.AddParameter("liste", SqlDbType.NVarChar, favoriteList.Name);
                var dt = new DataTable();
                da.Fill(dt);

                var existingItems = GetFavoritesContainedOnList(uid, favoriteList.Id).ToList();

                foreach (var row in dt.AsEnumerable())
                {
                    var veId = Convert.ToInt32(row["verzeichnungseinheit_id"]);
                    var existingItem = existingItems.Cast<VeFavorite>().FirstOrDefault(l => l.VeId == veId);

                    if (existingItem == null)
                    {
                        var newItem = new VeFavorite
                        {
                            VeId = veId,
                            Kind = FavoriteKind.Ve
                        };

                        // Import the VE to this list
                        Log.Information("Inserting Ve {veId} to list {name}", veId, favoriteList.Name);
                        AddFavorite(uid, favoriteList.Id, newItem);
                    }
                }
            }
        }
    }
}