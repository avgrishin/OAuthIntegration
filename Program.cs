using Microsoft.AspNetCore.Authentication;
using System.Net;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthentication("cookie")
  .AddCookie("cookie", o =>
  {
    o.LoginPath = "/login";
    var del = o.Events.OnRedirectToAccessDenied;
    o.Events.OnRedirectToAccessDenied = ctx =>
    {
      if (ctx.Request.Path.StartsWithSegments("/yt"))
      {
        return ctx.HttpContext.ChallengeAsync("youtube");
      }
      return del(ctx);
    };
  })
  .AddOAuth("youtube", o =>
  {
    o.ClientId = "668395821690-7g3hdloat52i93lc8av5joqtr3gp1la9.apps.googleusercontent.com";
    o.ClientSecret = "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";
    o.SaveTokens = false;
    o.AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    o.TokenEndpoint = "https://oauth2.googleapis.com/token";
    o.CallbackPath = "/signin-google";
    o.Scope.Clear();
    o.Scope.Add("https://www.googleapis.com/auth/youtube.readonly");
  });
builder.Services.AddAuthorization(b =>
{
  b.AddPolicy("youtube-enabled", pb =>
  {
    pb.AddAuthenticationSchemes("cookie").RequireClaim("yt-token", "Y").RequireAuthenticatedUser();
  });
});

builder.Services.AddSingleton<Database>();
builder.Services.AddHttpClient();
HttpClient.DefaultProxy.Credentials = CredentialCache.DefaultCredentials;
var app = builder.Build();

app.MapGet("/login", () => Results.SignIn(
  new ClaimsPrincipal(
    new ClaimsIdentity(
      new[] { new Claim("user_id", Guid.NewGuid().ToString()) },
      "cookie"
    )
  ),
  authenticationScheme: "cookie"
));
app.MapGet("/yt/info", (IHttpClientFactory clientFactory, Database db, HttpContext ctx) =>
{
  var user = ctx.User;
  var userId = user.FindFirstValue("user_id");
  var accessToken = db[userId];
  var client = clientFactory.CreateClient();
}).RequireAuthorization("youtube-enabled");

app.Run();

public class Database : Dictionary<string, object> { }
