using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Q.Lib.Utility
{

    public class QDI
    {
        public static IServiceCollection Services { get; set; }
        public static IServiceProvider ServiceProvider { get; set; }
    }

    public static class QDIExtensions
    {
        public static IServiceCollection AddQingDI(this IServiceCollection services)
        {
            QDI.Services = services;
            return services;
        }

        public static IApplicationBuilder UseQingDI(this IApplicationBuilder builder)
        {
            QDI.ServiceProvider = builder.ApplicationServices;
            return builder;
        }
    }
}
