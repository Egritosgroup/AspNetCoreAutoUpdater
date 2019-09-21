using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AspNetCoreAutoUpdater;
using AspNetCoreAutoUpdater.Web;
using SampleApplication1.Models;
using SampleApplication1.Services;

namespace SampleApplication1
{
    public class Startup
    {
        private readonly IHostingEnvironment _hostingEnvironment;

        public Startup(IConfiguration configuration, IHostingEnvironment hostingEnvironment)
        {
            Configuration = configuration;
            _hostingEnvironment = hostingEnvironment;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var generalSettings = new GeneralSettings();
            Configuration.GetSection("GeneralSettings").Bind(generalSettings);
            var entryAssembly = typeof(Startup).Assembly;
            generalSettings.CurrentVersion = entryAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            services.AddSingleton(generalSettings);

            _ConfigureServicesUpdater(services);

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });


            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        private void _ConfigureServicesUpdater(IServiceCollection services)
        {
            services.AddSingleton<IUpdaterCommunicationService, UpdaterCommunicationService>();

            services.UseHttpUpdater(opts => {
                //General Settings
                opts.AppName = "SampleApplication1";
                opts.AppEntryDll = "SampleApplication1.dll";
                opts.ContentRootPath = _hostingEnvironment.ContentRootPath;
                //opts.ArchiveName = "SampleApplication1.zip";              //it takes automatically from AppName+.zip
                //opts.SecondaryFiles = new List<UpdaterFileInfo>();

                //HTTP Settings
                opts.HttpDownloadUrl = "https://egritosgroup.gr/static/AutoUpdater/SampleApplication1/";
            });

            services.AddAspNetCoreAutoUpdaterUI();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
