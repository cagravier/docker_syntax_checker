using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DockerSyntaxChecker;

namespace docker_validator_netcore.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DockerValidatorController : ControllerBase
    {
        private readonly ILogger<DockerValidatorController> _logger;

        public DockerValidatorController(ILogger<DockerValidatorController> logger)
        {
            _logger = logger;
        }

        [HttpPost("validate")]
        public string Post(Payload payload)
        {
          bool result = Checker.Check(payload.DockerContent, out var message);
          return message;
        }
    }
}
