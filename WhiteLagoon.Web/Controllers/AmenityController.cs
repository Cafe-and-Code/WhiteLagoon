using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using WhiteLagoon.Application.Common.Interfaces;
using WhiteLagoon.Application.Common.Utility;
using WhiteLagoon.Domain.Entities;
using WhiteLagoon.Infrastructure.Data;
using WhiteLagoon.Web.ViewModel;

namespace WhiteLagoon.Web.Controllers
{   
    [Authorize(Roles = SD.Role_Admin)]
    public class AmenityController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public AmenityController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ;
        }

        // GET: VillaController
        public IActionResult Index()
        {
            var amenities = _unitOfWork.Amenity.GetAll(includeProperties: "Villa");
            return View(amenities);
        }

        public IActionResult Create()
        {
            // VillaNumberVM amenityVM = new()
            // {
            //     VillaList =  _db.Villas.ToList().Select(u => new SelectListItem
            //     {
            //         Text = u.Name,
            //         Value = u.Id.ToString()
            //     })
            // };
            // return View(amenityVM);
            AmenityVM amenityVM = new AmenityVM();
            amenityVM.VillaList = _unitOfWork.Villa.GetAll().ToList().Select(u => new SelectListItem
            {
                Text = u.Name,
                Value = u.Id.ToString(),
            });
            return View(amenityVM);
        }

        [HttpPost]
        public IActionResult Create(AmenityVM obj)
        {
            // ModelState.Remove("Villa");

            // bool roomNumberExists = _db.VillaNumbers.Any(u => u.Villa_Number == obj.Amenity.Villa_Number);
            // if(ModelState.IsValid && !roomNumberExists)
            // {
            //     _db.VillaNumbers.Add(obj.Amenity);
            //     _db.SaveChanges();
            //     TempData["success"]="The amenity has been created successfully.";
            //     return RedirectToAction("Index");
            // }
            // if(roomNumberExists)
            // {
            //     TempData["error"]="The amenity already exists.";
            // }
            // obj.VillaList = _db.Villas.ToList().Select(u => new SelectListItem
            // {
            //     Text = u.Name,
            //     Value = u.Id.ToString()
            // });
            // return View(obj);
            if (ModelState.IsValid)
            {
                _unitOfWork.Amenity.Add(obj.Amenity);
                _unitOfWork.Save();
                TempData["success"]="The amenity has been created successfully.";
                return RedirectToAction(nameof(Index));
            }
       
            obj.VillaList = _unitOfWork.Villa.GetAll().ToList().Select(u => new SelectListItem{
                Text = u.Name,
                Value = u.Id.ToString()
            });
            return View(obj);
        }

        public IActionResult Update(int AmenityId)
        {
            // VillaNumberVM amenityVM = new()
            // {
            //     VillaList =  _db.Villas.ToList().Select(u => new SelectListItem
            //     {
            //         Text = u.Name,
            //         Value = u.Id.ToString()
            //     }),
            //     Amenity = _db.VillaNumbers.FirstOrDefault(u => u.Villa_Number == VillaNumberId)
            // };
            // if(amenityVM.Amenity == null)
            // {
            //     return RedirectToAction("Error", "Home");
            // }
            // return View(amenityVM);
            AmenityVM amenityVM = new AmenityVM();
            amenityVM.VillaList = _unitOfWork.Villa.GetAll().Select(u => new SelectListItem{
                Text = u.Name,
                Value = u.Id.ToString()
            });
            amenityVM.Amenity = _unitOfWork.Amenity.Get(u => u.Id == AmenityId);
            if(amenityVM.Amenity == null)
            {
                return RedirectToAction("Error", "Home");
            }
            return View(amenityVM);
        }

        [HttpPost]
        public IActionResult Update(AmenityVM amenityVM)
        {
            // if(ModelState.IsValid)
            // {
            //     _db.VillaNumbers.Update(amenityVM.Amenity);
            //     _db.SaveChanges();
            //     TempData["success"] = "The amenity has been updated successfully.";
            //     return RedirectToAction("Index");
            // }
        
            // amenityVM.VillaList = _db.Villas.ToList().Select(u => new SelectListItem
            // {
            //     Text = u.Name,
            //     Value = u.Id.ToString()
            // });
            // return View(amenityVM);
            if(ModelState.IsValid)
            {
                _unitOfWork.Amenity.Update(amenityVM.Amenity);
                _unitOfWork.Save();
                TempData["success"] = "The amenity has been updated successfully.";
                return RedirectToAction(nameof(Index));
            }      
            amenityVM.VillaList = _unitOfWork.Villa.GetAll().Select(u => new SelectListItem{
                Text = u.Name,
                Value = u.Id.ToString()
            });
            return View(amenityVM);
        }
        public IActionResult Delete(int AmenityId)
        {
            AmenityVM amenityVM = new AmenityVM();
            amenityVM.VillaList = _unitOfWork.Villa.GetAll().Select(u => new SelectListItem{
                Text = u.Name,
                Value = u.Id.ToString()
            });
            amenityVM.Amenity = _unitOfWork.Amenity.Get(u => u.Id == AmenityId);
            if(amenityVM.Amenity == null)
            {
                return RedirectToAction("Error", "Home");
            }
            return View(amenityVM);

        }

        [HttpPost]
        public IActionResult Delete(AmenityVM amenityVM)
        {
            Amenity? objFormDb = _unitOfWork.Amenity.Get(u => u.Id == amenityVM.Amenity.Id);
            if(objFormDb is not null)
            {
                _unitOfWork.Amenity.Remove(objFormDb);
                _unitOfWork.Save();
                TempData["success"]="The amenity has been deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            TempData["error"]="The amenity could not be deleted.";
            return View();
        }
    }
}
