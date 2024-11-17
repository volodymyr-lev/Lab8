using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace Lab8.Models
{
    public class DatabaseManager
    {
        private readonly string _connectionString = "Host=localhost;Username=postgres;Password=password;Database=PostOffice";

        public IDbConnection GetConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }
    }
}
