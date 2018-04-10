using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using codeshop.Sockets;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;

namespace codeshop
{
	public class Startup
	{
		private readonly string _connectionString = "";
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
			_connectionString = configuration.GetSection("DB").GetValue<string>("mySQLConnectionString");
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
				.AddCookie(o =>
				{
					o.LoginPath = "/Account/Login/";
					o.Events.OnRedirectToLogin = (context) =>
					{
						context.Response.StatusCode = 401;
						return Task.CompletedTask;
					};
				});

			services.AddCors(o =>
			{
				o.AddPolicy("CORS_ENV_DEVELOPMENT", builder =>
				{
					builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader().AllowCredentials();
				});
			});
      services.AddWebSocketManager();
			services.AddMvc();
			services.AddTransient<IDbConnection>(x => CreateDBContext());

		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env, IServiceProvider serviceProvider)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
				app.UseCors("CORS_ENV_DEVELOPMENT");
			}
      app.UseWebSockets();
      app.MapWebSocketManager("/ws", serviceProvider.GetService<BroadcastMessageHandler>());
      app.MapWebSocketManager("/rooms", serviceProvider.GetService<RoomMessageHandler>());

			app.UseDefaultFiles();
			app.UseStaticFiles();
			app.UseMvc();
		}

		private IDbConnection CreateDBContext()
		{
			var connection = new MySqlConnection(_connectionString);
			connection.Open();
			return connection;
		}

	}
}
