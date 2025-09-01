using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maliev.OrderService.Data.Models
{
    public class Process
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = null!;

        public DateTime CreatedDate { get; set; }

        public DateTime ModifiedDate { get; set; }

        [Column("CategoryID")]
        public int CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public virtual Category Category { get; set; } = null!;

        public virtual ICollection<Order> Order { get; set; } = new List<Order>();
    }
}
