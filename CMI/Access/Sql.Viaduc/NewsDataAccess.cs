using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using CMI.Contract.Management;
using Serilog;

namespace CMI.Access.Sql.Viaduc
{
    public class NewsDataAccess
    {
        private readonly string connectionString;
        private readonly string[] validLanguages = {"de", "fr", "en", "it"};

        public NewsDataAccess(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public List<INewsForOneLanguage> GetRelevantNewsForViaducClient(string language)
        {
            if (language == null)
            {
                throw new ArgumentNullException(nameof(language));
            }

            if (!validLanguages.Contains(language.ToLowerInvariant()))
            {
                throw new ArgumentException(nameof(language)); // Prevent SQL Injection
            }

            return ExecuteQuery(
                $"SELECT FromDate, ToDate, {language}Header, {language} FROM News WHERE FromDate <= GETDATE() AND ToDate >= GETDATE() ORDER BY FromDate DESC, ToDate ASC",
                reader => CreateNewsForOneLanguageFromReader(reader, language)).ToList();
        }

        public List<INews> GetAllNewsForManagementClient()
        {
            return ExecuteQuery("SELECT * FROM News ORDER BY FromDate DESC, ToDate ASC", CreateNewsFromReader).ToList();
        }

        private SqlConnection CreateConnection()
        {
            return new SqlConnection(connectionString);
        }

        private IEnumerable<T> ExecuteQuery<T>(string commandText, Func<SqlDataReader, T> readFunc)
        {
            using (var connection = CreateConnection())
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = commandText;

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            yield return readFunc(reader);
                        }
                    }
                }
            }
        }

        private INews CreateNewsFromReader(SqlDataReader sqlDataReader)
        {
            if (sqlDataReader == null)
            {
                throw new ArgumentNullException(nameof(sqlDataReader));
            }

            return new News
            {
                Id = sqlDataReader["ID"].ToString(),
                FromDate = CreateReadableTimestamp((DateTime) sqlDataReader["FromDate"]),
                ToDate = CreateReadableTimestamp((DateTime) sqlDataReader["ToDate"]),
                De = sqlDataReader["De"].ToString(),
                En = sqlDataReader["En"].ToString(),
                Fr = sqlDataReader["Fr"].ToString(),
                It = sqlDataReader["It"].ToString(),
                DeHeader = sqlDataReader["DeHeader"].ToString(),
                EnHeader = sqlDataReader["EnHeader"].ToString(),
                FrHeader = sqlDataReader["FrHeader"].ToString(),
                ItHeader = sqlDataReader["ItHeader"].ToString()
            };
        }

        private INewsForOneLanguage CreateNewsForOneLanguageFromReader(SqlDataReader sqlDataReader, string language)
        {
            if (sqlDataReader == null)
            {
                throw new ArgumentNullException(nameof(sqlDataReader));
            }

            return new NewsForOneLanguage
            {
                FromDate = CreateReadableTimestamp((DateTime) sqlDataReader["FromDate"]),
                ToDate = CreateReadableTimestamp((DateTime) sqlDataReader["ToDate"]),
                Heading = sqlDataReader[$"{language}Header"].ToString(),
                Text = sqlDataReader[language].ToString()
            };
        }

        public void DeleteNews(List<string> idsToDelete)
        {
            if (idsToDelete == null)
            {
                throw new ArgumentNullException(nameof(idsToDelete));
            }

            if (!idsToDelete.Any())
            {
                Log.Warning("No Ids were transmitted");
                return;
            }

            DeleteNewsRecursively(idsToDelete);
        }

        private void DeleteNewsRecursively(IReadOnlyCollection<string> idsToDelete)
        {
            const int maxNumberOfPredicates = 100;

            var loopCounter = 0;
            var predicates = new List<string>();

            using (var connection = CreateConnection())
            {
                connection.Open();

                while (idsToDelete.Any())
                {
                    var temp = idsToDelete.Skip(loopCounter * maxNumberOfPredicates).Take(maxNumberOfPredicates).ToList();

                    if (!temp.Any())
                    {
                        return;
                    }

                    using (var command = connection.CreateCommand())
                    {
                        foreach (var id in temp)
                        {
                            var parameterName = $"@id{id}";
                            predicates.Add($"(id = {parameterName})");
                            command.Parameters.Add(CreateIdParameter(command, parameterName, id));
                        }

                        command.CommandText = "delete from News where " + predicates.Aggregate((a, b) => $"{a} OR {b}");
                        command.ExecuteNonQuery();
                    }

                    predicates.Clear();
                    loopCounter += 1;
                }
            }
        }

        private IDbDataParameter CreateIdParameter(IDbCommand command, string parameterName, string value)
        {
            var parameter = CreateParameter(command, DbType.Int16);
            parameter.ParameterName = parameterName;
            parameter.Value = value;

            return parameter;
        }

        private IDbDataParameter CreateStringParameter(IDbCommand command, string parameterName, string value)
        {
            var parameter = CreateParameter(command, DbType.String);
            parameter.ParameterName = parameterName;
            parameter.Value = value;

            return parameter;
        }

        private IDbDataParameter CreateDateTimeParameter(IDbCommand command, string parameterName, DateTime value)
        {
            var parameter = CreateParameter(command, DbType.DateTime);
            parameter.ParameterName = parameterName;
            parameter.Value = value;

            return parameter;
        }

        private IDbDataParameter CreateParameter(IDbCommand command, DbType dbType)
        {
            var parameter = command.CreateParameter();
            parameter.DbType = dbType;

            return parameter;
        }

        public INews GetSingleNews(int id)
        {
            using (var connection = CreateConnection())
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    var parameterName = $"@id{id}";
                    command.CommandText = $"select * from News where id = {parameterName}";

                    var idParameter = command.CreateParameter();
                    idParameter.ParameterName = parameterName;
                    idParameter.DbType = DbType.Int16;
                    idParameter.Value = id;

                    command.Parameters.Add(idParameter);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            return CreateNewsFromReader(reader);
                        }
                    }
                }
            }

            var message = $"invalid parameter: {id}";
            Log.Error(message);
            throw new Exception(message);
        }

        private DateTime ConvertToDateTime(string dateTimeAsString)
        {
            var regexObj = new Regex(
                @"\A(?<day>[0][1-9]|[1-2][0-9]|[3][0-1])\.(?<month>[0][1-9]|[1][0-2])\.(?<year>[0-9][0-9][0-9][0-9]) (?<hour>[0][0-9]|[1][0-9]|[2][0-3]):(?<minute>[0][0-9]|[1-5][0-9])\z");
            var matchResult = regexObj.Match(dateTimeAsString);
            if (matchResult.Success)
            {
                var day = int.Parse(matchResult.Groups["day"].Value);
                var month = int.Parse(matchResult.Groups["month"].Value);
                var year = int.Parse(matchResult.Groups["year"].Value);
                var hour = int.Parse(matchResult.Groups["hour"].Value);
                var minute = int.Parse(matchResult.Groups["minute"].Value);

                try
                {
                    return new DateTime(year, month, day, hour, minute, 0);
                }
                catch (Exception)
                {
                    throw new ArgumentException();
                }
            }

            throw new ArgumentException();
        }

        public void InsertOrUpdateNews(INews news)
        {
            if (news == null)
            {
                throw new ArgumentNullException(nameof(news));
            }

            using (var connection = CreateConnection())
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    CreateParameters(command, news);

                    var emptyParameters = GetEmptyParameters(command);

                    if (emptyParameters.Any())
                    {
                        throw new Exception($"Parameter(s) {string.Join(", ", GetParameterNames(emptyParameters))}  must not be null or whitespaces");
                    }

                    var isInsert = IsInsert(news);

                    if (isInsert)
                    {
                        CreateInsertCommandText(command);
                    }
                    else
                    {
                        CreateUpdateCommandTextAndIdParameter(command, news);
                    }

                    var result = command.ExecuteNonQuery();

                    if (result == 0)
                    {
                        var message = isInsert
                            ? "Unable to insert row"
                            : $"Id '{news.Id}' not found, unable to update row";

                        Log.Error(message);
                        throw new Exception(message);
                    }
                }
            }
        }

        private void CreateInsertCommandText(IDbCommand command)
        {
            var parameterNames = GetParameterNames(command.Parameters.OfType<IDbDataParameter>().ToList());

            command.CommandText =
                $"insert into News({string.Join(",", parameterNames).Replace("@", string.Empty)}) values({string.Join(",", parameterNames)})";
        }

        private void CreateUpdateCommandTextAndIdParameter(IDbCommand command, INews news)
        {
            var parameterNames = GetParameterNames(command.Parameters.OfType<IDbDataParameter>().ToList());

            var idParameter = CreateIdParameter(command, $"@{nameof(news.Id)}", news.Id);
            command.Parameters.Add(idParameter);

            command.CommandText = $"update News set {CreateUpdateStatementParts(parameterNames)} where Id = {idParameter.ParameterName}";
        }

        private string CreateUpdateStatementParts(List<string> parameterNames)
        {
            return string.Join(", ", parameterNames.Select(p => $"{p.Substring(1)} = {p}").ToList());
        }

        private List<string> GetParameterNames(List<IDbDataParameter> parameters)
        {
            return parameters.Select(p => p.ParameterName).ToList();
        }

        private List<IDbDataParameter> GetEmptyParameters(IDbCommand command)
        {
            return command.Parameters
                .OfType<IDbDataParameter>()
                .Where(p => string.IsNullOrWhiteSpace(p.Value?.ToString()))
                .ToList();
        }

        private void CreateParameters(IDbCommand command, INews news)
        {
            command.Parameters.Add(CreateDateTimeParameter(command, $"@{nameof(news.FromDate)}", ConvertToDateTime(news.FromDate)));
            command.Parameters.Add(CreateDateTimeParameter(command, $"@{nameof(news.ToDate)}", ConvertToDateTime(news.ToDate)));
            command.Parameters.Add(CreateStringParameter(command, $"@{nameof(news.De)}", news.De));
            command.Parameters.Add(CreateStringParameter(command, $"@{nameof(news.En)}", news.En));
            command.Parameters.Add(CreateStringParameter(command, $"@{nameof(news.Fr)}", news.Fr));
            command.Parameters.Add(CreateStringParameter(command, $"@{nameof(news.It)}", news.It));
            command.Parameters.Add(CreateStringParameter(command, $"@{nameof(news.DeHeader)}", news.DeHeader));
            command.Parameters.Add(CreateStringParameter(command, $"@{nameof(news.EnHeader)}", news.EnHeader));
            command.Parameters.Add(CreateStringParameter(command, $"@{nameof(news.FrHeader)}", news.FrHeader));
            command.Parameters.Add(CreateStringParameter(command, $"@{nameof(news.ItHeader)}", news.ItHeader));
        }

        private bool IsInsert(INews news)
        {
            if (string.IsNullOrWhiteSpace(news.Id))
            {
                return true;
            }

            if (!int.TryParse(news.Id, out var id))
            {
                return true;
            }

            return id < 1;
        }

        private string CreateReadableTimestamp(DateTime timeStamp)
        {
            return $"{timeStamp:dd.MM.yyyy HH:mm}";
        }
    }
}