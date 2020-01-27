using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EmplApp.Models
{
    /// <summary>
    /// Модель отдела предприятия
    /// </summary>
    public class Dep
    {

        public int Id { get; set; }

        [Required(ErrorMessage = "Поле 'Отдел' должно быть заполнено")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Длина поля 'Отдел' должна быть от 3 до 50 символов")]
        [Display(Name = "Отдел")]
        public string Name { get; set; }

        //навигация
        public List<Employee> Employers { get; set; }

        public Dep()
        {
            Employers = new List<Employee>();
        }
    }
}
