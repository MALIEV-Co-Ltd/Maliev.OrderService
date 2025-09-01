using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maliev.OrderService.Data.Models
{
    public class Category
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [StringLength(50)]
        public string? Name { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime ModifiedDate { get; set; }

        public virtual ICollection<Process> Process { get; set; } = new List<Process>();
    }
}
