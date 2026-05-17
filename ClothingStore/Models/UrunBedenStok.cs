using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothingStore.Models
{
    public partial class UrunBedenStok
    {
        [Key]
        public int UrunBedenId { get; set; }

        public int? UrunId { get; set; }

        public int? BedenId { get; set; }

        public int StokAdedi { get; set; }

        [ForeignKey("BedenId")]
        public virtual Bedenler Beden { get; set; }

        [ForeignKey("UrunId")]
        public virtual Urunler Urun { get; set; }
    }
}