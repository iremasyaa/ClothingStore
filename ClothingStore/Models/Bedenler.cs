using System;
using System.Collections.Generic;

namespace ClothingStore.Models
{
    public partial class Bedenler
    {
        public int BedenId { get; set; }
        public string BedenTanimi { get; set; }
        public virtual ICollection<UrunBedenStok> UrunBedenStoklar { get; set; } = new List<UrunBedenStok>();
    }
}