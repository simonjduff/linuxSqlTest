using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using lib.Exeptions;
using Microsoft.Data.SqlClient;
using IsolationLevel = System.Data.IsolationLevel;

namespace lib
{
    public class SqlTest
    {
        private readonly string _connectionString;
        private static readonly Random Random = new Random();
        private readonly CancellationToken _cancellationToken;
        public int Successes = 0;
        public int Failures = 0;
        public int Exceptions = 0;
        private readonly List<string> _ids = new List<string>();
        private static int Count = 0;
        private readonly bool UseDapper = bool.Parse(Environment.GetEnvironmentVariable("SQLTEST_DAPPER") ?? "False");
        private readonly SemaphoreSlim Semaphore = new SemaphoreSlim(10000, 10000);
        private const int CommandTimeout = 2;

        public SqlTest(string connectionString, CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _connectionString = connectionString;
            
            Console.WriteLine($"Dapper {UseDapper}");

            Console.WriteLine("Loading dataset");
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            var command = new SqlCommand("SELECT Id FROM Data", connection);
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                _ids.Add(reader.GetString(0));
            }

            Console.WriteLine("Loaded dataset");
        }
        
        public async Task Run(object input)
        {
            await Semaphore.WaitAsync(_cancellationToken);
            State state = (State) input;
            // Console.WriteLine($"\t\tThread {state.Id} starting");
            Interlocked.Increment(ref Count);
            try
            {
                await InternalRun();
            }
            // Cancellation has completed. Return.
            catch (OperationCanceledException e)
            {
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Interlocked.Increment(ref Exceptions);
            }
            finally
            {
                state.AutoResetEvent.Set();
                Semaphore.Release();
            }
            
            // Console.WriteLine($"\t\tThread {state.Id} ended");
        }

        private async Task InternalRun()
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(_cancellationToken);

            // Range of rows in the target database
            var idIndex = Random.Next(0, _ids.Count);
            var id = _ids[idIndex];

            string sql = "SELECT Id, * FROM Data WHERE Id = @Id";

            string idResult;
            if (UseDapper)
            {
                idResult = await Dapper(id, sql, connection);
            }
            else
            {
                idResult = await SqlCommand(id, sql, connection);
            }

            if (!id.Equals(idResult))
            {
                Console.WriteLine($"Asked for [{id}] got [{idResult}]");
                Interlocked.Increment(ref Failures);
            }

            Interlocked.Increment(ref Successes);
        }

        private async Task<string> Dapper(string id, string sql, SqlConnection connection)
        {
            var result = await connection.QueryAsync<string>(sql, 
                new {Id = id},
                commandTimeout:2);
            var idResult = result.SingleOrDefault();
            if (string.IsNullOrEmpty(idResult))
            {
                throw new RowNotFoundException();
            }
            return idResult;
        }

        private async Task<string> SqlCommand(string id, string sql, SqlConnection connection)
        {
            var parameter = new SqlParameter("Id", SqlDbType.VarChar) {Value = id};
            
            var command = new SqlCommand(sql, connection);
            command.Parameters.Add(parameter);
            command.Transaction = tx;
            command.CommandTimeout = CommandTimeout;

            string idResult = null;
            using (var reader = await command.ExecuteReaderAsync(_cancellationToken))
            {
                while (await reader.ReadAsync(_cancellationToken))
                {
                    idResult = reader.GetString(0);
                    break;
                }
            }

            if (idResult == null)
            {
                throw new RowNotFoundException();
            }
            
            return idResult;
        }
    }
}
