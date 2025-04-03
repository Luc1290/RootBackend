using Microsoft.AspNetCore.Mvc;
using RootBackend.Explorer.Skills;

namespace RootBackend.Controllers
{
    [ApiController]
    [Route("api/test-skill")]
    public class TestSkillController : ControllerBase
    {
        private readonly NavigatorSkill _navigatorSkill;

        public TestSkillController(NavigatorSkill navigatorSkill)
        {
            _navigatorSkill = navigatorSkill;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SkillTestRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Message))
                return BadRequest("Message vide.");

            try
            {
                var response = await _navigatorSkill.HandleAsync(request.Message);
                return Ok(new { success = true, input = request.Message, response });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        public class SkillTestRequest
        {
            public string Message { get; set; } = string.Empty;
        }
    }
}
