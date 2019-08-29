using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClassTranscribeDatabase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClassTranscribeServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        [HttpPost]
        [Authorize(Roles = Globals.ROLE_ADMIN)]
        public ActionResult Wake()
        {
            WakeDownloader.Wake();
            return Ok();
        }
    }
}