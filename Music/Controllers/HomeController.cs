using System;
using System.Threading.Tasks;
using AutoMapper;
using Clam.Repository;
using ClamDataLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Clam.Areas.Music.Controllers
{
    [Authorize(Policy = "Level-One")]
    [Area("Music")]
    [Route("music")]
    public class HomeController : Controller
    {

        private readonly UserManager<ClamUserAccountRegister> _userManager;
        private readonly RoleManager<ClamRoles> _roleManager;
        private readonly IMapper _mapper;
        private readonly UnitOfWork _unitOfWork;

        public HomeController(UserManager<ClamUserAccountRegister> userManager, RoleManager<ClamRoles> roleManager,
            IMapper mapper, UnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        // GET: Home
        public async Task<IActionResult> Index(string search)
        {
            // First initial check -> If user not authenticated, deny access
            if (!User.Identity.IsAuthenticated)
            {
                return View("AccessDenied");
            }
            var model = await _unitOfWork.MusicControl.GetDisplayHomeContent(search);
            return View(model);
        }
    }
}