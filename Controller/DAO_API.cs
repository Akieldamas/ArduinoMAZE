using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ArduinoMAZE.Model;
using Newtonsoft.Json;

namespace ArduinoMAZE.Controller
{
    public class DAO_API
    {
        HttpClient client;
        public DAO_API()
        {
            client = new HttpClient();
            client.BaseAddress = new Uri("https://este.alwaysdata.net");
        }

        public async Task<List<string>> GetNomsModeles()
        {

            var response = await client.GetAsync("/getAllModelNames");
            response.EnsureSuccessStatusCode();

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                List<string> nomsModeles = JsonConvert.DeserializeObject<List<string>>(content);
                return nomsModeles;
            }
            else
            {
                return null;
            }
        }

        public async Task<AIModel> getModelByName(string modelName)
        {
            var response = await client.GetAsync($"/getModelByName/{modelName}");
            response.EnsureSuccessStatusCode();
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                AIModel model = JsonConvert.DeserializeObject<AIModel>(content);
                return model;
            }
            else
            {
                return null;
                
            }
        }

    }
}
