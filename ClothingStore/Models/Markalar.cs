using System.ComponentModel.DataAnnotations;

namespace ClothingStore.Models
{
    public class Markalar
    {
        [Key]
        public int MarkaId { get; set; }

        [Required]
        public string MarkaAdi { get; set; }

        public string MarkaResimYolu { get; set; } 

        public virtual ICollection<Urunler> Urunlers { get; set; }
    }
}