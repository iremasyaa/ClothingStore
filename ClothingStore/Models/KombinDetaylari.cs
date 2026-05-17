using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; 

namespace ClothingStore.Models
{
    public class KombinDetaylari
    {
        [Key]
        public int KombinDetayId { get; set; }

        public int KombinId { get; set; }

        public int? UrunId { get; set; } 
        public int? DolapId { get; set; } 

        public virtual Kombinler Kombin { get; set; }

        [ForeignKey("UrunId")]
        public virtual Urunler SitedekiUrun { get; set; }

        [ForeignKey("DolapId")]
        public virtual Dolap DolaptakiUrun { get; set; }
    }
}