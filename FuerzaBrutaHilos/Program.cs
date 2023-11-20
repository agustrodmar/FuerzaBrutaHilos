using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;


// Resharper Disable All
/**
 * Clase principal del programa
 */
class Program
{
    /**
     * La función principal del programa
     */
    static void Main()
    {
        // Creo objeto de la clase RompeClaves, indico 4 hilos.
        RompeClaves elRuso = new RompeClaves("./2151220-passwords.txt", "cQw49yt5ZvVnO+lWVnrVS9Tr7n0HXnFQUiujoIkhu6M=", 16);

        // Iniciar el proceso de cracking
        elRuso.Crack();
    }
}

class RompeClaves
{
    private string rutaArchivo;
    private string miContrasena;
    private int maxHilos;
    private bool contrasenaEncontrada = false;  // Añade esta línea

    /**
     * Clase que se encarga de romper las claves.
     * 
     * @property rutaArchivo La ruta al archivo que contiene las contraseñas.
     * @property miContrasena La contraseña que estamos buscando.
     * @property maxHilos El número máximo de hilos que se pueden usar.
     */
    public RompeClaves(string rutaArchivo, string miContrasena, int maxHilos)
    {
        this.rutaArchivo = rutaArchivo;
        this.miContrasena = miContrasena;
        this.maxHilos = maxHilos;
    }

    /**
     * Función que empieza a crackear la contraseña
     */
    public void Crack()
    {
        // Crear un SemaphoreSlim con un máximo de hilos
        SemaphoreSlim semaphore = new SemaphoreSlim(maxHilos);

        // Iniciar el cronómetro
        Stopwatch cronometroTotal = new Stopwatch();
        cronometroTotal.Start();

        try
        {
            using (StreamReader sr = new StreamReader(rutaArchivo))
            {
                string line;
                while ((line = sr.ReadLine()!) != null)
                {
                    // El semáforo espera hasta que haya un hilo disponible
                    semaphore.Wait();

                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        try
                        {
                            if (contrasenaEncontrada)  // Añade esta línea
                            {
                                semaphore.Release();
                                return;
                            }

                            // Inicio un cronómetro para que cuente el tiempo que tarda
                            Stopwatch cronometro = new Stopwatch();
                            cronometro.Start();

                            string hashedPassword = ComputeSha256Hash(line);
                            if (hashedPassword == miContrasena)
                            {
                                Console.WriteLine("Contraseña desencriptada: {0}", line);
                                contrasenaEncontrada = true;  // Añade esta línea
                            }

                            cronometro.Stop();
                            if (cronometro.ElapsedMilliseconds > 0)
                            {
                                Console.WriteLine("Tiempo transcurrido: {0}ms en línea \"{1}\"", cronometro.ElapsedMilliseconds, line);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Se produjo una excepción: {0}", e.Message);
                        }
                        finally
                        {
                            // Libero el hilo cuando se haya terminado
                            semaphore.Release();
                        }
                    });
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("El archivo no se puede leer:");
            Console.WriteLine(e.Message);
        }

        // Detengo el cronómetro, se imprime el tiempo total
        cronometroTotal.Stop();
        Console.WriteLine("Tiempo total transcurrido: {0}ms", cronometroTotal.ElapsedMilliseconds);
    }

    /**
     * Función que calcula el hash SHA256 de una cadena de texto.
     *
     * @param rawData La cadena de texto a la que se le va a calcular el hash.
     * @return El hash SHA256 de la cadena de texto.
     */
    private string ComputeSha256Hash(string rawData)
    {
        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            string hashed = Convert.ToBase64String(bytes);

            return hashed;
        }
    }
}
