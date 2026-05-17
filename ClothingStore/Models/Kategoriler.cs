

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothingStore.Models;

[Table("Kategoriler")]
public partial class Kategoriler
{
    [Key] 
    public string KategoriId { get; set; } = null!;

    public string? UstKategoriId { get; set; }

    public string KategoriAdi { get; set; } = null!;
}