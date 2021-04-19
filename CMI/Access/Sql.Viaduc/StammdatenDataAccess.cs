using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using CMI.Contract.Messaging;

namespace CMI.Access.Sql.Viaduc
{
    public class StammdatenDataAccess
    {
        private readonly string connectionString;


        public StammdatenDataAccess(string connectionString)
        {
            this.connectionString = connectionString;
        }


        public IEnumerable<NameAndId> GetReasons(string language)
        {
            return GetNamesAndIds("Reason", language);
        }

        public IEnumerable<NameAndId> GetArtDerArbeiten(string language)
        {
            return GetNamesAndIds("ArtDerArbeit", language);
        }


        public IEnumerable<NameAndId> GetNamesAndIds(string table, string language)
        {
            var columnName = "Name_" + language;

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT ID, {columnName} FROM {table}";

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            yield return new NameAndId
                            {
                                Id = Convert.ToInt32(reader["ID"]),
                                Name = reader[columnName] as string
                            };
                        }
                    }
                }
            }
        }
    }
}