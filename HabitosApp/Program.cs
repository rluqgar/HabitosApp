using System.Data;
using System.Globalization;
using HabitosApp;
using Microsoft.Data.Sqlite;

// Hábito que vamos a registrar (se mide por cantidad, no por tiempo)
const string NOMBRE_HABITO = "Pepsi bebidas";

// Las fechas se guardan en la base de datos como yyyy-MM-dd (así se ordenan bien)
// y se muestran y se piden al usuario como dd/MM/yyyy
const string FORMATO_BD = "yyyy-MM-dd";
const string FORMATO_USUARIO = "dd/MM/yyyy";

// Consultas SQL
const string SQL_CREAR_TABLA = @"
    CREATE TABLE IF NOT EXISTS Registros (
        Id       INTEGER PRIMARY KEY AUTOINCREMENT,
        Fecha    TEXT    NOT NULL,
        Cantidad INTEGER NOT NULL CHECK (Cantidad > 0)
    );";

const string SQL_INSERTAR = @"
    INSERT INTO Registros (Fecha, Cantidad)
    VALUES (@fecha, @cantidad);
    SELECT last_insert_rowid();";

const string SQL_LISTAR = "SELECT * FROM Registros ORDER BY Fecha DESC, Id DESC;";

const string SQL_POR_ID = "SELECT * FROM Registros WHERE Id = @id;";

const string SQL_ACTUALIZAR = @"
    UPDATE Registros
    SET Fecha = @fecha, Cantidad = @cantidad
    WHERE Id = @id;";

const string SQL_ELIMINAR = "DELETE FROM Registros WHERE Id = @id;";

// Programa principal

// La base de datos se crea junto al ejecutable
string ruta = Path.Combine(AppContext.BaseDirectory, "habitos.db");
var db = new DataBaseManager(ruta);

try
{
    db.EjecutarNonQuery(SQL_CREAR_TABLA);
}
catch (Exception ex)
{
    Console.WriteLine($"No se ha podido crear la base de datos: {ex.Message}");
    return;
}

bool salir = false;
while (!salir)
{
    MostrarMenu();
    string opcion = LeerTexto("Opción: ");

    // Todo el trabajo con la base de datos va dentro del try para que
    // un fallo no cierre la aplicación
    try
    {
        switch (opcion)
        {
            case "1":
                VerRegistros(db);
                break;
            case "2":
                AnadirRegistro(db);
                break;
            case "3":
                ModificarRegistro(db);
                break;
            case "4":
                EliminarRegistro(db);
                break;
            case "0":
                salir = true;
                break;
            default:
                Console.WriteLine("Opción no válida, elige un número del menú.");
                break;
        }
    }
    catch (SqliteException ex)
    {
        Console.WriteLine($"Error con la base de datos: {ex.Message}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ha ocurrido un error inesperado: {ex.Message}");
    }
}

Console.WriteLine("¡Hasta pronto!");

// Opciones del menú

static void VerRegistros(DataBaseManager db)
{
    MostrarTabla(db.EjecutarConsulta(SQL_LISTAR));
}

static void AnadirRegistro(DataBaseManager db)
{
    var datos = PedirDatos();
    var id = db.EjecutarEscalar(SQL_INSERTAR, datos);
    Console.WriteLine($"Registro añadido con Id {id}.");
}

static void ModificarRegistro(DataBaseManager db)
{
    int id = PedirEntero("Id del registro a modificar: ", 1);
    if (!ExisteRegistro(db, id))
    {
        Console.WriteLine("No existe ningún registro con ese Id.");
        return;
    }

    var datos = PedirDatos();
    datos["@id"] = id;
    int filas = db.EjecutarNonQuery(SQL_ACTUALIZAR, datos);
    Console.WriteLine(filas > 0 ? "Registro modificado." : "No se ha podido modificar.");
}

static void EliminarRegistro(DataBaseManager db)
{
    int id = PedirEntero("Id del registro a eliminar: ", 1);
    if (!ExisteRegistro(db, id))
    {
        Console.WriteLine("No existe ningún registro con ese Id.");
        return;
    }

    if (!PedirConfirmacion("¿Seguro que quieres eliminarlo? (s/n): "))
    {
        Console.WriteLine("Operación cancelada.");
        return;
    }

    int filas = db.EjecutarNonQuery(SQL_ELIMINAR, new Dictionary<string, object?> { ["@id"] = id });
    Console.WriteLine(filas > 0 ? "Registro eliminado." : "No se ha podido eliminar.");
}

// Funciones auxiliares

static void MostrarMenu()
{
    Console.WriteLine();
    Console.WriteLine($"--- REGISTRO DE HÁBITOS: {NOMBRE_HABITO} ---");
    Console.WriteLine("1. Ver registros");
    Console.WriteLine("2. Añadir registro");
    Console.WriteLine("3. Modificar registro");
    Console.WriteLine("4. Eliminar registro");
    Console.WriteLine("0. Salir");
}

// Lee una línea de la consola. Si ya no hay entrada (null) cerramos
// la aplicación con normalidad en vez de quedarnos en un bucle infinito
static string LeerTexto(string mensaje)
{
    Console.Write(mensaje);
    string? texto = Console.ReadLine();

    if (texto == null)
    {
        Console.WriteLine();
        Environment.Exit(0);
    }

    return texto.Trim();
}

// Pide un número entero y repite la pregunta hasta que sea válido
static int PedirEntero(string mensaje, int minimo)
{
    while (true)
    {
        string texto = LeerTexto(mensaje);

        if (!int.TryParse(texto, out int valor))
        {
            Console.WriteLine("Eso no es un número entero válido.");
        }
        else if (valor < minimo)
        {
            Console.WriteLine($"El número debe ser como mínimo {minimo}.");
        }
        else
        {
            return valor;
        }
    }
}

// Pide una fecha. Se puede escribir "hoy" para usar la fecha actual.
// Devuelve la fecha ya en el formato de la base de datos
static string PedirFecha(string mensaje)
{
    while (true)
    {
        string texto = LeerTexto(mensaje);

        if (texto.Equals("hoy", StringComparison.OrdinalIgnoreCase))
        {
            return DateTime.Today.ToString(FORMATO_BD);
        }

        string[] formatos = { FORMATO_USUARIO, "d/M/yyyy" };
        if (!DateTime.TryParseExact(texto, formatos, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fecha))
        {
            Console.WriteLine($"Fecha incorrecta. Usa el formato {FORMATO_USUARIO} (por ejemplo 25/03/2026) o escribe 'hoy'.");
        }
        else if (fecha > DateTime.Today)
        {
            Console.WriteLine("La fecha no puede ser posterior a hoy.");
        }
        else
        {
            return fecha.ToString(FORMATO_BD);
        }
    }
}

static bool PedirConfirmacion(string mensaje)
{
    while (true)
    {
        string texto = LeerTexto(mensaje).ToLower();

        if (texto == "s" || texto == "si" || texto == "sí") return true;
        if (texto == "n" || texto == "no") return false;

        Console.WriteLine("Responde 's' o 'n'.");
    }
}

// Pide los datos del registro y los devuelve como parámetros SQL.
// Se usa tanto al añadir como al modificar para no repetir código
static Dictionary<string, object?> PedirDatos()
{
    string fecha = PedirFecha($"Fecha ({FORMATO_USUARIO}) o 'hoy': ");
    int cantidad = PedirEntero($"{NOMBRE_HABITO} (cantidad): ", 1);

    return new Dictionary<string, object?>
    {
        ["@fecha"] = fecha,
        ["@cantidad"] = cantidad
    };
}

static bool ExisteRegistro(DataBaseManager db, int id)
{
    var resultado = db.EjecutarConsulta(SQL_POR_ID, new Dictionary<string, object?> { ["@id"] = id });
    return resultado.Rows.Count > 0;
}

// Convierte la fecha de la base de datos al formato que ve el usuario
static string FormatearFecha(object? valor)
{
    string texto = valor?.ToString() ?? "";

    if (DateTime.TryParseExact(texto, FORMATO_BD, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fecha))
    {
        return fecha.ToString(FORMATO_USUARIO);
    }

    // Si por lo que sea la fecha guardada no tiene el formato esperado, se enseña tal cual
    return texto;
}

static void MostrarTabla(DataTable tabla)
{
    if (tabla.Rows.Count == 0)
    {
        Console.WriteLine("Todavía no hay registros.");
        return;
    }

    foreach (DataRow fila in tabla.Rows)
    {
        Console.WriteLine($"[{fila["Id"]}] {FormatearFecha(fila["Fecha"])} - {fila["Cantidad"]}");
    }
}
