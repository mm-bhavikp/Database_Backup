using Database_Backup.Services;
using Microsoft.AspNetCore.Mvc;

namespace Database_Backup.Controllers
{
    [ApiController]
    [Route("/")]
    public class HomeController : Controller
    {
        private readonly ICopyBlobService _copyBlobService;
        private readonly IBackUpDatabaseService _backUpservice;
        private readonly string _sourceContainer;
        private readonly string _destinationContainer;

        public HomeController(ICopyBlobService copyBlobService, IBackUpDatabaseService backUpService)
        {
            _copyBlobService = copyBlobService;
            _backUpservice = backUpService;
        }

        [HttpGet]
        [Route("/CopyDatabase")]
        public async Task<IActionResult> CopyDatabase()
        {
            await _copyBlobService.CopyBlobInOtherBucket(_sourceContainer, _destinationContainer);
            return Ok("Done");
        }

        [HttpGet]
        [Route("/Backup")]
        public IActionResult GetBackup()
        {
            _backUpservice.BackupAllUserDatabases();
            return Ok("Text");
        }
    }
}
