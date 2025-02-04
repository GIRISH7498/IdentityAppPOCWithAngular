using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAppPOCWithAngular.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlayController : ControllerBase
    {
        [HttpGet("get-players")]
        public ActionResult Players()
        {
            return Ok(new JsonResult ( new { message = "Only for authorized users."}));
        }
    }
}
