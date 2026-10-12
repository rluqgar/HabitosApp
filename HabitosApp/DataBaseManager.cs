using System.Data;
using Microsoft.Data.Sqlite;

namespace HabitosApp;

// Clase genérica para trabajar con SQLite. No sabe nada de hábitos:
// recibe el SQL y los parámetros desde fuera.
public class DataBaseManager
{
    private readonly string _cadenaConexion;

    public DataBaseManager(string rutaArchivo)
    {
        // Con ReadWriteCreate, si el archivo .db no existe SQLite lo crea solo
        _cadenaConexion = new SqliteConnectionStringBuilder
        {
            DataSource = rutaArchivo,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();
    }

    // Para CREATE, INSERT, UPDATE y DELETE. Devuelve las filas afectadas
    public int EjecutarNonQuery(string sql, Dictionary<string, object?>? parametros = null)
    {
        using var conexion = new SqliteConnection(_cadenaConexion);
        conexion.Open();
        using var cmd = CrearComando(conexion, sql, parametros);
        return cmd.ExecuteNonQuery();
    }

    // Para consultas que devuelven un solo valor (por ejemplo last_insert_rowid)
    public object? EjecutarEscalar(string sql, Dictionary<string, object?>? parametros = null)
    {
        using var conexion = new SqliteConnection(_cadenaConexion);
        conexion.Open();
        using var cmd = CrearComando(conexion, sql, parametros);
        return cmd.ExecuteScalar();
    }

    // Para los SELECT. Devuelve todas las filas en un DataTable
    public DataTable EjecutarConsulta(string sql, Dictionary<string, object?>? parametros = null)
    {
        var tabla = new DataTable();

        using var conexion = new SqliteConnection(_cadenaConexion);
        conexion.Open();
        using var cmd = CrearComando(conexion, sql, parametros);
        using var reader = cmd.ExecuteReader();
        tabla.Load(reader);

        return tabla;
    }

    // Crea el comando y le añade los parámetros (@fecha, @id...).
    // Así nunca se mete texto del usuario directamente en el SQL.
    private static SqliteCommand CrearComando(SqliteConnection conexion, string sql, Dictionary<string, object?>? parametros)
    {
        var cmd = new SqliteCommand(sql, conexion);

        if (parametros != null)
        {
            foreach (var p in parametros)
            {
                cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);
            }
        }

        return cmd;
    }
}
