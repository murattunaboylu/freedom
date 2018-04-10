using System;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Freedom.SimulatorServices.Controllers;

namespace Freedom.Broker
{
    class SimulationManager
    {
        public SimulationManager()
        {
            Client.BaseAddress = new Uri(ConfigurationManager.AppSettings["SimulationServicesUrl"]);
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private readonly HttpClient Client = new HttpClient();

        public async Task<SimulationResult> GetProductAsync(string path)
        {
            SimulationResult simulationResult = null;
            try
            {
                HttpResponseMessage response = await Client.GetAsync(path);
                if (response.IsSuccessStatusCode)
                {
                    simulationResult = await response.Content.ReadAsAsync<SimulationResult>();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return simulationResult;
        }
    }
}
