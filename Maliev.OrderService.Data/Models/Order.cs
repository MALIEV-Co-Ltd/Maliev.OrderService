using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maliev.OrderService.Data.Models
{
    public class Order
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [StringLength(100)]
        public string Name { get; set; } = "unnamed";

        [StringLength(250)]
        public string? Description { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(5, 2)")]
        public decimal DiscountPercent { get; set; }

        public int Manufactured { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal Subtotal { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public int Remaining { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime ModifiedDate { get; set; }

        [Column(TypeName = "date")]
        public DateTime? PromisedDate { get; set; }

        [Column(TypeName = "date")]
        public DateTime? FinishedDate { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public int? Turnaround { get; set; }

        [StringLength(250)]
        public string? TrackingNumber { get; set; }

        [Column("ProcessID")]
        public int ProcessId { get; set; }

        [Column("ColorID")]
        public int? ColorId { get; set; }

        [Column("CurrencyID")]
        public int? CurrencyId { get; set; }

        [Column("CustomerID")]
        public int? CustomerId { get; set; }

        [Column("EmployeeID")]
        public int? EmployeeId { get; set; }

        [Column("MaterialID")]
        public int? MaterialId { get; set; }

        [Column("SurfaceFinishID")]
        public int? SurfaceFinishId { get; set; }

        [ForeignKey(nameof(ProcessId))]
        public virtual Process Process { get; set; } = null!;

        public virtual ICollection<OrderFile> OrderFile { get; set; } = new List<OrderFile>();
    }
}
