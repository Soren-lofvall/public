using System;
using System.Collections.Generic;
using System.Text;

namespace IHC_Maui_App.Shared.Services;

public interface IBaseHttpClient
{
    Task<T> PostAsync<T, R>(string apiMethod, R content);
    Task<T> GetAsync<T>(string apiMethod, object? content = null);
    Task<T> PutAsync<T, R>(string apiMethod, R content);
}
