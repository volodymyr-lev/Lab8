using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using Lab8.Models;
using System.Data;

namespace Lab8.Services
{
    public class DataService
    {
        private readonly DatabaseManager _dbManager = new DatabaseManager();
        private NpgsqlTransaction _activeTransaction;
        private NpgsqlConnection _connection;

        public void BeginTransaction(IsolationLevel isolationLevel)
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
            {
                _connection = (NpgsqlConnection)_dbManager.GetConnection();
                _connection.Open();
            }

            _activeTransaction = _connection.BeginTransaction(isolationLevel);
        }

        public void CommitTransaction()
        {
            try
            {
                _activeTransaction?.Commit();
                Console.WriteLine("Transaction committed successfully.");
            }
            catch (PostgresException ex)
            {
                Console.WriteLine($"Commit failed: {ex.Message}. Rolling back...");
                RollbackTransaction();
            }
            finally
            {
                _activeTransaction = null;
                _connection?.Close();
            }
        }

        public void RollbackTransaction()
        {
            try
            {
                _activeTransaction?.Rollback();
                Console.WriteLine("Transaction rolled back.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Rollback failed: {ex.Message}");
            }
            finally
            {
                _activeTransaction = null;
                _connection?.Close();
            }
        }


        public DataTable GetPaginatedData(int idFilter, int page, int pageSize)
        {
            try
            {
                string query = @"
                    SELECT * FROM post_offices
                    WHERE post_office_id > @IdFilter
                    LIMIT @PageSize OFFSET @Offset;";

                using (var cmd = new NpgsqlCommand(query, _connection, _activeTransaction))
                {
                    cmd.Parameters.AddWithValue("IdFilter", idFilter);
                    cmd.Parameters.AddWithValue("PageSize", pageSize);
                    cmd.Parameters.AddWithValue("Offset", (page - 1) * pageSize);

                    DataTable dt = new DataTable();
                    using (var reader = cmd.ExecuteReader())
                    {
                        dt.Load(reader);
                    }
                    return dt;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching data: {ex.Message}");
                throw;
            }
        }

        //insert staff
        public void InsertStaff(string staffName, string staffPosition, int postOfficeId, string phoneNumber, DateTime startDate)
        {
            try {
                string query = @"
                    INSERT INTO staff (staff_name, staff_position, post_office_id, phone_number, start_date) 
                    VALUES (@Name, @Position, @PostOfficeId, @PhoneNumber, @StartDate);";

                using (var cmd = new NpgsqlCommand(query, _connection, _activeTransaction))
                {
                    cmd.Parameters.AddWithValue("Name", staffName);
                    cmd.Parameters.AddWithValue("Position", staffPosition);
                    cmd.Parameters.AddWithValue("PostOfficeId", postOfficeId);
                    cmd.Parameters.AddWithValue("PhoneNumber", phoneNumber);
                    cmd.Parameters.AddWithValue("StartDate", startDate);
                    cmd.ExecuteNonQuery();
                }
            }
            catch(PostgresException ex)
            {
                Console.WriteLine($"Database error during insert: {ex.Message}");
                throw;
            }
        }

        public void UpdateParcelStatus(int parcelId, string newStatus)
        {
            try
            {
                string query = "UPDATE parcels SET status = @NewStatus WHERE parcel_id = @ParcelId;";

                using (var cmd = new NpgsqlCommand(query, _connection, _activeTransaction))
                {
                    cmd.Parameters.AddWithValue("NewStatus", newStatus);
                    cmd.Parameters.AddWithValue("ParcelId", parcelId);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (PostgresException ex)
            {
                Console.WriteLine($"Update failed: {ex.Message}");
                throw;
            }
        }

        public void CallStoredProcedure(int parcelId, double weight)
        {
            try
            {
                string query = "CALL update_parcel_weight(@ParcelId,@Weight);";

                using (var cmd = new NpgsqlCommand(query, _connection, _activeTransaction))
                {
                    cmd.Parameters.AddWithValue("ParcelId", parcelId);
                    cmd.Parameters.AddWithValue("Weight", weight);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (PostgresException ex)
            {
                Console.WriteLine($"Procedure failed: {ex.Message}");
                throw;
            }
        }

        //public void SimulateTransactionConflict(int parcelId)
        //{
        //    try
        //    {
        //        BeginTransaction(IsolationLevel.ReadCommitted);
        //        string query1 = "UPDATE parcels SET weight = weight + 1 WHERE parcel_id = @ParcelId;";

        //        using (var cmd = new NpgsqlCommand(query1, _connection, _activeTransaction))
        //        {
        //            cmd.Parameters.AddWithValue("ParcelId", parcelId);
        //            cmd.ExecuteNonQuery();
        //        }

        //        Task.Run(() =>
        //        {
        //            BeginTransaction(IsolationLevel.ReadCommitted);
        //            string query2 = "UPDATE parcels SET weight = weight + 1 WHERE parcel_id = @ParcelId;";

        //            using (var cmd = new NpgsqlCommand(query2, _connection, _activeTransaction))
        //            {
        //                cmd.Parameters.AddWithValue("ParcelId", parcelId);
        //                cmd.ExecuteNonQuery();
        //            }
        //        }).Wait();

        //        CommitTransaction();

        //    }
        //    catch (PostgresException ex)
        //    {
        //        Console.WriteLine($"Conflict detected: {ex.Message}");
        //        RollbackTransaction();
        //    }
        //}

        public void SimulateTransactionConflict(int parcelId)
        {
            using (var connection1 = (NpgsqlConnection)_dbManager.GetConnection())
            using (var connection2 = (NpgsqlConnection)_dbManager.GetConnection())
            {
                connection1.Open();
                connection2.Open();

                var task1 = Task.Run(async () =>
                {
                    using (var transaction1 = connection1.BeginTransaction(IsolationLevel.RepeatableRead))
                    {
                        try
                        {
                            string query1 = "SELECT weight FROM parcels WHERE parcel_id = @ParcelId FOR UPDATE";
                            using (var cmd1 = new NpgsqlCommand(query1, connection1, transaction1))
                            {
                                cmd1.Parameters.AddWithValue("ParcelId", parcelId);
                                var weight = (double)await cmd1.ExecuteScalarAsync();

                                await Task.Delay(2000);

                                string updateQuery1 = "UPDATE parcels SET weight = @NewWeight WHERE parcel_id = @ParcelId";
                                using (var updateCmd1 = new NpgsqlCommand(updateQuery1, connection1, transaction1))
                                {
                                    updateCmd1.Parameters.AddWithValue("ParcelId", parcelId);
                                    updateCmd1.Parameters.AddWithValue("NewWeight", weight + 1);
                                    await updateCmd1.ExecuteNonQueryAsync();
                                }
                            }

                            await Task.Delay(1000); 
                            transaction1.Commit();
                            Console.WriteLine("Transaction 1: Committed successfully");
                        }
                        catch (PostgresException ex)
                        {
                            Console.WriteLine($"Transaction 1 failed: {ex.Message}");
                            transaction1.Rollback();
                            throw; 
                        }
                    }
                });

                var task2 = Task.Run(async () =>
                {
                    await Task.Delay(1000);

                    using (var transaction2 = connection2.BeginTransaction(IsolationLevel.RepeatableRead))
                    {
                        try
                        {
                            string query2 = "SELECT weight FROM parcels WHERE parcel_id = @ParcelId FOR UPDATE";
                            using (var cmd2 = new NpgsqlCommand(query2, connection2, transaction2))
                            {
                                cmd2.Parameters.AddWithValue("ParcelId", parcelId);
                                var weight = (double)await cmd2.ExecuteScalarAsync();

                                string updateQuery2 = "UPDATE parcels SET weight = @NewWeight WHERE parcel_id = @ParcelId";
                                using (var updateCmd2 = new NpgsqlCommand(updateQuery2, connection2, transaction2))
                                {
                                    updateCmd2.Parameters.AddWithValue("ParcelId", parcelId);
                                    updateCmd2.Parameters.AddWithValue("NewWeight", weight + 2);
                                    await updateCmd2.ExecuteNonQueryAsync();
                                }
                            }

                            transaction2.Commit();
                            Console.WriteLine("Transaction 2: Committed successfully");
                        }
                        catch (PostgresException ex)
                        {
                            Console.WriteLine($"Transaction 2 failed: {ex.Message}");
                            transaction2.Rollback();
                            throw; 
                        }
                    }
                });

                try
                {
                    Task.WaitAll(task1, task2);
                }
                catch (AggregateException ae)
                {
                    throw new Exception("Transaction conflict detected!", ae.InnerException);
                }
            }
        }

        public async Task ResolveConflictWithHigherIsolation(int parcelId)
        {
            using (var connection1 = (NpgsqlConnection)_dbManager.GetConnection())
            using (var connection2 = (NpgsqlConnection)_dbManager.GetConnection())
            {
                await connection1.OpenAsync();
                await connection2.OpenAsync();

                var task1 = Task.Run(async () =>
                {
                    using (var transaction1 = connection1.BeginTransaction(IsolationLevel.ReadCommitted))
                    {
                        try
                        {
                            string checkQuery = "SELECT weight FROM parcels WHERE parcel_id = @ParcelId";
                            decimal currentWeight;
                            using (var cmd = new NpgsqlCommand(checkQuery, connection1, transaction1))
                            {
                                cmd.Parameters.AddWithValue("ParcelId", parcelId);
                                var result = await cmd.ExecuteScalarAsync();
                                Console.WriteLine($"Current weight before update 1: {result}");
                                currentWeight = result == DBNull.Value ? 0 : Convert.ToDecimal(result);
                            }

                            await Task.Delay(2000);

                            string updateQuery1 = @"
                        UPDATE parcels 
                        SET weight = weight + @Increment 
                        WHERE parcel_id = @ParcelId 
                        RETURNING weight";  

                            using (var updateCmd1 = new NpgsqlCommand(updateQuery1, connection1, transaction1))
                            {
                                updateCmd1.Parameters.AddWithValue("ParcelId", parcelId);
                                updateCmd1.Parameters.AddWithValue("Increment", 1.0m); 
                                var newWeight = await updateCmd1.ExecuteScalarAsync();
                                Console.WriteLine($"Transaction 1: Updated weight to {newWeight}");
                            }

                            await transaction1.CommitAsync();
                            Console.WriteLine("Transaction 1: Committed successfully");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Transaction 1 failed: {ex.Message}");
                            Console.WriteLine($"Stack trace: {ex.StackTrace}");
                            await transaction1.RollbackAsync();
                            throw;
                        }
                    }
                });

                var task2 = Task.Run(async () =>
                {
                    await Task.Delay(1000);

                    using (var transaction2 = connection2.BeginTransaction(IsolationLevel.ReadCommitted))
                    {
                        try
                        {
                            string checkQuery = "SELECT weight FROM parcels WHERE parcel_id = @ParcelId";
                            decimal currentWeight;
                            using (var cmd = new NpgsqlCommand(checkQuery, connection2, transaction2))
                            {
                                cmd.Parameters.AddWithValue("ParcelId", parcelId);
                                var result = await cmd.ExecuteScalarAsync();
                                Console.WriteLine($"Current weight before update 2: {result}");
                                currentWeight = result == DBNull.Value ? 0 : Convert.ToDecimal(result);
                            }

                            string updateQuery2 = @"
                        UPDATE parcels 
                        SET weight = weight + @Increment 
                        WHERE parcel_id = @ParcelId 
                        RETURNING weight";  

                            using (var updateCmd2 = new NpgsqlCommand(updateQuery2, connection2, transaction2))
                            {
                                updateCmd2.Parameters.AddWithValue("ParcelId", parcelId);
                                updateCmd2.Parameters.AddWithValue("Increment", 2.0m); 
                                var newWeight = await updateCmd2.ExecuteScalarAsync();
                                Console.WriteLine($"Transaction 2: Updated weight to {newWeight}");
                            }

                            await transaction2.CommitAsync();
                            Console.WriteLine("Transaction 2: Committed successfully");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Transaction 2 failed: {ex.Message}");
                            Console.WriteLine($"Stack trace: {ex.StackTrace}");
                            await transaction2.RollbackAsync();
                            throw;
                        }
                    }
                });

                try
                {
                    await Task.WhenAll(task1, task2);

                    using (var checkCmd = new NpgsqlCommand("SELECT weight FROM parcels WHERE parcel_id = @ParcelId", connection1))
                    {
                        checkCmd.Parameters.AddWithValue("ParcelId", parcelId);
                        var finalWeight = await checkCmd.ExecuteScalarAsync();
                        Console.WriteLine($"Final weight after both transactions: {finalWeight}");
                    }

                    Console.WriteLine("Both transactions completed successfully!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error occurred: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    throw;
                }
            }
        }
    }
}
