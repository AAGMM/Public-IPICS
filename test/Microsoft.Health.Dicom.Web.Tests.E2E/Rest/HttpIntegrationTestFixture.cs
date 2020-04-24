﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Web.Tests.E2E.Clients;
using Microsoft.Health.Dicom.Web.Tests.E2E.Common;
using Microsoft.IO;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class HttpIntegrationTestFixture<TStartup> : IDisposable
    {
        private readonly string _environmentUrl;
        private readonly HttpMessageHandler _messageHandler;
        private readonly Dictionary<string, string> _bearerTokens = new Dictionary<string, string>();

        public HttpIntegrationTestFixture()
            : this(Path.Combine("src"))
        {
        }

        protected HttpIntegrationTestFixture(string targetProjectParentDirectory)
        {
            string environmentUrl = Environment.GetEnvironmentVariable("TestEnvironmentUrl");

            if (string.IsNullOrWhiteSpace(environmentUrl))
            {
                environmentUrl = "http://localhost/";

                StartInMemoryServer(targetProjectParentDirectory);

                _messageHandler = Server.CreateHandler();
                IsUsingInProcTestServer = true;
            }
            else
            {
                if (environmentUrl.Last() != '/')
                {
                    environmentUrl = $"{environmentUrl}/";
                }

                _messageHandler = new HttpClientHandler();
            }

            _environmentUrl = environmentUrl;

            HttpClient = CreateHttpClient();

            SetupAuthenticationAsync(TestApplications.GlobalAdminServicePrincipal, null).GetAwaiter().GetResult();

            RecyclableMemoryStreamManager = new RecyclableMemoryStreamManager();

            Client = new DicomWebClient(HttpClient, RecyclableMemoryStreamManager);
        }

        public bool IsUsingInProcTestServer { get; }

        public HttpClient HttpClient { get; }

        protected TestServer Server { get; private set; }

        public RecyclableMemoryStreamManager RecyclableMemoryStreamManager { get; }

        public DicomWebClient Client { get; }

        public HttpClient CreateHttpClient()
            => new HttpClient(new SessionMessageHandler(_messageHandler)) { BaseAddress = new Uri(_environmentUrl) };

        public void Dispose()
        {
            HttpClient.Dispose();
            Server?.Dispose();
        }

        /// <summary>
        /// Gets the full path to the target project that we wish to test
        /// </summary>
        /// <param name="projectRelativePath">
        /// The parent directory of the target project.
        /// e.g. src, samples, test, or test/Websites
        /// </param>
        /// <param name="startupType">The startup type</param>
        /// <returns>The full path to the target project.</returns>
        private static string GetProjectPath(string projectRelativePath, Type startupType)
        {
            for (Type type = startupType; type != null; type = type.BaseType)
            {
                // Get name of the target project which we want to test
                var projectName = type.GetTypeInfo().Assembly.GetName().Name;

                // Get currently executing test project path
                var applicationBasePath = AppContext.BaseDirectory;

                // Find the path to the target project
                var directoryInfo = new DirectoryInfo(applicationBasePath);
                do
                {
                    directoryInfo = directoryInfo.Parent;

                    var projectDirectoryInfo = new DirectoryInfo(Path.Combine(directoryInfo.FullName, projectRelativePath));
                    if (projectDirectoryInfo.Exists)
                    {
                        var projectFileInfo = new FileInfo(Path.Combine(projectDirectoryInfo.FullName, projectName, $"{projectName}.csproj"));
                        if (projectFileInfo.Exists)
                        {
                            return Path.Combine(projectDirectoryInfo.FullName, projectName);
                        }
                    }
                }
                while (directoryInfo.Parent != null);
            }

            throw new Exception($"Project root could not be located for startup type {startupType.FullName}");
        }

        private void StartInMemoryServer(string targetProjectParentDirectory)
        {
            var contentRoot = GetProjectPath(targetProjectParentDirectory, typeof(TStartup));

            IWebHostBuilder builder = WebHost.CreateDefaultBuilder()
                .UseContentRoot(contentRoot)
                .UseStartup(typeof(TStartup))
                .ConfigureServices(serviceCollection =>
                {
                    // ensure that HttpClients
                    // use a message handler for the test server
                    serviceCollection
                        .AddHttpClient(Options.DefaultName)
                        .ConfigurePrimaryHttpMessageHandler(() => _messageHandler);
                });

            Server = new TestServer(builder);
        }

        private async Task SetupAuthenticationAsync(TestApplication clientApplication, TestUser user = null)
        {
            if (AuthenticationSettings.SecurityEnabled)
            {
                var tokenUrl = AuthenticationSettings.TokenUrl;

                var tokenKey = $"{clientApplication.ClientId}:{(user == null ? string.Empty : user.UserId)}";

                if (!_bearerTokens.TryGetValue(tokenKey, out string bearerToken))
                {
                    bearerToken = await GetBearerToken(clientApplication, user, tokenUrl);
                    _bearerTokens[tokenKey] = bearerToken;
                }

                HttpClient.SetBearerToken(bearerToken);
            }
        }

        private async Task<string> GetBearerToken(TestApplication clientApplication, TestUser user, string tokenUrl)
        {
            if (clientApplication.Equals(TestApplications.InvalidClient))
            {
                return null;
            }

            var formContent = new FormUrlEncodedContent(GetAppSecuritySettings(clientApplication));

            HttpResponseMessage tokenResponse = await HttpClient.PostAsync(tokenUrl, formContent);

            var tokenJson = JObject.Parse(await tokenResponse.Content.ReadAsStringAsync());

            var bearerToken = tokenJson["access_token"].Value<string>();

            return bearerToken;
        }

        private List<KeyValuePair<string, string>> GetAppSecuritySettings(TestApplication clientApplication)
        {
            return new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("client_id", clientApplication.ClientId),
                new KeyValuePair<string, string>("client_secret", clientApplication.ClientSecret),
                new KeyValuePair<string, string>("grant_type", clientApplication.GrantType),
                new KeyValuePair<string, string>("scope", AuthenticationSettings.Scope),
                new KeyValuePair<string, string>("resource", AuthenticationSettings.Resource),
            };
        }

        /// <summary>
        /// An <see cref="HttpMessageHandler"/> that maintains session consistency between requests.
        /// </summary>
        private class SessionMessageHandler : DelegatingHandler
        {
            private string _sessionToken;

            public SessionMessageHandler(HttpMessageHandler innerHandler)
                : base(innerHandler)
            {
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (!string.IsNullOrEmpty(_sessionToken))
                {
                    request.Headers.TryAddWithoutValidation("x-ms-session-token", _sessionToken);
                }

                request.Headers.TryAddWithoutValidation("x-ms-consistency-level", "Session");

                HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

                if (response.Headers.TryGetValues("x-ms-session-token", out IEnumerable<string> tokens))
                {
                    _sessionToken = tokens.SingleOrDefault();
                }

                return response;
            }
        }
    }
}
