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
        public IConfiguration Configuration { get; }


        public UserController(IConfiguration configuration,Learn_DBContext learn_DB, IOptions<JWTSetting> options)
        {
            context = learn_DB;
            setting = options.Value;
            Configuration = configuration;

        }

        [Route("Authentificate")]
        [HttpPost]
        public IActionResult Authentificate([FromBody] UserCred User )
        {
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
                    }
                ),
                Expires = DateTime.Now.AddMinutes(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(tokenkey), SecurityAlgorithms.HmacSha256)
            };
            var token = tokenhandler.CreateToken(tokenDescriptor);
            string finaltoken = tokenhandler.WriteToken(token);
            return Ok(finaltoken);
        }
    }
}
