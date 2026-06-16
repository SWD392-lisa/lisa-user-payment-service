using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using Yarp.ReverseProxy.Forwarder;

namespace ProjectLucy.API.Tests
{
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"success\":true}", Encoding.UTF8, "application/json")
            };
            return response;
        }
    }

    public class TestForwarderHttpClientFactory : IForwarderHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;
        public TestForwarderHttpClientFactory(HttpMessageHandler handler) => _handler = handler;

        public HttpMessageInvoker CreateClient(ForwarderHttpClientContext context)
        {
            return new HttpMessageInvoker(_handler, disposeHandler: false);
        }
    }

    public class GatewayTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public GatewayTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task TestAuthChecking_WithoutJwt_ReturnsUnauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/agora/token?channelName=test");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task TestEnvConfigLoading_ShouldOverrideRealtimeAddress()
        {
            // Arrange
            const string expectedUrl = "http://my-realtime-server.com/";
            const string expectedAuthPolicy = "MyCustomPolicy";
            const string expectedClusterId = "my-cluster";
            const string expectedAgoraPath = "/custom/agora/{**catch-all}";
            const string expectedSocketIoPath = "/custom/socket.io/{**catch-all}";

            try
            {
                Environment.SetEnvironmentVariable("REALTIME_SERVICE_URL", expectedUrl);
                Environment.SetEnvironmentVariable("REALTIME_SERVICE_AUTH_POLICY", expectedAuthPolicy);
                Environment.SetEnvironmentVariable("REALTIME_SERVICE_CLUSTER_ID", expectedClusterId);
                Environment.SetEnvironmentVariable("REALTIME_SERVICE_AGORA_PATH", expectedAgoraPath);
                Environment.SetEnvironmentVariable("REALTIME_SERVICE_SOCKETIO_PATH", expectedSocketIoPath);

                // Thêm đăng ký Policy 'MyCustomPolicy' để YARP đi qua phần validate lúc khởi tạo
                using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        services.AddAuthorization(options =>
                        {
                            options.AddPolicy("MyCustomPolicy", policy => policy.RequireAssertion(_ => true));
                        });
                    });
                });

                // Act
                var config = factory.Services.GetRequiredService<IConfiguration>();

                // Assert
                Assert.Equal(expectedUrl, config["ReverseProxy:Clusters:realtime-cluster:Destinations:nest-realtime-instance:Address"]);
                Assert.Equal(expectedAuthPolicy, config["ReverseProxy:Routes:agora-route:AuthorizationPolicy"]);
                Assert.Equal(expectedClusterId, config["ReverseProxy:Routes:agora-route:ClusterId"]);
                Assert.Equal(expectedAgoraPath, config["ReverseProxy:Routes:agora-route:Match:Path"]);

                Assert.Equal(expectedAuthPolicy, config["ReverseProxy:Routes:socketio-route:AuthorizationPolicy"]);
                Assert.Equal(expectedClusterId, config["ReverseProxy:Routes:socketio-route:ClusterId"]);
                Assert.Equal(expectedSocketIoPath, config["ReverseProxy:Routes:socketio-route:Match:Path"]);
            }
            finally
            {
                // Dọn dẹp các biến môi trường để không gây ảnh hưởng đến các test case khác chạy song song
                Environment.SetEnvironmentVariable("REALTIME_SERVICE_URL", null);
                Environment.SetEnvironmentVariable("REALTIME_SERVICE_AUTH_POLICY", null);
                Environment.SetEnvironmentVariable("REALTIME_SERVICE_CLUSTER_ID", null);
                Environment.SetEnvironmentVariable("REALTIME_SERVICE_AGORA_PATH", null);
                Environment.SetEnvironmentVariable("REALTIME_SERVICE_SOCKETIO_PATH", null);
            }
        }

        [Fact]
        public async Task TestHeaderTransformation_WithValidJwt_ProxiesAndForwardsHeaders()
        {
            // Arrange
            var testUserId = "user-12345";
            var testUserName = "Nguyen Van A";
            var testUserEmail = "a.nguyen@example.com";
            var testUserRole = "mentor";

            var token = GenerateTestJwtToken(testUserId, testUserName, testUserEmail, testUserRole);
            var mockHandler = new MockHttpMessageHandler();

            using var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Đăng ký Mock HTTP Client Factory cho YARP để intercept request downstream
                    services.AddSingleton<IForwarderHttpClientFactory>(new TestForwarderHttpClientFactory(mockHandler));
                });
            });

            var client = factory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/agora/token?channelName=test");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(mockHandler.LastRequest);

            var headers = mockHandler.LastRequest.Headers;
            Assert.True(headers.Contains("X-User-Id"));
            Assert.True(headers.Contains("X-User-Name"));
            Assert.True(headers.Contains("X-User-Email"));
            Assert.True(headers.Contains("X-User-Role"));

            Assert.Equal(testUserId, headers.GetValues("X-User-Id").First());
            Assert.Equal(testUserName, headers.GetValues("X-User-Name").First());
            Assert.Equal(testUserEmail, headers.GetValues("X-User-Email").First());
            Assert.Equal(testUserRole, headers.GetValues("X-User-Role").First());
        }

        private string GenerateTestJwtToken(string userId, string userName, string email, string role)
        {
            var secretKey = "8vOvSRycvAEBW/EDujcErz8UmfN70KGZZGKqfSuX7goQ1db0sPBwU1ICfogYXuy8QdOKRih5DEYOvMq1PL4Acg==";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role)
            };

            var token = new JwtSecurityToken(
                issuer: "ProjectLucy.API",
                audience: "ProjectLucy.Client",
                claims: claims,
                expires: DateTime.Now.AddMinutes(15),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
