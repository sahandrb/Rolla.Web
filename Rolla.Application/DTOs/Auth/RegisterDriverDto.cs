using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Rolla.Application.DTOs.Auth;

public class RegisterDriverDto
{
    [Required(ErrorMessage = "مدل خودرو الزامی است")]
    public string CarModel { get; set; } = default!;

    [Required(ErrorMessage = "پلاک خودرو الزامی است")]
    public string PlateNumber { get; set; } = default!;

    [Required(ErrorMessage = "عکس کارت ملی الزامی است")]
    public IFormFile NationalCardImage { get; set; } = default!;

    [Required(ErrorMessage = "عکس گواهینامه الزامی است")]
    public IFormFile LicenseImage { get; set; } = default!;

    [Required(ErrorMessage = "عکس خودرو الزامی است")]
    public IFormFile CarImage { get; set; } = default!;
}