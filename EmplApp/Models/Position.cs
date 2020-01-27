using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EmplApp.Models
{
    /// <summary>
    /// Модель должности на предприятии
    /// </summary>
    public class Position
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Поле 'Должность' должно быть заполнено")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Длина поля 'Должность' должна быть от 3 до 50 символов")]
        [Display(Name = "Должность")]
        public string Name { get; set; }

        //навигация
        public List<Employee> Employers { get; set; }

        public Position()
        {
            Employers = new List<Employee>();
        }


    }
}
