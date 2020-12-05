using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using lib.Exeptions;
using Microsoft.Data.SqlClient;

namespace lib
{
    public class SqlTest
    {
        private readonly string _connectionString;
        private static readonly Random Random = new Random();
        private CancellationToken _cancellationToken;
        private int Successes = 0;
        private int Failures = 0;
        private int Exceptions = 0;
        private readonly List<int> _ids = new List<int>();
        private int Count = 0;
        private bool UseDapper = bool.Parse(Environment.GetEnvironmentVariable("SQLTEST_DAPPER") ?? "False");

        public SqlTest(string connectionString, CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _connectionString = connectionString;
            
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            var command = new SqlCommand("SELECT Id FROM Players", connection);
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                _ids.Add(reader.GetInt32(0));
            }

            Console.WriteLine($"Dapper {UseDapper}");
        }
        
        public void Run(object semaphore)
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                Interlocked.Increment(ref Count);
                try
                {
                    AsyncWrapper.Wait(InternalRun(), _cancellationToken);
                }
                // Cancellation has completed. Return.
                catch (OperationCanceledException e)
                {
                }
                catch (IncorrectRowReturnedException e)
                {
                    Interlocked.Increment(ref Failures);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Interlocked.Increment(ref Exceptions);
                }

                if (Count % 10 == 0)
                {
                    Console.WriteLine($"Good {Successes} Bad {Failures} Error {Exceptions}");
                }
            }

            ((SemaphoreSlim) semaphore).Release();
        }

        private async Task InternalRun()
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(_cancellationToken);

            // Range of rows in the target database
            var idIndex = Random.Next(0, _ids.Count);
            var id = _ids[idIndex];

            string sql = "SELECT Id, * FROM Players WHERE Id = @Id";

            int idResult;
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
                throw new IncorrectRowReturnedException();
            }

            Interlocked.Increment(ref Successes);
        }

        private async Task<int> Dapper(int id, string sql, SqlConnection connection)
        {
            var result = await connection.QueryAsync<int?>(sql, new {Id = id});
            var idResult = result.SingleOrDefault();
            if (idResult == null)
            {
                throw new RowNotFoundException();
            }

            return idResult.Value;
        }

        private async Task<int> SqlCommand(int id, string sql, SqlConnection connection)
        {
            var parameter = new SqlParameter("Id", SqlDbType.Int) {Value = id};
            var command = new SqlCommand(sql, connection);
            command.Parameters.Add(parameter);

            var reader = await command.ExecuteReaderAsync(_cancellationToken);
            if (!await reader.ReadAsync(_cancellationToken))
            {
                throw new RowNotFoundException();
            }

            var idResult = reader.GetInt32(0);
            return idResult;
        }
    }
}
