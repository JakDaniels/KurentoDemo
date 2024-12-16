using Kurento.NET;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using KurentoDemo.Hubs;
using Microsoft.Extensions.Hosting;

namespace KurentoDemo
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
       
            //there is your kms address
            services.AddSingleton(p => new KurentoClient("ws://127.0.0.1:8888/kurento"));
            services.AddSingleton<RoomSessionManager>();
            services.AddMvc(mvc => mvc.EnableEndpointRouting = false);
            services.AddSignalR(config =>
            {
                config.EnableDetailedErrors = true;
            }).AddJsonProtocol(options =>
            {
                // options.PayloadSerializerOptions.
                // PayloadSerializerSettings.DateFormatString = "yyyy/MM/dd HH:mm:ss";
            });
        }
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<RoomHub>("/roomHub");
                endpoints.MapControllerRoute(name: "Default", pattern: "{controller=Home}/{action=Index}/{id?}");
            });
            app.UseMvc(routes =>
            {
                routes.MapRoute(name: "Default", template: "{controller=Home}/{action=Index}/{id?}");
            });

        }
    }
}
