
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Prueba_API_One_Drive
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // id cliente de la aplicación registrada en Azure
            string clientId = "888b93c7-cc63-4d8c-9282-67068df20bfb";
            // id inquilino 
            string tenantId = "3c907651-d8c6-4ca6-a8a4-6a242430e653";
            // enlace para iniciar sesión en una cuenta Outloock
            string instance = "https://login.microsoftonline.com/";

            // aplicación de cliente público: se utiliza para adquirir tokens en apliacciones de escritorio
            IPublicClientApplication app = PublicClientApplicationBuilder
                .Create(clientId)
                .WithAuthority($"{instance}{tenantId}")
                .Build();

            // obtiene la colección cuentas por su identificador disponibles en la caché de tokens, según el flujo de usuarios
            var accounts = await app.GetAccountsAsync();
            IAccount firstAccount = accounts.FirstOrDefault();

            // aquí se almacenan los resultados de una operación de adquisición de tokens
            AuthenticationResult result = null;

            // alcance proporcionado 
            string[] scopes = new string[] {
                "User.Read"
            };

            try
            {
                // almacena la cadena de texto en una contraseña segura
                var securePassword = new SecureString();

                Console.WriteLine("Ingrese correo: ");
                string username = Console.ReadLine();
                Console.WriteLine("Ingrese contraseña: ");
                string password = Console.ReadLine();

                foreach (char c in password)
                    securePassword.AppendChar(c);
                // aquiere el token por usuario y contraseña
                result = await app.AcquireTokenByUsernamePassword(scopes, username, securePassword)
                    .ExecuteAsync();

                Console.WriteLine(result.Account.Username);
            }
            catch (Exception)
            {
                Console.WriteLine("Error en la aquisición del token");
            }

            // permisos para OneDrive
            string[] permissions = new string[] {
                "Files.ReadWrite",
                "Files.ReadWrite.All",
                "Sites.ReadWrite.All"
            };

            // se crea un proveedor de autentificación
            DeviceCodeProvider authProvider = new DeviceCodeProvider(app, permissions);

            // se crea un cliente http  
            var httpClient = new HttpClient();
            HttpResponseMessage response;
            try
            {
                string graphAPIEndpoint = "https://graph.microsoft.com/v1.0";
                var request = new HttpRequestMessage(HttpMethod.Get, graphAPIEndpoint);
                // se configura el encabezado de autorización
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                response = await httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine(content);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al obtener el token de autorización");
            }

            // se crea el cliente graph con el proveedor de autentificacion
            GraphServiceClient graphClient = new GraphServiceClient(authProvider);

            string folio = "001";
            DateTime date = DateTime.Now;
            string path = $"Bluespend/{date.Year}/{date.Month}/{date.Day}/{folio}/archivo.txt";

            StringBuilder text = new StringBuilder();

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(@"Hola mundo"));

            await graphClient.Me.Drive.Root.ItemWithPath(path).Content
                .Request()
                .PutAsync<DriveItem>(stream);


        }
    }
}