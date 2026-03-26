using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Coepd.Web.Models
{
    [Table("leads")]
    public class Lead
    {
        [Key]
        public int Id { get; set; }
        [MaxLength(255)] public string Name { get; set; }
        [MaxLength(50)] public string Phone { get; set; }
        [MaxLength(255)] public string Email { get; set; }
        [MaxLength(255)] public string Location { get; set; }
        [MaxLength(255)] public string InterestedDomain { get; set; }
        [MaxLength(50)] public string Whatsapp { get; set; }
        [MaxLength(50)] public string Source { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
