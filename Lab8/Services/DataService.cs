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


        public DataTable GetPaginatedData(string nameFilter, string addressFilter, int page, int pageSize)
        {
            try
            {
                string query = @"
                    SELECT * FROM post_offices
                    WHERE (@NameFilter IS NULL OR post_office_name ILIKE @NameFilter)
                    AND (@AddressFilter IS NULL OR address ILIKE @AddressFilter)
                    LIMIT @PageSize OFFSET @Offset;";

                using (var cmd = new NpgsqlCommand(query, _connection, _activeTransaction))
                {
                    cmd.Parameters.AddWithValue("NameFilter", string.IsNullOrEmpty(nameFilter) ? (object)DBNull.Value : $"%{nameFilter}%");
                    cmd.Parameters.AddWithValue("AddressFilter", string.IsNullOrEmpty(addressFilter) ? (object)DBNull.Value : $"%{addressFilter}%");
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

        public void CallStoredProcedure(int shipmentId)
        {
            try
            {
                string query = "CALL update_shipment_status(@ShipmentId);";

                using (var cmd = new NpgsqlCommand(query, _connection, _activeTransaction))
                {
                    cmd.Parameters.AddWithValue("ShipmentId", shipmentId);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (PostgresException ex)
            {
                Console.WriteLine($"Procedure failed: {ex.Message}");
                throw;
            }
        }

        public void SimulateTransactionConflict(int parcelId)
        {
            try
            {
                BeginTransaction(IsolationLevel.ReadCommitted); 
                string query1 = "UPDATE parcels SET weight = weight + 1 WHERE parcel_id = @ParcelId;";

                using (var cmd = new NpgsqlCommand(query1, _connection, _activeTransaction))
                {
                    cmd.Parameters.AddWithValue("ParcelId", parcelId);
                    cmd.ExecuteNonQuery();
                }

                Task.Run(() => {
                    BeginTransaction(IsolationLevel.ReadCommitted);
                    string query2 = "UPDATE parcels SET weight = weight + 1 WHERE parcel_id = @ParcelId;";

                    using (var cmd = new NpgsqlCommand(query2, _connection, _activeTransaction))
                    {
                        cmd.Parameters.AddWithValue("ParcelId", parcelId);
                        cmd.ExecuteNonQuery();
                    }
                }).Wait();

                CommitTransaction();

            }
            catch (PostgresException ex)
            {
                Console.WriteLine($"Conflict detected: {ex.Message}");
                RollbackTransaction();
            }
        }


        public void ResolveConflictWithHigherIsolation(int parcelId)
        {
            BeginTransaction(IsolationLevel.Serializable);
            SimulateTransactionConflict(parcelId);
            CommitTransaction();
        }
    }
}
