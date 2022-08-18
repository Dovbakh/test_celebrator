using System.ComponentModel.DataAnnotations;

namespace celebrator.Models
{
    public class Person
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(100)]
        [Required]
        [Display(Name = "Имя")]
        public string Name { get; set; }

        [DataType(DataType.Date)]
        [Required]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "День рождения")]
        public DateTime BirthDate { get; set; }

        [Display(Name = "Исполняется")]
        public int Age { get { return DateTime.Now.Year - BirthDate.Year; } }

        [MaxLength(500)]
        [Display(Name = "Фото")]
        public string? ImageSrc { get; set; }

        public bool isPast()
        {
            return (DateTime.Now.DayOfYear - BirthDate.DayOfYear) > 0 ?
                true : false;
        }


    }
}
