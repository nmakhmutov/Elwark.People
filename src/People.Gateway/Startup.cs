using System;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using People.Gateway.Infrastructure;
using People.Gateway.Infrastructure.Identity;

namespace People.Gateway
{
    public class Startup
    {
        public Startup(IConfiguration configuration) =>
            Configuration = configuration;

        private IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2Support", true);
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Remove("sub");

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.Authority = Configuration["Urls:Identity"];
                    options.Audience = "elwark.people.api";
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        NameClaimType = "name",
                        RoleClaimType = "role"
                    };
                });

            services.AddAuthorizationCore(options =>
                options.AddPolicy(Policy.RequireAccountId, Policy.RequireAccountIdPolicy())
            );

            services.AddHttpContextAccessor()
                .AddScoped<IIdentityService, IdentityService>();

            services.AddGrpcClient<Grpc.Gateway.Gateway.GatewayClient>(
                options => options.Address = new Uri(Configuration["Urls:People.Api"])
            );

            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}