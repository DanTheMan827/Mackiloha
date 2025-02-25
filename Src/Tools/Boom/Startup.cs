﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Boom.Data;
using Boom.Extensions;

namespace Boom
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var con = Configuration.GetConnectionString("Milo");

            services.AddDbContext<MiloContext>(op =>
                op.UseSqlite(Configuration.GetConnectionString("Milo")));

            services.AddControllers().AddNewtonsoftJson(op =>
            {
                op.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            });
        }
 
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            //app.MapControllers();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
