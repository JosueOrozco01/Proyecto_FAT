using System;
using System.IO;
using Newtonsoft.Json;

public class ArchivoFAT
{
    public string Nombre { get; set; }
    public string RutaArchivoInicial { get; set; }
    public bool PapeleraReciclaje { get; set; }
    public int TotalCaracteres { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaModificacion { get; set; }
    public DateTime FechaEliminacion { get; set; }

    public ArchivoFAT(string nombre, string rutaArchivoInicial, int totalCaracteres)
    {
        Nombre = nombre;
        RutaArchivoInicial = rutaArchivoInicial;
        PapeleraReciclaje = false;
        TotalCaracteres = totalCaracteres;
        FechaCreacion = DateTime.Now;
        FechaModificacion = DateTime.Now;
    }
}

public class SegmentoArchivo
{
    public string Datos { get; set; }
    public string SiguienteArchivo { get; set; }
    public bool EOF { get; set; }

    public SegmentoArchivo(string datos, string siguienteArchivo, bool eof)
    {
        Datos = datos;
        SiguienteArchivo = siguienteArchivo;
        EOF = eof;
    }
}

public class SistemaFAT
{
    private const int MaxCaracteresPorSegmento = 20;

    public void CrearArchivo(string nombre, string contenido)
    {
        string rutaArchivoInicial = $"{nombre}_segmento1.json";
        GuardarSegmentos(contenido, rutaArchivoInicial);

        ArchivoFAT nuevoArchivo = new ArchivoFAT(nombre, rutaArchivoInicial, contenido.Length);
        GuardarArchivoFAT(nuevoArchivo);
    }

    public void ListarArchivos()
    {
        Console.WriteLine("Archivos disponibles:");
        foreach (var archivoFAT in Directory.GetFiles(".", "*.fat"))
        {
            ArchivoFAT archivo = CargarArchivoFAT(archivoFAT);
            if (!archivo.PapeleraReciclaje)
            {
                Console.WriteLine($"Nombre: {archivo.Nombre} - {archivo.TotalCaracteres} caracteres - Creado el {archivo.FechaCreacion}");
            }
        }
    }

    public void AbrirArchivo(string nombreArchivo)
    {
        string rutaFAT = $"{nombreArchivo}.fat";
        if (File.Exists(rutaFAT))
        {
            ArchivoFAT archivo = CargarArchivoFAT(rutaFAT);
            if (!archivo.PapeleraReciclaje)
            {
                Console.WriteLine($"Nombre: {archivo.Nombre}\nTotal Caracteres: {archivo.TotalCaracteres}\nFecha de Creación: {archivo.FechaCreacion}\nContenido:");
                MostrarContenido(archivo.RutaArchivoInicial);
            }
            else
            {
                Console.WriteLine("El archivo está en la papelera de reciclaje.");
            }
        }
    }

    public void EliminarArchivo(string nombreArchivo)
{
    Console.Write($"¿Estás seguro de que quieres eliminar el archivo '{nombreArchivo}'? (s/n): ");
    string confirmacion = Console.ReadLine().ToLower();
    
    if (confirmacion == "s")
    {
        string rutaFAT = $"{nombreArchivo}.fat";
        if (File.Exists(rutaFAT))
        {
            ArchivoFAT archivo = CargarArchivoFAT(rutaFAT);
            archivo.PapeleraReciclaje = true;
            archivo.FechaEliminacion = DateTime.Now;
            GuardarArchivoFAT(archivo);
            Console.WriteLine("Archivo movido a la papelera de reciclaje.");
        }
    }
    else
    {
        Console.WriteLine("Operación de eliminación cancelada.");
    }
}


    public void RecuperarArchivo(string nombreArchivo)
    {
        string rutaFAT = $"{nombreArchivo}.fat";
        if (File.Exists(rutaFAT))
        {
            ArchivoFAT archivo = CargarArchivoFAT(rutaFAT);
            archivo.PapeleraReciclaje = false;
            GuardarArchivoFAT(archivo);
            Console.WriteLine("Archivo recuperado de la papelera de reciclaje.");
        }
    }

    public void ModificarArchivo(string nombreArchivo)
{
    string rutaFAT = $"{nombreArchivo}.fat";
    if (File.Exists(rutaFAT))
    {
        ArchivoFAT archivo = CargarArchivoFAT(rutaFAT);
        if (!archivo.PapeleraReciclaje)
        {
            Console.WriteLine($"Nombre: {archivo.Nombre}\nTotal Caracteres: {archivo.TotalCaracteres}\nContenido Actual:");
            MostrarContenido(archivo.RutaArchivoInicial);

            Console.WriteLine("Ingresa el nuevo contenido del archivo (Presiona 'Escape' para guardar y salir):");

            // Logica para capturar la tecla Escape
            ConsoleKeyInfo key;
            string nuevoContenido = "";
            do
            {
                key = Console.ReadKey();
                if (key.Key != ConsoleKey.Escape)
                {
                    nuevoContenido += key.KeyChar;
                }
            } while (key.Key != ConsoleKey.Escape);
            
            Console.WriteLine("\nGuardando cambios...");

            // Eliminar los archivos de segmentos antiguos
            EliminarSegmentos(archivo.RutaArchivoInicial);

            // Crear nuevos segmentos
            string nuevaRutaArchivoInicial = $"{archivo.Nombre}_segmento1.json";
            GuardarSegmentos(nuevoContenido, nuevaRutaArchivoInicial);

            // Actualizar la información FAT
            archivo.RutaArchivoInicial = nuevaRutaArchivoInicial;
            archivo.TotalCaracteres = nuevoContenido.Length;
            archivo.FechaModificacion = DateTime.Now;
            GuardarArchivoFAT(archivo);

            Console.WriteLine("El archivo ha sido modificado correctamente.");
        }
        else
        {
            Console.WriteLine("El archivo está en la papelera de reciclaje.");
        }
    }
}


    private void GuardarSegmentos(string contenido, string rutaArchivoInicial)
    {
        int index = 0;
        string rutaActual = rutaArchivoInicial;

        while (index < contenido.Length)
        {
            int caracteresRestantes = contenido.Length - index;
            string datos = contenido.Substring(index, Math.Min(MaxCaracteresPorSegmento, caracteresRestantes));
            index += MaxCaracteresPorSegmento;

            string siguienteArchivo = (index < contenido.Length) ? $"{rutaActual}_segmento{index / MaxCaracteresPorSegmento + 1}.json" : null;
            SegmentoArchivo segmento = new SegmentoArchivo(datos, siguienteArchivo, siguienteArchivo == null);

            File.WriteAllText(rutaActual, JsonConvert.SerializeObject(segmento, Formatting.Indented));

            rutaActual = siguienteArchivo;
        }
    }

    private void MostrarContenido(string rutaArchivo)
    {
        while (rutaArchivo != null)
        {
            string contenidoSegmento = File.ReadAllText(rutaArchivo);
            SegmentoArchivo segmento = JsonConvert.DeserializeObject<SegmentoArchivo>(contenidoSegmento);
            Console.Write(segmento.Datos);
            rutaArchivo = segmento.SiguienteArchivo;
        }
        Console.WriteLine();
    }

    private void EliminarSegmentos(string rutaArchivoInicial)
    {
        while (rutaArchivoInicial != null)
        {
            string contenidoSegmento = File.ReadAllText(rutaArchivoInicial);
            SegmentoArchivo segmento = JsonConvert.DeserializeObject<SegmentoArchivo>(contenidoSegmento);
            File.Delete(rutaArchivoInicial);
            rutaArchivoInicial = segmento.SiguienteArchivo;
        }
    }

    private void GuardarArchivoFAT(ArchivoFAT archivo)
    {
        string rutaFAT = $"{archivo.Nombre}.fat";
        File.WriteAllText(rutaFAT, JsonConvert.SerializeObject(archivo, Formatting.Indented));
    }

    private ArchivoFAT CargarArchivoFAT(string rutaFAT)
    {
        string contenidoFAT = File.ReadAllText(rutaFAT);
        return JsonConvert.DeserializeObject<ArchivoFAT>(contenidoFAT);
    }
}

class Program
{
    static void Main()
    {
        SistemaFAT sistema = new SistemaFAT();
        bool salir = false;

        while (!salir)
        {
            Console.WriteLine("======================================");
            Console.WriteLine("      Sistema de Archivos FAT         ");
            Console.WriteLine("======================================");
            Console.WriteLine("1. Crear archivo");
            Console.WriteLine("2. Listar archivos");
            Console.WriteLine("3. Abrir archivo");
            Console.WriteLine("4. Modificar archivo");
            Console.WriteLine("5. Eliminar archivo");
            Console.WriteLine("6. Recuperar archivo");
            Console.WriteLine("7. Salir");
            Console.WriteLine("======================================");
            Console.Write("Elige una opción: ");
            string opcion = Console.ReadLine();
            Console.WriteLine("======================================");

            switch (opcion)
            {
                case "1":
                    Console.Write("Ingresa el nombre del archivo: ");
                    string nombre = Console.ReadLine();
                    Console.WriteLine("======================================");
                    Console.Write("Ingresa el contenido del archivo: ");
                    string contenido = Console.ReadLine();
                    sistema.CrearArchivo(nombre, contenido);
                    Console.WriteLine("======================================");
                    break;

                case "2":
                    sistema.ListarArchivos();
                    break;

                case "3":
                    Console.Write("Ingresa el nombre del archivo a abrir: ");
                    string nombreArchivoAbrir = Console.ReadLine();
                    sistema.AbrirArchivo(nombreArchivoAbrir);
                    Console.WriteLine("======================================");
                    break;

                case "4":
                    Console.Write("Ingresa el nombre del archivo a modificar: ");
                    string nombreArchivoModificar = Console.ReadLine();
                    sistema.ModificarArchivo(nombreArchivoModificar);
                    Console.WriteLine("======================================");
                    break;

                case "5":
                    Console.Write("Ingresa el nombre del archivo a eliminar: ");
                    string nombreArchivoEliminar = Console.ReadLine();
                    sistema.EliminarArchivo(nombreArchivoEliminar);
                    Console.WriteLine("======================================");
                    break;

                case "6":
                    Console.Write("Ingresa el nombre del archivo a recuperar: ");
                    string nombreArchivoRecuperar = Console.ReadLine();
                    sistema.RecuperarArchivo(nombreArchivoRecuperar);
                    Console.WriteLine("======================================");
                    break;

                case "7":
                    salir = true;
                    break;

                default:
                    Console.WriteLine("Opción no válida. Inténtalo de nuevo.");
                    Console.WriteLine("======================================");
                    break;
            }
        }
    }
}
