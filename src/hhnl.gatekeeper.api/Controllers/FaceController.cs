using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using hhnl.gatekeeper.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace hhnl.gatekeeper.api.Controllers
{
    [Route("face")]
    public class FaceController : ControllerBase
    {
        private FaceRecognitionService _faceRecognitionService;

        public FaceController(FaceRecognitionService faceRecognitionService)
        {
            _faceRecognitionService = faceRecognitionService;
        }

        [HttpPost("recognize")]
        public async Task<IActionResult> Recognize(IFormFile file1, IFormFile file2)
        {
            var t = _faceRecognitionService.RecognizeFacesInImage(file1.OpenReadStream(), file2.OpenReadStream());

            return Ok(t);
        }
    }
}
