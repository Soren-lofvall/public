using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace IHC_Maui_App.Shared.Services;

public class BaseHttpClient(HttpClient httpClient, ILogger<BaseHttpClient> logger) : IBaseHttpClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<BaseHttpClient> logger = logger;

    public async Task<T> GetAsync<T>(string apiMethod, object? content = null)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"api/v1.0/IHCTerminals/{apiMethod}");
        try
        {
            if (content != null)
                requestMessage.Content = JsonContent.Create(content);

            var response = await _httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();

            logger.LogInformation($"Request {requestMessage.Method} to {requestMessage.RequestUri} with was successful");

            using var contentStream = await response.Content.ReadAsStreamAsync();
            var result = await JsonSerializer.DeserializeAsync<T>(contentStream);
            return result!;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, $"Request {requestMessage.Method} to {requestMessage.RequestUri} failed");
            throw;
        }
    }

    public async Task<T> PostAsync<T, R>(string apiMethod, R content)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"api/v1.0/IHCTerminals/{apiMethod}");
        try
        {
            if (content != null)
            {
                requestMessage.Content = JsonContent.Create(content);

                var response = await _httpClient.SendAsync(requestMessage);
                response.EnsureSuccessStatusCode();

                logger.LogInformation($"Request {requestMessage.Method} to {requestMessage.RequestUri} with was successful");

                using var contentStream = await response.Content.ReadAsStreamAsync();
                var result = await JsonSerializer.DeserializeAsync<T>(contentStream);
                return result!;
            }
            else
            {
                logger.LogError("Content cannot be null");
                throw new ArgumentNullException(nameof(content));
            }
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, $"Request {requestMessage.Method} to {requestMessage.RequestUri} failed");
            throw;
        }
    }
    public async Task<T> PutAsync<T, R>(string apiMethod, R content)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Put, $"api/v1.0/IHCTerminals/{apiMethod}");

        try
        {
            if (content != null)
            {
                requestMessage.Content = JsonContent.Create(content);

                var response = await _httpClient.SendAsync(requestMessage);
                response.EnsureSuccessStatusCode();

                logger.LogInformation($"Request {requestMessage.Method} to {requestMessage.RequestUri} with was successful");

                using var contentStream = await response.Content.ReadAsStreamAsync();
                var result = await JsonSerializer.DeserializeAsync<T>(contentStream);
                return result!;
            }
            else
            {
                logger.LogError("Content cannot be null");
                throw new ArgumentNullException(nameof(content));
            }
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, $"Request {requestMessage.Method} to {requestMessage.RequestUri} failed");
            throw;
        }
    }
}
