using CustomerApi.Models;
using CustomerAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace CustomerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        public readonly Learn_DBContext context;
        public readonly JWTSetting setting;
        private readonly IRefreshTokenGenerator tokenGenerator;

        public IConfiguration Configuration { get; }


        public UserController(IConfiguration configuration,Learn_DBContext learn_DB, IOptions<JWTSetting> options,IRefreshTokenGenerator _refreshToken)
        {
            context = learn_DB;
            setting = options.Value;
            tokenGenerator = _refreshToken;

            Configuration = configuration;

        }

        [NonAction]
        public TokenResponse Authentificate(string username, Claim[] claims)
        {
            TokenResponse tokenResponse = new TokenResponse();
            var tokenkey = Encoding.UTF8.GetBytes(Configuration["JWTSetting:securitykey"]);

            var tokenhandler = new JwtSecurityToken(
          claims: claims,
          expires: DateTime.Now.AddMinutes(15),
           signingCredentials: new SigningCredentials(new SymmetricSecurityKey(tokenkey), SecurityAlgorithms.HmacSha256)

          );
            tokenResponse.JWTToken = new JwtSecurityTokenHandler().WriteToken(tokenhandler);
            tokenResponse.RefreshToken = tokenGenerator.GenerateToken(username);
            return tokenResponse;
        }


        [Route("Authentificate")]
        [HttpPost]
        public IActionResult Authentificate([FromBody] UserCred User )
        {
            TokenResponse tokenResponse = new TokenResponse();

            var _user = context.TblUser.FirstOrDefault(o => o.Userid == User.userName && o.Password == User.Password);
            if(_user == null) return Unauthorized();

            var tokenhandler = new JwtSecurityTokenHandler();
            var tokenkey = Encoding.UTF8.GetBytes(Configuration["JWTSetting:securitykey"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(
                    new Claim[]
                    {
                        new Claim(ClaimTypes.Name, _user.Userid),
                        new Claim(ClaimTypes.Role, _user.Role)
                    }
                ),
                Expires = DateTime.Now.AddMinutes(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(tokenkey), SecurityAlgorithms.HmacSha256)
            };
            var token = tokenhandler.CreateToken(tokenDescriptor);
            string finaltoken = tokenhandler.WriteToken(token);

            tokenResponse.JWTToken = finaltoken;
            tokenResponse.RefreshToken = tokenGenerator.GenerateToken(User.userName);

            return Ok(tokenResponse);
        }

        [Route("Refresh")]
        [HttpPost]
        public IActionResult Refresh([FromBody] TokenResponse token)
        {
            var tokenhandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;
            var authkey = Configuration.GetValue<string>("JWTSetting:securitykey");

            var principal = tokenhandler.ValidateToken(token.JWTToken, new TokenValidationParameters {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authkey)),
                ValidateIssuer = false,
                ValidateAudience = false
            },out securityToken);

            var _token = securityToken as JwtSecurityToken;
            if(_token != null && !_token.Header.Alg.Equals(SecurityAlgorithms.HmacSha256))
            {
                return Unauthorized();
            }
            var username = principal.Identity.Name;
            var _reftable = context.TblRefreshtoken.FirstOrDefault(o => o.UserId == username && o.RefreshToken == token.RefreshToken);
            if( _reftable == null)
            {
                return Unauthorized();
            }

            TokenResponse _result = Authentificate(username,principal.Claims.ToArray());

            return Ok(_result);
        }


        }
}
