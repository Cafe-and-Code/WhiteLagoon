using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations.Schema;

namespace WhiteLagoon.Domain.Entities;

public class VillaNumber
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Display(Name ="Villa number")]
    public int Villa_Number { get; set; }
    [ForeignKey("Villa")]
    public int VillaId { get; set; }
    [ValidateNever]
    public required Villa Villa { get; set; }
    public string? SpecialDetail { get; set; }
}
