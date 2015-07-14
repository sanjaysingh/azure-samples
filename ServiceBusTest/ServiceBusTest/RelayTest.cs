using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ServiceModel;
using System.ServiceModel.Web;
using Microsoft.ServiceBus;
using System.Net.Http;

namespace ServiceBusTest
{
    [TestClass]
    public class RelayTest
    {
        [TestMethod]
        public void Host_Service_Response_From_Local_And_Relay_Should_Match()
        {
            WebServiceHost sh = new WebServiceHost(typeof(Service), new Uri("http://localhost:9999"));
            var localEndpoint = sh.AddServiceEndpoint(typeof(IService), new WebHttpBinding(), "service");
            var remoteEndpoint = sh.AddServiceEndpoint(typeof(IService), new WebHttpRelayBinding(EndToEndWebHttpSecurityMode.None, RelayClientAuthenticationType.None), ServiceBusEnvironment.CreateServiceUri("https", AppSetting.Namespace, "service"));
            remoteEndpoint.Behaviors.Add(new TransportClientEndpointBehavior
            {
                TokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider("RootManageSharedAccessKey", AppSetting.RootAccessKey)
            });

            try
            {
                sh.Open();

                Assert.AreEqual(GetHttpResponse("http://localhost:9999/service/message"), GetHttpResponse($"https://{AppSetting.Namespace}.servicebus.windows.net/service/message"), "Data returned from relay endpoint does not match one returned from local end point.");
            }
            finally
            {
                sh.Close();
            }
        }

        private string GetHttpResponse(string url)
        {
            string responseText = string.Empty;
            using (var client = new HttpClient())
            {
                var response = client.GetAsync(url).Result;
                responseText = response.Content.ReadAsStringAsync().Result;
            }

            return responseText;
        }
    }

    [ServiceContract]
    interface IService
    {
        [WebGet(ResponseFormat = WebMessageFormat.Json)]
        [OperationContract]
        string Message();
    }

    interface IServiceChannel : IService, IClientChannel { }

    class Service : IService
    {
        public string Message()
        {
            return "This is sample message from service.";
        }
    }
}
