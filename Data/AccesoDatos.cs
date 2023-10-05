namespace TableroApuestas.Data
{
    using System;
    using System.Data;
    using System.Data.OleDb;
    using System.Reflection;
    using TableroApuestas.Models;


    public class AccesoDatos
    {
        private readonly string connectionString;

        public AccesoDatos(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public DataTable ObtenerTodosLosUsuarios()
        {
            DataTable dataTable = new DataTable();

            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM usuarios";
                using (OleDbDataAdapter adapter = new OleDbDataAdapter(query, connection))
                {
                    adapter.Fill(dataTable);
                }
            }

            return dataTable;
        }

        public DataTable ObtenerUsuarioPorNombreYPassword(string nombre, string password, string connectionString)
        {
            DataTable dataTable = new DataTable();

            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM usuarios WHERE nombre = @nombre AND password = @password";
                using (OleDbCommand command = new OleDbCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@nombre", nombre);
                    command.Parameters.AddWithValue("@password", password);
                    using (OleDbDataAdapter adapter = new OleDbDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }
                }
            }

            return dataTable;
        }

        private DataTable ConsultarDatos(string consulta)
        {
            DataTable dataTable = new DataTable();

            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                connection.Open();
                using (OleDbDataAdapter adapter = new OleDbDataAdapter(consulta, connection))
                {
                    adapter.Fill(dataTable);
                }
            }

            return dataTable;
        }

        public async Task<DataTable> ObtenerDatosDeporteCompletoPorNombreAsync(string nombreDeporte)
        {
            string consulta = "SELECT * FROM deportes WHERE nombre = @nombreDeporte";
            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                await connection.OpenAsync();
                using (OleDbCommand command = new OleDbCommand(consulta, connection))
                {
                    command.Parameters.AddWithValue("@nombreDeporte", nombreDeporte);
                    DataTable dataTable = new DataTable();
                    using (OleDbDataAdapter adapter = new OleDbDataAdapter(command))
                    {
                        await Task.Run(() => adapter.Fill(dataTable));
                    }
                    return dataTable;
                }
            }
        }

        public async Task<decimal> ObtenerMontoTotalDeporteAsync(string nombreDeporte)
        {
            string consulta = @"
            SELECT SUM(ml.monto)
            FROM ((mes_ligas AS ml
            INNER JOIN ligas AS l ON ml.id_liga = l.id_liga)
            INNER JOIN deportes_liga AS dl ON l.id_liga = dl.id_liga)
            INNER JOIN deportes AS d ON dl.id_deporte = d.id_deporte
            WHERE d.nombre = @nombreDeporte";

            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                await connection.OpenAsync();
                using (OleDbCommand command = new OleDbCommand(consulta, connection))
                {
                    command.Parameters.Add(new OleDbParameter("@nombreDeporte", OleDbType.VarChar)).Value = nombreDeporte;
                    object result = await command.ExecuteScalarAsync();
                    return (result != DBNull.Value) ? Convert.ToDecimal(result) : 0;
                }
            }
        }

        public async Task<DataTable> ObtenerDatosDeportesAsync()
        {
            string consulta = "SELECT id_deporte, nombre, objetivo FROM deportes";

            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                await connection.OpenAsync();

                using (OleDbCommand command = new OleDbCommand(consulta, connection))
                {
                    DataTable dataTable = new DataTable();

                    using (OleDbDataAdapter adapter = new OleDbDataAdapter(command))
                    {
                        await Task.Run(() => adapter.Fill(dataTable));
                    }

                    return dataTable;
                }
            }
        }

        public async Task ActualizarObjetivoDeporteAsync(int idDeporte, decimal nuevoObjetivo)
        {
            string consulta = "UPDATE deportes SET objetivo = @nuevoObjetivo WHERE id_deporte = @idDeporte";

            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                await connection.OpenAsync();

                using (OleDbCommand command = new OleDbCommand(consulta, connection))
                {
                    command.Parameters.AddWithValue("@nuevoObjetivo", nuevoObjetivo);
                    command.Parameters.AddWithValue("@idDeporte", idDeporte);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<List<Mes>> ObtenerDatosMesesFutbolAsync()
        {
            string consulta = @"
                SELECT m.id_mes, m.nombre, SUM(ml.monto) AS monto_mes
                FROM (((mes_ligas AS ml
                INNER JOIN ligas AS l ON ml.id_liga = l.id_liga)
                INNER JOIN deportes_liga AS dl ON l.id_liga = dl.id_liga)
                INNER JOIN deportes AS d ON dl.id_deporte = d.id_deporte)
                INNER JOIN meses AS m ON ml.id_mes = m.id_mes
                WHERE d.nombre = 'futbol'
                GROUP BY m.id_mes, m.nombre
                ORDER BY m.id_mes";

            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                await connection.OpenAsync();
                using (OleDbCommand command = new OleDbCommand(consulta, connection))
                {
                    DataTable dataTable = new DataTable();
                    using (OleDbDataAdapter adapter = new OleDbDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }
                    List<Mes> meses = new List<Mes>();
                    foreach (DataRow row in dataTable.Rows)
                    {
                        meses.Add(new Mes
                        {
                            Id = Convert.ToInt32(row["id_mes"]),
                            Nombre = row["nombre"].ToString(),
                            Monto = Convert.ToDecimal(row["monto_mes"])
                        });
                    }
                    return meses;
                }
            }
        }

        public async Task<List<Liga>> ObtenerDatosLigasFutbolPorMesAsync(int idMes)
        {
            string consulta = @"
                SELECT l.id_liga, l.nombre, SUM(ml.monto) AS monto_generado
                FROM ((mes_ligas AS ml
                INNER JOIN ligas AS l ON ml.id_liga = l.id_liga)
                INNER JOIN deportes_liga AS dl ON l.id_liga = dl.id_liga)
                INNER JOIN deportes AS d ON dl.id_deporte = d.id_deporte
                WHERE d.nombre = 'futbol' AND ml.id_mes = @idMes
                GROUP BY l.id_liga, l.nombre
                ORDER BY l.id_liga";

            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                await connection.OpenAsync();
                using (OleDbCommand command = new OleDbCommand(consulta, connection))
                {
                    command.Parameters.AddWithValue("@idMes", idMes);
                    DataTable dataTable = new DataTable();
                    using (OleDbDataAdapter adapter = new OleDbDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }
                    List<Liga> ligas = new List<Liga>();
                    foreach (DataRow row in dataTable.Rows)
                    {
                        ligas.Add(new Liga
                        {
                            Id = Convert.ToInt32(row["id_liga"]),
                            Nombre = row["nombre"].ToString(),
                            Monto = Convert.ToDecimal(row["monto_generado"])
                        });
                    }
                    return ligas;
                }
            }
        }

        public async Task<List<Mes>> ObtenerDatosMesesBasquetAsync()
        {
            string consulta = @"
        SELECT m.id_mes, m.nombre, SUM(ml.monto) AS monto_mes
        FROM (((mes_ligas AS ml
        INNER JOIN ligas AS l ON ml.id_liga = l.id_liga)
        INNER JOIN deportes_liga AS dl ON l.id_liga = dl.id_liga)
        INNER JOIN deportes AS d ON dl.id_deporte = d.id_deporte)
        INNER JOIN meses AS m ON ml.id_mes = m.id_mes
        WHERE d.nombre = 'basquet'
        GROUP BY m.id_mes, m.nombre
        ORDER BY m.id_mes";

            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                await connection.OpenAsync();
                using (OleDbCommand command = new OleDbCommand(consulta, connection))
                {
                    DataTable dataTable = new DataTable();
                    using (OleDbDataAdapter adapter = new OleDbDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }
                    List<Mes> meses = new List<Mes>();
                    foreach (DataRow row in dataTable.Rows)
                    {
                        meses.Add(new Mes
                        {
                            Id = Convert.ToInt32(row["id_mes"]),
                            Nombre = row["nombre"].ToString(),
                            Monto = Convert.ToDecimal(row["monto_mes"])
                        });
                    }
                    return meses;
                }
            }
        }

        public async Task<List<Liga>> ObtenerDatosLigasBasquetPorMesAsync(int idMes)
        {
            string consulta = @"
        SELECT l.id_liga, l.nombre, SUM(ml.monto) AS monto_generado
        FROM ((mes_ligas AS ml
        INNER JOIN ligas AS l ON ml.id_liga = l.id_liga)
        INNER JOIN deportes_liga AS dl ON l.id_liga = dl.id_liga)
        INNER JOIN deportes AS d ON dl.id_deporte = d.id_deporte
        WHERE d.nombre = 'basquet' AND ml.id_mes = @idMes
        GROUP BY l.id_liga, l.nombre
        ORDER BY l.id_liga";

            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                await connection.OpenAsync();
                using (OleDbCommand command = new OleDbCommand(consulta, connection))
                {
                    command.Parameters.AddWithValue("@idMes", idMes);
                    DataTable dataTable = new DataTable();
                    using (OleDbDataAdapter adapter = new OleDbDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }
                    List<Liga> ligas = new List<Liga>();
                    foreach (DataRow row in dataTable.Rows)
                    {
                        ligas.Add(new Liga
                        {
                            Id = Convert.ToInt32(row["id_liga"]),
                            Nombre = row["nombre"].ToString(),
                            Monto = Convert.ToDecimal(row["monto_generado"])
                        });
                    }
                    return ligas;
                }
            }
        }

        public async Task<List<Mes>> ObtenerDatosMesesTenisAsync()
        {
            string consulta = @"
            SELECT m.id_mes, m.nombre, SUM(ml.monto) AS monto_mes
            FROM (((mes_ligas AS ml
            INNER JOIN ligas AS l ON ml.id_liga = l.id_liga)
            INNER JOIN deportes_liga AS dl ON l.id_liga = dl.id_liga)
            INNER JOIN deportes AS d ON dl.id_deporte = d.id_deporte)
            INNER JOIN meses AS m ON ml.id_mes = m.id_mes
            WHERE d.nombre = 'tenis'
            GROUP BY m.id_mes, m.nombre
            ORDER BY m.id_mes";

            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                await connection.OpenAsync();
                using (OleDbCommand command = new OleDbCommand(consulta, connection))
                {
                    DataTable dataTable = new DataTable();
                    using (OleDbDataAdapter adapter = new OleDbDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }
                    List<Mes> meses = new List<Mes>();
                    foreach (DataRow row in dataTable.Rows)
                    {
                        meses.Add(new Mes
                        {
                            Id = Convert.ToInt32(row["id_mes"]),
                            Nombre = row["nombre"].ToString(),
                            Monto = Convert.ToDecimal(row["monto_mes"])
                        });
                    }
                    return meses;
                }
            }
        }

        public async Task<List<Liga>> ObtenerDatosLigasTenisPorMesAsync(int idMes)
        {
            string consulta = @"
            SELECT l.id_liga, l.nombre, SUM(ml.monto) AS monto_generado
            FROM ((mes_ligas AS ml
            INNER JOIN ligas AS l ON ml.id_liga = l.id_liga)
            INNER JOIN deportes_liga AS dl ON l.id_liga = dl.id_liga)
            INNER JOIN deportes AS d ON dl.id_deporte = d.id_deporte
            WHERE d.nombre = 'tenis' AND ml.id_mes = @idMes
            GROUP BY l.id_liga, l.nombre
            ORDER BY l.id_liga";

            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                await connection.OpenAsync();
                using (OleDbCommand command = new OleDbCommand(consulta, connection))
                {
                    command.Parameters.AddWithValue("@idMes", idMes);
                    DataTable dataTable = new DataTable();
                    using (OleDbDataAdapter adapter = new OleDbDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }
                    List<Liga> ligas = new List<Liga>();
                    foreach (DataRow row in dataTable.Rows)
                    {
                        ligas.Add(new Liga
                        {
                            Id = Convert.ToInt32(row["id_liga"]),
                            Nombre = row["nombre"].ToString(),
                            Monto = Convert.ToDecimal(row["monto_generado"])
                        });
                    }
                    return ligas;
                }
            }
        }

    }
}
