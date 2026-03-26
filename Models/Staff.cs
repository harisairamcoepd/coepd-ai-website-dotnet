using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Coepd.Web.Models
{
    [Table("staff")]
    public class Staff
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(120)] public string Name { get; set; }
        [Required, MaxLength(120)] public string Email { get; set; }
        [Required, MaxLength(255)] public string PasswordHash { get; set; }
        [Required, MaxLength(20)] public string Role { get; set; }
        [Required, MaxLength(20)] public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
