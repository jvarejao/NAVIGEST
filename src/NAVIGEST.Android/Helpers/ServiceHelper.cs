using System;
using Microsoft.Maui;

namespace NAVIGEST.Android.Services
{
    public static class ServiceHelper
    {
        public static T? GetService<T>() where T : class
            => Current?.GetService(typeof(T)) as T;

        private static IServiceProvider? Current
            => Application.Current?.Handler?.MauiContext?.Services;
    }
}


