using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WebMvc;

namespace WebMvc.Controllers
{
    public class ServiceConnectionCheck
    {
        public string ServiceName { get; set; }
        public string ServiceUrl { get; set; }
        public string Result { get; set; }
        public string Reason { get; set; }
        public string Response { get; set; }
        public long TimeElapsed { get;set; }
    }

    public class ConnectionResponse 
    {
        public List<ServiceConnectionCheck> VerificationResults { get;set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class VerifyController : ControllerBase
    {   
        private IOptions<AppSettings> _settings;
        public VerifyController(IOptions<AppSettings> settings)
        {
            _settings = settings;
        }

        [HttpGet]
        public async Task<ActionResult<ConnectionResponse>> Get()
        {
            var response = new ConnectionResponse();
            response.VerificationResults = new List<ServiceConnectionCheck>();

            // Verify Catalog service connection
            var serviceCheck = new ServiceConnectionCheck();
            serviceCheck.ServiceName = "Catalog Service";
            serviceCheck.ServiceUrl = _settings.Value.CatalogUrl + "/swagger/index.html";
            serviceCheck.Result = "Connection failed!";
            await CallService(serviceCheck);

            response.VerificationResults.Add(serviceCheck);

            // Verify Database Check
            if (!string.IsNullOrEmpty(_settings.Value.ConnectionString))
            {
                serviceCheck = new ServiceConnectionCheck();
                serviceCheck.ServiceName = "Database Connection";
                serviceCheck.ServiceUrl = _settings.Value.ConnectionString;
                serviceCheck.Result = "Connection failed!";
                DatabaseCheck(serviceCheck);

                response.VerificationResults.Add(serviceCheck);
            }

            // External service
            serviceCheck = new ServiceConnectionCheck();
            serviceCheck.ServiceName = "External Service";
            serviceCheck.ServiceUrl = "http://worldclockapi.com/api/json/est/now";
            serviceCheck.Result = "Connection failed!";
            await CallService(serviceCheck, true);

            response.VerificationResults.Add(serviceCheck);

            return response;
        }

        public async Task CallService(ServiceConnectionCheck serviceCheck, bool includeResponse = false)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.Timeout = TimeSpan.FromSeconds(30);
                var stopWatch = new Stopwatch();
                try
                {
                    stopWatch.Start();
                    var res = await httpClient.GetAsync(serviceCheck.ServiceUrl);

                    string content = string.Empty;
                    if (res.StatusCode == HttpStatusCode.OK)
                    {
                        serviceCheck.Result = "Connection is OK";
                        if (includeResponse)
                        {
                            serviceCheck.Response = await res.Content.ReadAsStringAsync();
                        }
                    }
                    stopWatch.Stop();
                    serviceCheck.TimeElapsed = stopWatch.ElapsedMilliseconds;
                }
                catch (Exception ex)
                {
                    stopWatch.Stop();
                    serviceCheck.TimeElapsed = stopWatch.ElapsedMilliseconds;
                    serviceCheck.Reason = ex.Message;
                }
            }
        }

        public void DatabaseCheck(ServiceConnectionCheck serviceCheck)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            using (SqlConnection connection = new SqlConnection(serviceCheck.ServiceUrl))
            {
                try
                {
                    connection.Open();
                    serviceCheck.Result = "Connection is OK";

                    stopWatch.Stop();
                    serviceCheck.TimeElapsed = stopWatch.ElapsedMilliseconds;
                }
                catch (SqlException ex)
                {
                    stopWatch.Stop();
                    serviceCheck.TimeElapsed = stopWatch.ElapsedMilliseconds;
                    serviceCheck.Reason = ex.Message;
                }
            }
        }
    }
}
