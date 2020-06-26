using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Clam.Areas.Music.Models;
using Clam.Filters;
using Clam.Repository;
using Clam.Utilities;
using Clam.Utilities.Security;
using ClamDataLibrary.DataAccess;
using ClamDataLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Clam.Areas.Music.Controllers
{
    [Authorize(Policy = "Level-Two")]
    [Authorize(Policy = "Contributor-Access")]
    [Area("Music")]
    [Route("music/manage")]
    public class MusicController : Controller
    {

        private readonly StreamMusicDataUpload _uploadFile;
        private readonly UserManager<ClamUserAccountRegister> _userManager;
        private readonly RoleManager<ClamRoles> _roleManager;
        private readonly ClamUserAccountContext _context;
        private readonly IMapper _mapper;

        // Stream Path Host
        private readonly string _targetFolderPath;
        private readonly string _targetFilePath;
        private readonly long _fileSizeLimit;
        private readonly ILogger<MusicController> _logger;
        private readonly string[] _permittedExtentions = { ".mp3" };

        // WebHosting Enviroment
        private readonly IWebHostEnvironment _environment;
        private readonly UnitOfWork _unitOfWork;
        // Get the default form options so that we can use them to set the default
        // limits for request body data.
        private static readonly FormOptions _defaultFormOptions = new FormOptions();

        public MusicController(UserManager<ClamUserAccountRegister> userManager, RoleManager<ClamRoles> roleManager,
            ClamUserAccountContext context, IMapper mapper, IConfiguration config, IWebHostEnvironment environment, 
            ILogger<MusicController> logger, StreamMusicDataUpload uploadFile, UnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _unitOfWork = unitOfWork;

            _uploadFile = uploadFile;
            _environment = environment;
            _targetFilePath = config.GetValue<string>("AbsoluteRootFilePathStore");
            _targetFolderPath = config.GetValue<string>("AbsoluteFilePath-Music");
            _fileSizeLimit = config.GetValue<long>("MusicTrackSizeLimit");
        }

        // GET: Music
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var getUserName = User.Identity.Name;
            var model = await _unitOfWork.MusicControl.GetAllUserMusic(getUserName);
            return View(model);
        }

        [Authorize(Policy = "Contributor-Create")]
        [HttpGet("upload")]
        public IActionResult PostUploadMusic()
        {
            List<bool> decisions = new List<bool>() { true, false };
            var dictionary = new Dictionary<string, bool>()
            {
                { "Public", true },
                { "Private", false }
            };
            List<SelectListItem> viewingStatus = new List<SelectListItem>();
            foreach (var item in dictionary)
            {
                viewingStatus.Add(new SelectListItem()
                {
                    Text = item.Key,
                    Value = item.Value.ToString()
                });
            }
            ViewBag.ViewStatus = viewingStatus;
            return View();
        }

        [Authorize(Policy = "Contributor-Update")]
        [HttpGet("edit/{id}")]
        public async Task<IActionResult> EditMusic(Guid id)
        {
            var model = await _unitOfWork.MusicControl.GetAsyncMusicTrack(id);
            StreamFormDataMusic result = new StreamFormDataMusic()
            {
                SongTitle = model.SongTitle,
                SongArtist = model.SongArtist,
                Status = model.Status.ToString()
            };

            List<bool> decisions = new List<bool>() { true, false };
            var dictionary = new Dictionary<string, bool>()
            {
                { "Public", true },
                { "Private", false }
            };
            List<SelectListItem> viewingStatus = new List<SelectListItem>();
            foreach (var item in dictionary)
            {
                viewingStatus.Add(new SelectListItem()
                {
                    Text = item.Key,
                    Value = item.Value.ToString()
                });
            }
            ViewBag.ViewStatus = viewingStatus;
            ViewBag.MusicId = id;
            return View(result);
        }

        [Authorize(Policy = "Contributor-Update")]
        [Authorize(Policy = "Permission-Update")]
        [HttpPost("edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMusic(Guid id, StreamFormDataMusic formData)
        {
            await _unitOfWork.MusicControl.UpdateMusicTrack(id, formData);
            _unitOfWork.Complete();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("details/{id}")]
        public async Task<IActionResult> MusicDetails(Guid id)
        {
            var model = await _unitOfWork.MusicControl.GetAsyncMusicTrack(id);
            ViewBag.SongId = model.SongId;
            return View(model);
        }

        [Authorize(Policy = "Contributor-Remove")]
        [Authorize(Policy = "Permission-Remove")]
        [HttpPost("delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveTrack(Guid id)
        {
            await _unitOfWork.MusicControl.RemoveMusicTrack(id);
            _unitOfWork.Complete();
            return RedirectToAction(nameof(Index));
        }

        // Category Section
        [HttpGet("genre")]
        public async Task<IActionResult> Category()
        {
            var model = await _unitOfWork.MusicControl.GetAllMusicGenres();
            return View(model);
        }

        [Authorize(Policy = "Contributor-Create")]
        [HttpGet("genre/create")]
        public IActionResult CreateGenre()
        {
            return View();
        }

        [Authorize(Policy = "Contributor-Create")]
        [Authorize(Policy = "Permission-Create")]
        [HttpPost("genre/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGenre(AreaUserMusicCategory model)
        {
            try
            {
                await _unitOfWork.MusicControl.AddAsyncGenre(model);
                _unitOfWork.Complete();
                return RedirectToAction(nameof(Category));
            }
            catch (Exception)
            {
                return View();
            }
        }

        [Authorize(Policy = "Contributor-Remove")]
        [Authorize(Policy = "Permission-Remove")]
        [HttpPost("genre/delete")]
        public async Task<IActionResult> RemoveGenre(Guid id)
        {
            await _unitOfWork.MusicControl.RemoveGenre(id);
            _unitOfWork.Complete();
            return RedirectToAction(nameof(Category));
        }

        [Authorize(Policy = "Contributor-Create")]
        [HttpGet("genre/select/{id}")]
        public async Task<ActionResult> GenreSelect(Guid id)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return View("AccessDenied");
            }
            var genre = await _unitOfWork.MusicControl.GetAsyncGenre(id);
            var model = await _unitOfWork.MusicControl.GetAllMusicTracksForGenreSelection(id, User.Identity.Name);
            ViewBag.GenreName = genre.CategoryName;
            ViewBag.GenreId = genre.CategoryId;
            return View(model);
        }

        [Authorize(Policy = "Contributor-Create")]
        [Authorize(Policy = "Permission-Create")]
        [HttpPost("genre/select/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenreSelect(Guid id, List<MusicGenreSelection> model)
        {
            if (!ModelState.IsValid)
            {
                return View("Error");
            }
            await _unitOfWork.MusicControl.UpdateAllMusicTrackGenreSelection(id, model);
            _unitOfWork.Complete();
            return RedirectToAction(nameof(Category));
        }

        [HttpGet("genre/details/{id}")]
        public async Task<IActionResult> GenreDetails(Guid id)
        {
            var category = await _unitOfWork.MusicControl.GetAsyncGenre(id);
            var model = await _unitOfWork.MusicControl.GetAsyncGenreWithMusicTracks(id);
            ViewBag.CategoryName = category.CategoryName;
            ViewBag.CategoryId = category.CategoryId;
            return View(model.OrderBy(x => x.SongTitle));
        }

        // ###################################################################################################
        // ###################################################################################################
        // ###################################################################################################

        #region UploadMusicToDatabase
        [Authorize(Policy = "Contributor-Create")]
        [Authorize(Policy = "Permission-Create")]
        [HttpPost("stream")]
        [DisableFormValueModelBinding]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(4294967295)]
        public async Task<IActionResult> UploadDatabase()
        {
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                ModelState.AddModelError("File",
                    $"The request couldn't be processed (Error 1).");
                // Log error

                return BadRequest(ModelState);
            }

            // User Profile
            var name = User.Identity.Name;
            var profile = await _userManager.FindByNameAsync(name);

            // Accumulate the form data key-value pairs in the request (formAccumulator).
            var formAccumulator = new KeyValueAccumulator();
            var trustedFileNameForDisplay = string.Empty;
            var untrustedFileNameForStorage = string.Empty;
            var trustedFilePathStorage = string.Empty;
            var trustedFileNameForFileStorage = string.Empty;
            var streamedFileImageContent = new byte[0];
            var streamedFilePhysicalContent = new byte[0];


            // List Byte for file storage
            List<byte[]> filesByteStorage = new List<byte[]>();
            List<string> filesNameStorage = new List<string>();
            List<string> storedPaths = new List<string>();
            List<string> storedPathDictionaryKeys = new List<string>();
            var fileStoredData = new Dictionary<string, byte[]>();

            var boundary = MultipartRequestHelper.GetBoundary(
                MediaTypeHeaderValue.Parse(Request.ContentType),
                _defaultFormOptions.MultipartBoundaryLengthLimit);
            var reader = new MultipartReader(boundary, HttpContext.Request.Body);

            var section = await reader.ReadNextSectionAsync();
            while (section != null)
            {
                var hasContentDispositionHeader =
                    ContentDispositionHeaderValue.TryParse(
                        section.ContentDisposition, out var contentDisposition);

                if (hasContentDispositionHeader)
                {
                    if (MultipartRequestHelper
                        .HasFileContentDisposition(contentDisposition))
                    {
                        untrustedFileNameForStorage = contentDisposition.FileName.Value;
                        // Don't trust the file name sent by the client. To display
                        // the file name, HTML-encode the value.
                        trustedFileNameForDisplay = WebUtility.HtmlEncode(
                                contentDisposition.FileName.Value);

                        if (!Directory.Exists(_targetFilePath))
                        {
                            string path = String.Format("{0}", _targetFilePath);
                            Directory.CreateDirectory(path);
                        }

                        //streamedFileContent =
                        //    await FileHelpers.ProcessStreamedFile(section, contentDisposition,
                        //        ModelState, _permittedExtentions, _fileSizeLimit);

                        streamedFilePhysicalContent = await FileHelpers.ProcessStreamedFile(
                            section, contentDisposition, ModelState,
                            _permittedExtentions, _fileSizeLimit);

                        filesNameStorage.Add(trustedFileNameForDisplay);
                        filesByteStorage.Add(streamedFilePhysicalContent);
                        fileStoredData.Add(trustedFileNameForDisplay, streamedFilePhysicalContent);

                        if (!ModelState.IsValid)
                        {
                            return BadRequest(ModelState);
                        }
                    }
                    else if (MultipartRequestHelper
                        .HasFormDataContentDisposition(contentDisposition))
                    {
                        // Don't limit the key name length because the 
                        // multipart headers length limit is already in effect.
                        var key = HeaderUtilities
                            .RemoveQuotes(contentDisposition.Name).Value;
                        var encoding = GetEncoding(section);

                        if (encoding == null)
                        {
                            ModelState.AddModelError("File",
                                $"The request couldn't be processed (Error 2).");
                            // Log error

                            return BadRequest(ModelState);
                        }

                        using (var streamReader = new StreamReader(
                            section.Body,
                            encoding,
                            detectEncodingFromByteOrderMarks: true,
                            bufferSize: 1024,
                            leaveOpen: true))
                        {
                            // The value length limit is enforced by 
                            // MultipartBodyLengthLimit
                            var value = await streamReader.ReadToEndAsync();

                            if (string.Equals(value, "undefined",
                                StringComparison.OrdinalIgnoreCase))
                            {
                                value = string.Empty;
                            }

                            formAccumulator.Append(key, value);

                            if (formAccumulator.ValueCount >
                                _defaultFormOptions.ValueCountLimit)
                            {
                                // Form key count limit of 
                                // _defaultFormOptions.ValueCountLimit 
                                // is exceeded.
                                ModelState.AddModelError("File",
                                    $"The request couldn't be processed (Error 3).");
                                // Log error

                                return BadRequest(ModelState);
                            }
                        }
                    }
                }

                // Drain any remaining section body that hasn't been consumed and
                // read the headers for the next section.
                section = await reader.ReadNextSectionAsync();
            }

            // Bind form data to the model
            var formData = new StreamFormDataMusic();
            var formValueProvider = new FormValueProvider(
                BindingSource.Form,
                new FormCollection(formAccumulator.GetResults()),
                CultureInfo.CurrentCulture);
            var bindingSuccessful = await TryUpdateModelAsync(formData, prefix: "",
                valueProvider: formValueProvider);
            var keyPathFolder = FilePathUrlHelper.GenerateKeyPath(profile.Id);

            trustedFilePathStorage = String.Format("{0}\\{1}\\{2}\\{3}",
                _targetFolderPath,
                keyPathFolder,
                GenerateSecurity.Encode(profile.Id),
                Path.GetRandomFileName());

            if (!bindingSuccessful)
            {
                ModelState.AddModelError("File",
                    "The request couldn't be processed (Error 5).");
                // Log error

                return BadRequest(ModelState);
            }

            // **WARNING!**
            // In the following example, the file is saved without
            // scanning the file's contents. In most production
            // scenarios, an anti-virus/anti-malware scanner API
            // is used on the file before making the file available
            // for download or for use by other systems. 
            // For more information, see the topic that accompanies 
            // this sample app.

            Directory.CreateDirectory(trustedFilePathStorage);

            foreach (var item in fileStoredData)
            {
                using (var targetStream = System.IO.File.Create(
                            Path.Combine(trustedFilePathStorage, item.Key)))
                {
                    await targetStream.WriteAsync(item.Value);

                    _logger.LogInformation(
                        "Uploaded file '{TrustedFileNameForDisplay}' saved to " +
                        "'{TargetFilePath}' as {TrustedFileNameForFileStorage}",
                        item.Key, trustedFilePathStorage,
                        item.Key);
                }
                storedPaths.Add(Path.Combine(trustedFilePathStorage, item.Key));
                storedPathDictionaryKeys.Add(item.Key);
            }

            var keyValue = storedPathDictionaryKeys[0];
            var keyConvert = fileStoredData[keyValue];
            var file = new ClamUserMusic()
            {
                SongTitle = formData.SongTitle,
                SongArtist = formData.SongArtist,
                ItemPath = storedPaths[0],
                Size =  keyConvert.Length,
                DateCreated = DateTime.Now,
                Status = bool.Parse(formData.Status),
                UserId = profile.Id
            };

            _context.Add(file);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        #endregion

        [Authorize(Policy = "Contributor-Create")]
        [Authorize(Policy = "Permission-Create")]
        [HttpGet("download")]
        public ActionResult DownloadFile(Guid id)
        {
            var requestFile = _context.ClamUserMusic.SingleOrDefault(m => m.SongId == id);
            if (requestFile == null)
            {
                return null;
            }
            return PhysicalFile(requestFile.ItemPath, MediaTypeNames.Application.Octet, WebUtility.HtmlEncode(FilePathUrlHelper.GetFileAtEndOfPath(requestFile.ItemPath)));
        }
        private static Encoding GetEncoding(MultipartSection section)
        {
            var hasMediaTypeHeader =
                MediaTypeHeaderValue.TryParse(section.ContentType, out var mediaType);

            // UTF-7 is insecure and shouldn't be honored. UTF-8 succeeds in 
            // most cases.
            if (!hasMediaTypeHeader || Encoding.UTF7.Equals(mediaType.Encoding))
            {
                return Encoding.UTF8;
            }

            return mediaType.Encoding;
        }
    }
}