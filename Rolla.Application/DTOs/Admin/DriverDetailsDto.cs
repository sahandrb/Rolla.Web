using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rolla.Domain.Enums;

namespace Rolla.Application.DTOs.Admin;

public class DriverDetailsDto
{
    public string Id { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? FullName { get; set; }
    public string CarModel { get; set; } = default!;
    public string PlateNumber { get; set; } = default!;
    public DriverStatus Status { get; set; }

    // لیست مدارک کاربر
    public List<DriverDocumentDto> Documents { get; set; } = new();
}

public class DriverDocumentDto
{
    public int Id { get; set; }
    public DocumentType Type { get; set; }
    public string ContentType { get; set; } = default!;
}