using System.Text;
using System.Net;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using Microsoft.AspNetCore.HttpOverrides;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.Extensions;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Diagnostics;
using IdentityServer4.Events;
using System.Linq;

namespace cbapiserviceconsole
{
    class Program
    {
        static void Main(string[] args)
        {

            foreach(var argument in args)
            {
                if(argument == null)
                {
                    throw new ArgumentNullException();
                }
                string strarg = argument.Substring(0, argument.IndexOf("="));
                strarg = strarg.Remove('-');
                strarg = strarg.Remove('-');
                strarg = strarg.Remove('"');
                strarg = strarg.Remove('"');
                string strval = argument.Substring(argument.IndexOf("=")+1);
                if (!String.IsNullOrEmpty(strarg) && !String.IsNullOrEmpty(strval)){
                    switch (strarg)
                    {
                        case "ENABLE_TESTSALE":	
                            if (strval != "true"){	
                                Globals.ENABLE_TESTSALE = false;	
                            }	
                            break;
                        case "CERT_MODE":
                            Globals.CERT_MODE = strval;
                            break;
                        case "CERT_PEM":
                            Globals.CERT_PEM_FILE = strval;
                            break;
                        case "KEY_PEM":
                            Globals.KEY_PEM_FILE = strval;
                            break;
                        case "HTTP_PORT":
                            Globals.LOCAL_HTTP_PORT = strval;
                            break;
                        case "HTTPS_PORT":
                            Globals.LOCAL_HTTPS_PORT = strval;
                            break;
                        case "USER_AGENT":
                            Globals.LOCAL_USER_AGENT = strval;
                            break;
                        case "URL_LIST":
                            Globals.LOCAL_URL_LIST = strval;
                            break;
                        case "API_AUTHKEY":
                            Globals.CB_API.CLICKBANK_API_AUTHKEY = strval;
                            break;
                        case "VENDOR":
                            Globals.CB_API.CURRENT_VENDOR = strval;
                            break;
                        case "CANCEL_PRODUCT_TITLE":
                            Globals.CB_API.COMMON_PRODUCT_TITLE_STRING = strval;
                            break;
                        case "MONTHLY_PRODUCT_SKU":
                            Globals.CB_API.CLICKBANK_MONTHLY_PRODUCT_SKU = strval;
                            break;
                        case "CORS_LIST":
                            Globals.CORSList = strval.Split(' ');
                            break;
                        case "CORS":
                            if (strval != "true"){
                                Globals.CORS = false;
                            }
                            break;
                    }
                }
            }

            RunServer();	
        }	
        static void RunServer()	
        {	
            Console.WriteLine("ClickBank PassThrough API Monitor Console.");
            var builder = WebHost.CreateDefaultBuilder();

            switch (Globals.CERT_MODE){
                case "PROD":
                    X509Certificate2 cert = System.Security.Cryptography.X509Certificates.X509Certificate2.CreateFromPemFile(Globals.CERT_PEM_FILE, Globals.KEY_PEM_FILE);
                    builder.
                            ConfigureKestrel(options =>
                            {
                                options.ConfigureHttpsDefaults(https => https.ServerCertificate = cert);
                            }).
                            UseKestrel().
                            UseSetting("https_port", Globals.LOCAL_HTTPS_PORT);
                    break;
                case "DEV":
                    builder.
                        UseSetting("https_port", Globals.LOCAL_HTTPS_PORT).
                        UseSetting("http_port", Globals.LOCAL_HTTP_PORT).
                        UseUrls(Globals.LOCAL_URL_LIST);
                    break;
                case "NONE":
                    if (Globals.LOCAL_URL_LIST.Contains("https")){
                        Console.WriteLine("URL_LIST may not contain HTTPS URLs CERT_MODE=NONE");
                        throw new Exception("URL_LIST may not contain HTTPS URLs CERT_MODE=NONE");
                    }
                    Globals.SSL = false;
                    builder.
                        UseSetting("http_port", Globals.LOCAL_HTTP_PORT).
                        UseUrls(Globals.LOCAL_URL_LIST);
                    break;
            }

            if (Globals.CORS && Globals.CORSList.Any()){
                builder.
                    ConfigureServices(s =>
                    {
                        s.AddCors(options =>
                        {
                            options.AddPolicy(name: "_myAllowSpecificOrigins",
                                            builder =>
                                            {
                                                builder.WithOrigins(Globals.CORSList);
                                            });
                        });
                        //s.AddIdentityServer().AddTestConfig();
                        s.AddSingleton<ClickBaseService>();
                        // s.AddAuthorization(options =>
                        // {
                        //     options.FallbackPolicy = new AuthorizationPolicyBuilder().
                        //         AddAuthenticationSchemes("Bearer").
                        //         RequireAuthenticatedUser().
                        //         RequireClaim("scope", "read").
                        //         Build();
                        // })
                        // .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                        // .AddJwtBearer(o =>
                        // {
                        //     o.Authority = "http://localhost:5000/openid";
                        //     o.Audience = "embedded";
                        //     o.RequireHttpsMetadata = false;
                        // });
                    });
            }

            builder.
            UseUrls(Globals.LOCAL_URL_LIST).
            Configure(app =>
            {
                app.UseCors(options => options.SetIsOriginAllowed(x => _ = true).AllowAnyMethod().AllowAnyHeader().AllowCredentials());
                ForwardedHeadersOptions forwardingOptions = new() { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.All };
                app.UseForwardedHeaders(forwardingOptions);
                app.UseRouting();
                // app.Map("/openid", id =>
                // {
                //     // use embedded identity server to issue tokens
                //     id.UseIdentityServer();
                // });
                // app.UseAuthentication();
                // app.UseAuthorization();
                app.UseEndpoints(e =>
                {
                    var clickBaseService = e.ServiceProvider.GetRequiredService<ClickBaseService>();
                    e.MapGet("/cbapi/requestcancel",
                        async c =>
                        {
                            string strResp = "ACCEPT";
                            string userIpIn = c.Request.HttpContext.Connection.RemoteIpAddress.ToString();
                            if (String.IsNullOrEmpty(userIpIn))
                            {
                                userIpIn = c.Request.Headers["HTTP_X_FORWARDED_FOR"];
                                if (String.IsNullOrEmpty(userIpIn))
                                {
                                    userIpIn = c.Request.Headers["REMOTE_ADDR"];
                                }
                            }
                            var urlIn = c.Request.GetEncodedPathAndQuery();

                            var queryString = c.Request.QueryString.ToString();
                            if (String.IsNullOrEmpty(urlIn) || String.IsNullOrEmpty(queryString) || Regex.Matches(urlIn, "&").Count != 2 || Regex.Matches(urlIn, "\\?").Count != 1)
                            {
                                strResp = "FAIL";
                                c.Response.StatusCode = 409;
                            }
                            else
                            {
                                queryString = queryString.Substring(queryString.IndexOf("?email=") + 7);
                                var emailIn = queryString.Substring(0, queryString.IndexOf("&lastName="));
                                queryString = queryString.Substring(queryString.IndexOf("&lastName=") + 10);
                                var lastNameIn = queryString.Substring(0, queryString.IndexOf("&zip="));
                                queryString = queryString.Substring(queryString.IndexOf("&zip=") + 5);
                                var zipIn = queryString;

                                c.Response.StatusCode = 409;
                                if (!String.IsNullOrEmpty(emailIn) && !String.IsNullOrEmpty(zipIn) && !String.IsNullOrEmpty(lastNameIn) && !String.IsNullOrEmpty(userIpIn))
                                {
                                    APIUserInfo userInfo = new()
                                    {
                                        email = emailIn,
                                        lastName = lastNameIn,
                                        zip = zipIn,
                                        userIp = userIpIn,
                                    };

                                    strResp = await clickBaseService.RequestCancel(userInfo);

                                    switch (strResp)
                                    {
                                        case "OK":
                                            c.Response.StatusCode = 200;
                                            break;
                                        case "OK MORE":
                                            c.Response.StatusCode = 200;
                                            break;
                                        case "ALREADY CANCL":
                                            c.Response.StatusCode = 201;
                                            break;
                                        case "NOT FOUND":
                                            c.Response.StatusCode = 404;
                                            break;
                                        case "REJECT":
                                            c.Response.StatusCode = 403;
                                            break;
                                        default: //stuck on ACCEPT
                                            strResp = "FAIL";
                                            c.Response.StatusCode = 409;
                                            break;
                                    }
                                }
                                else
                                {
                                    strResp = "MISSING DATA";
                                    c.Response.StatusCode = 409;
                                }
                            }

                            c.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                            c.Response.Headers.TryAdd("Access-Control-Allow-Headers", "accept, authorization, Content-Type");
                            c.Response.Headers.TryAdd("Access-Control-Allow-Methods", "PUT, POST, GET, DELETE, PATCH, OPTIONS");
                            c.Response.Headers.Remove("X-Powered-By");

                            await c.Response.WriteAsync(strResp);

                        });
                    e.MapGet("/cbapi/requestchange19",
                        async c =>
                        {
                            string strResp = "ACCEPT";
                            string userIpIn = c.Request.HttpContext.Connection.RemoteIpAddress.ToString();
                            if (String.IsNullOrEmpty(userIpIn))
                            {
                                userIpIn = c.Request.Headers["HTTP_X_FORWARDED_FOR"];
                                if (String.IsNullOrEmpty(userIpIn))
                                {
                                    userIpIn = c.Request.Headers["REMOTE_ADDR"];
                                }
                            }
                            var urlIn = c.Request.GetEncodedPathAndQuery();
                            var queryString = c.Request.QueryString.ToString();
                            if (String.IsNullOrEmpty(urlIn) || String.IsNullOrEmpty(queryString) || Regex.Matches(urlIn, "&").Count != 2 || Regex.Matches(urlIn, "\\?").Count != 1)
                            {
                                strResp = "FAIL";
                                c.Response.StatusCode = 409;
                            }
                            else
                            {
                                queryString = queryString.Substring(queryString.IndexOf("?email=") + 7);
                                var emailIn = queryString.Substring(0, queryString.IndexOf("&lastName="));
                                queryString = queryString.Substring(queryString.IndexOf("&lastName=") + 10);
                                var lastNameIn = queryString.Substring(0, queryString.IndexOf("&zip="));
                                queryString = queryString.Substring(queryString.IndexOf("&zip=") + 5);
                                var zipIn = queryString;

                                c.Response.StatusCode = 409;
                                if (!String.IsNullOrEmpty(emailIn) && !String.IsNullOrEmpty(zipIn) && !String.IsNullOrEmpty(lastNameIn) && !String.IsNullOrEmpty(userIpIn))
                                {
                                    APIUserInfo userInfo = new()
                                    {
                                        email = emailIn,
                                        lastName = lastNameIn,
                                        zip = zipIn,
                                        userIp = userIpIn,
                                    };
                                    strResp = await clickBaseService.RequestChange19(userInfo);

                                    switch (strResp)
                                    {
                                        case "OK":
                                            c.Response.StatusCode = 200;
                                            break;
                                        case "OK MORE":
                                            c.Response.StatusCode = 200;
                                            break;
                                        case "ALREADY 19":
                                            c.Response.StatusCode = 201;
                                            break;
                                        case "NOT FOUND":
                                            c.Response.StatusCode = 404;
                                            break;
                                        case "REJECT":
                                            c.Response.StatusCode = 403;
                                            break;
                                        default: //stuck on ACCEPT
                                            strResp = "FAIL";
                                            c.Response.StatusCode = 409;
                                            break;
                                    }
                                }
                                else
                                {
                                    strResp = "MISSING DATA";
                                    c.Response.StatusCode = 409;
                                }
                            }

                            c.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                            c.Response.Headers.TryAdd("Access-Control-Allow-Headers", "accept, authorization, Content-Type");
                            c.Response.Headers.TryAdd("Access-Control-Allow-Methods", "PUT, POST, GET, DELETE, PATCH, OPTIONS");
                            c.Response.Headers.Remove("X-Powered-By");

                            await c.Response.WriteAsync(strResp);

                        });
                    // e.MapGet("/contacts",
                    //     async c => await c.Response.WriteAsJsonAsync(await contactService.GetAll()));
                    // e.MapGet("/contacts/{id:int}",
                    //     async c => await c.Response.WriteAsJsonAsync(await contactService.Get(int.Parse((string)c.Request.RouteValues["id"]))));
                    // e.MapDelete("/contacts/{id:int}",
                    //     async c =>
                    //     {
                    //         await contactService.Delete(int.Parse((string)c.Request.RouteValues["id"]));
                    //         c.Response.StatusCode = 204;
                    //     });
                });
            }).Build().Run();
            Console.WriteLine("API Initialized and URL Binding Successfully");
            Console.WriteLine("Port: " + Globals.LOCAL_HTTP_PORT + "    SSL Port: " + Globals.LOCAL_HTTPS_PORT);
            Console.WriteLine("URL List: " + Globals.LOCAL_URL_LIST);

        }

        private void UseProdCert(ref WebHostBuilder builder){

        }
        private void UseDevCert(ref WebHostBuilder builder){
            
        }
        private void UseNoCert(ref WebHostBuilder builder){
            
        }

        private const string HttpContext = "MS_HttpContext";
        private const string RemoteEndpointMessage = "System.ServiceModel.Channels.RemoteEndpointMessageProperty";
        public static string GetClientIpAddress(HttpRequestMessage request)
        {
            if (request.Properties.ContainsKey(HttpContext))
            {
                dynamic ctx = request.Properties[HttpContext];
                if (ctx != null)
                {
                    return ctx.Request.UserHostAddress;
                }
            }

            if (request.Properties.ContainsKey(RemoteEndpointMessage))
            {
                dynamic remoteEndpoint = request.Properties[RemoteEndpointMessage];
                if (remoteEndpoint != null)
                {
                    return remoteEndpoint.Address;
                }
            }

            return null;
        }

    }
}