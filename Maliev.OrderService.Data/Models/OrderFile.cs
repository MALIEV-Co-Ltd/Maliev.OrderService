using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maliev.OrderService.Data.Models
{
    public class OrderFile
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Bucket { get; set; } = null!;

        [Required]
        public string ObjectName { get; set; } = null!;

        public DateTime CreatedDate { get; set; }

        public DateTime ModifiedDate { get; set; }

        [Column("OrderID")]
        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public virtual Order Order { get; set; } = null!;
    }
}
