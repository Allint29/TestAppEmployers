using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace EmplApp.Models
{
    /// <summary>
    /// Сотрудник
    /// </summary>
    public class Employee :IQueryable<Employee>
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Поле имени должно быть заполнено")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Длина имени должна быть от 3 до 50 символов")]
        //[RegularExpression(@"[A-Za-z]", ErrorMessage = "Можно вводить только русские и латинские буквы")]
        [Display(Name = "Имя")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Поле фамилии должно быть заполнено")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Длина фамилии должна быть от 2 до 50 символов")]
        //[RegularExpression(@"[A-Za-z]", ErrorMessage = "Можно вводить только русские и латинские буквы")]
        [Display(Name = "Фамилия")]
        public string LastName { get; set; }

        [Display(Name = "Отчество")]
        public string FatherName { get; set; }

        [Required(ErrorMessage = "Поле даты рождения должно быть заполнено")]
        [Display(Name = "Дата рождения")]
        public DateTime Birthday { get; set; } = new DateTime(1960, 1, 1);

        [Display(Name = "Мужской пол")] 
        public Boolean Man { get; set; } = true;

        [Required(ErrorMessage = "Поле даты устройства на работу должно быть заполнено")]
        [Display(Name = "Дата устройства на работу")]
        public DateTime Employmentday { get; set; }

        [Display(Name = "Уволен")]
        public Boolean Fired { get; set; } = false;

        [Display(Name = "Дата увольнения")]
        public DateTime Dismissalday { get; set; }

        [Display(Name = "Женат/Замужем")]
        public Boolean IsMarried { get; set; } = false;

        [Display(Name = "Личный автомобиль")]
        public Boolean HasAuto { get; set; } = false;

        [Display(Name = "Комментарий")]
        public string Comment { get; set; }

        //навигация
        [Display(Name = "Отдел")]
        public int DepId { get; set; }
        public Dep Dep { get; set; }

        [Display(Name = "Должность")]
        public int PositionId { get; set; }
        public Position Position { get; set; }


        public IEnumerator<Employee> GetEnumerator()
        {
            return null;
            //throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Type ElementType { get; }
        public Expression Expression { get; }
        public IQueryProvider Provider { get; }
    }
}
