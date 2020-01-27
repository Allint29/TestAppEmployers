using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EmplApp.ViewModels
{
    public class PositionViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Поле 'Должность' должно быть заполнено")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Длина поля 'Должность' должна быть от 3 до 50 символов")]
        [Display(Name = "Должность")]
        public string Name { get; set; }
    }
}
