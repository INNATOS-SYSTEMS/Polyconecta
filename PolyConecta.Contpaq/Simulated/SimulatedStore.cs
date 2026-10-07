using System;
using Contpaq.Bridge.Core.Contract;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Contpaq.Bridge.Simulated
{
    /// <summary>
    /// Estado del simulador que sobrevive a un reinicio: folios por concepto, el siguiente id ERP y
    /// los almacenes dados de alta con ALTA_ALMACEN. Vive en el mismo SQLite que el outbox.
    /// </summary>
    public sealed class SimulatedStore(string connectionString)
    {
        private readonly object _lock = new();

        public void Inicializar()
        {
            using var conn = Abrir();
            conn.Execute(@"
                CREATE TABLE IF NOT EXISTS simulated_folio (concepto TEXT PRIMARY KEY, ultimo_folio INTEGER NOT NULL);
                CREATE TABLE IF NOT EXISTS simulated_id (id INTEGER PRIMARY KEY CHECK (id = 1), ultimo INTEGER NOT NULL);
                INSERT OR IGNORE INTO simulated_id (id, ultimo) VALUES (1, 50000);
                CREATE TABLE IF NOT EXISTS simulated_almacen (codigo TEXT PRIMARY KEY, nombre TEXT NOT NULL, id_erp INTEGER NOT NULL);");
        }

        /// <summary>Siguiente folio del concepto y su id ERP, como los asignaría CONTPAQi.</summary>
        public DocumentoErp NuevoDocumento(string rol, string concepto)
        {
            lock (_lock)
            {
                using var conn = Abrir();
                using var tx = conn.BeginTransaction();
                conn.Execute("INSERT OR IGNORE INTO simulated_folio (concepto, ultimo_folio) VALUES (@c, 0);", new { c = concepto }, tx);
                conn.Execute("UPDATE simulated_folio SET ultimo_folio = ultimo_folio + 1 WHERE concepto = @c;", new { c = concepto }, tx);
                var folio = conn.ExecuteScalar<long>("SELECT ultimo_folio FROM simulated_folio WHERE concepto = @c;", new { c = concepto }, tx);
                var id = SiguienteId(conn, tx);
                tx.Commit();
                return new DocumentoErp { Rol = rol, Concepto = concepto, Folio = folio.ToString(System.Globalization.CultureInfo.InvariantCulture), IdErp = id };
            }
        }

        public AlmacenContrato? AlmacenCreado(string codigo)
        {
            using var conn = Abrir();
            return conn.QuerySingleOrDefault<AlmacenContrato>(
                "SELECT codigo AS Codigo, nombre AS Nombre, id_erp AS IdErp FROM simulated_almacen WHERE codigo = @codigo;", new { codigo });
        }

        public System.Collections.Generic.IEnumerable<AlmacenContrato> AlmacenesCreados()
        {
            using var conn = Abrir();
            return conn.Query<AlmacenContrato>("SELECT codigo AS Codigo, nombre AS Nombre, id_erp AS IdErp FROM simulated_almacen ORDER BY codigo;");
        }

        public AlmacenContrato CrearAlmacen(string codigo, string nombre)
        {
            lock (_lock)
            {
                using var conn = Abrir();
                using var tx = conn.BeginTransaction();
                var id = SiguienteId(conn, tx);
                conn.Execute("INSERT INTO simulated_almacen (codigo, nombre, id_erp) VALUES (@codigo, @nombre, @id);", new { codigo, nombre, id }, tx);
                tx.Commit();
                return new AlmacenContrato { Codigo = codigo, Nombre = nombre, IdErp = id };
            }
        }

        private static long SiguienteId(SqliteConnection conn, SqliteTransaction tx)
        {
            conn.Execute("UPDATE simulated_id SET ultimo = ultimo + 1 WHERE id = 1;", transaction: tx);
            return conn.ExecuteScalar<long>("SELECT ultimo FROM simulated_id WHERE id = 1;", transaction: tx);
        }

        private SqliteConnection Abrir()
        {
            var conn = new SqliteConnection(connectionString);
            conn.Open();
            return conn;
        }
    }
}
