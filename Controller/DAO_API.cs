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
        private readonly string loginUsername = "";
        private readonly string loginPassword = "";

        string authToken = "";

        public DAO_API()
        {
            client = new HttpClient();
            client.BaseAddress = new Uri("");
        }

        private async Task loginToAPI()
        {
            var body = new
            {
                username = loginUsername,
                password = loginPassword
            };

            string json = JsonConvert.SerializeObject(body);

            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                var response = await client.PostAsync("/login", content);
                response.EnsureSuccessStatusCode();
                if (response.IsSuccessStatusCode)
                {
                    authToken = await response.Content.ReadAsStringAsync();
                    tokenModel token = JsonConvert.DeserializeObject<tokenModel>(authToken);
                    authToken = token.token;
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
                }
            }
        }

        public async Task<List<string>> GetNomsModeles()
        {
            await loginToAPI();

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
            await loginToAPI();

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
