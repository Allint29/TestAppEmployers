using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using EmplApp.Models;
using EmplApp.Services;
using EmplApp.Utils;
using EmplApp.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;

namespace EmplApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        public IConfiguration _configuration { get; }
        private MyDbContext _db;

        public HomeController(ILogger<HomeController> logger, MyDbContext context,  IConfiguration configuration)
        {
            _logger = logger;
            _db = context;
            _configuration = configuration;
        }
        
        /// <summary>
        /// Подключает кастомный DbContext, создает таблицы если их нет
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Createdatabase()
        {
            await _db.CreateNewDatabase(_configuration.GetSection("ConnectionStringsToMyDbContext")["MyDbContext"]);
            await _db.CreateAllTable(new List<object>() { new Position(), new Dep(), new Employee() });
            await _db.PutStoreProcedure(new List<object>() { new Position(), new Dep(), new Employee() });

            //использую хранимую процедуру - она должна быть
            //заранее создана из пункта меню Инициализации БД
            if (await _db.CountStorageProcedure(new Position()) < 3)
            {

                var list_position = new List<string>() { "Директор", "Менеджер", "Инженер", "Бухгалтер", "Дизайнер" };
                var list_deps = new List<string>() { "Управление", "Бухгалтерия", "Прозводство", "Маркетинг" };

                foreach (var i in list_position)
                {
                    await _db.Insert(new Position() { Name = i });
                }

                foreach (var i in list_deps)
                {
                    await _db.Insert(new Dep() { Name = i });
                }
            }
            return RedirectToAction("Index");
        }
        
        /// <summary>
        /// Добавляет сотрудника автоматом заполняя поля
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Initialize()
        {
            var rnd = new Random();
            var list_names = new List<string>(){"Алексей", "Сергей", "Иван" , "Дмитрий" , "Петр" , "Тимур" };
            int numName = rnd.Next(list_names.Count());
            var list_fathername = new List<string>() { "Алексеевич", "Сергеевич", "Иванович", "Дмитриевич", "Петрович", "Тимурович" };
            int numNameFatherName = rnd.Next(list_fathername.Count());
            var list_lastname = new List<string>() { "Сергеев", "Бадаев", "Пупкин", "Иванов", "Депордье", "Филимонов" };
            int numLastName= rnd.Next(list_lastname.Count());

            var emp1 = new Employee
             {
                 FirstName = list_names[numName],
                 LastName = list_lastname[numLastName],
                 FatherName = list_fathername[numNameFatherName],
                 Man = true,
                 Birthday = new DateTime(rnd.Next(1985,1999), rnd.Next(1, 11), rnd.Next(1, 25)),
                 IsMarried = true,
                 HasAuto = false,
                 Employmentday = new DateTime(rnd.Next(2005, 2018), rnd.Next(1, 11), rnd.Next(1, 25)),
                 Comment = "Комментарий к" + list_lastname[numLastName],
                 PositionId = rnd.Next(1, 5),
                 DepId = rnd.Next(1, 4),
                 Fired = false,
                 Dismissalday = DateTime.MaxValue
             };

            //await _dbEF.AddRangeAsync(emp1);
            //await _dbEF.SaveChangesAsync();
            await _db.Insert(emp1);

            var listUsers = await _db.GetAllEmployersStoreProc();
            foreach (var i in listUsers)
            {
                Debug.WriteLine(i.FirstName.ToString());
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Выводит всех сотрудников с фильтром
        /// </summary>
        /// <param name="positionId"></param>
        /// <param name="departamentId"></param>
        /// <returns></returns>
        public async Task<IActionResult> Index(int? positionId, int? departamentId, string? find_str, SortState sortOrder = SortState.NameAsc)
        {
            //Создаю подключение при первом запуске, если БД нет, то создаю ее и все
            //сопутствующие представления и хранимые процедуры
            await Createdatabase();

            ViewData["NameSort"] = sortOrder == SortState.NameAsc ? SortState.NameDesc : SortState.NameAsc;

            List<PositionViewModel> positionsModel = _db.GetAllPositionsStoreProc()
                .Result
                .Select(p => new PositionViewModel { Id = p.Id, Name = p.Name })
                .ToList();
            positionsModel.Insert(0, new PositionViewModel { Id = 0, Name = "Все" });
            
            List<DepViewModel> depModel = _db.GetAllDepStoreProc()
                .Result
                .Select(d => new DepViewModel { Id = d.Id, Name = d.Name })
                .ToList();
            depModel.Insert(0, new DepViewModel { Id = 0, Name = "Все" });
            IQueryable<Employee> emplModel = null;

            if (String.IsNullOrEmpty(find_str))
                emplModel = _db.GetAllEmployersStoreProc().Result;
            else
                 emplModel = _db.FindEmploees(find_str).Result;
            emplModel.OrderByDescending(d => d.LastName);

            emplModel = sortOrder switch
                {
                SortState.NameDesc => emplModel.OrderByDescending(s => s.LastName),
                _ => emplModel.OrderBy(s => s.LastName),
                };
            
            IndexViewModel ivm = new IndexViewModel { Positions = positionsModel, Deps = depModel,  Employers = emplModel};
            
            if (positionId != null && positionId > 0)
                ivm.Employers = (IQueryable<Employee>)emplModel.Where(e => e.PositionId == positionId);

            if (departamentId != null && departamentId > 0)
                ivm.Employers = (IQueryable<Employee>)emplModel.Where(e => e.DepId == departamentId);
            
            return View(ivm);
        }

        public IActionResult Createposition() => View();

        [HttpPost]
        public async Task<IActionResult> Createposition(Position position)
        {
            if (ModelState.IsValid && position != null)
            {
                await _db.Insert(position);
                return RedirectToAction("Index");
            }
            return View(position);
        }

        public IActionResult Createdepartament() => View();

        [HttpPost]
        public async Task<IActionResult> Createdepartament(Dep dep)
        {
            if (ModelState.IsValid && dep != null)
            {
                await _db.Insert(dep);
                return RedirectToAction("Index");
            }
            return View(dep);
        }

        /// <summary>
        /// Маршрут создания пользователя вручную
        /// </summary>
        /// <returns></returns>
        public async  Task<IActionResult> Create()
        {
            var pos = _db.GetAllPositionsStoreProc().Result;
            var dep = _db.GetAllDepStoreProc().Result;
            ViewBag.Positions = new SelectList(pos, "Id", "Name"); 
            ViewBag.Departaments = new SelectList(dep, "Id", "Name");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Employee employee)
        {
            if (ModelState.IsValid && employee != null)
            {
                await _db.Insert(employee);
                return RedirectToAction("Index");
            }
            return View(employee);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id != null)
            {
                var pos = _db.GetAllPositionsStoreProc().Result;
                var dep = _db.GetAllDepStoreProc().Result;
                ViewBag.Positions = new SelectList(pos, "Id", "Name");
                ViewBag.Departaments = new SelectList(dep, "Id", "Name");
                
                Employee empl = await _db.TakeFirstEmployee(id);

                if (empl != null)
                    return View(empl);
            }
            return NotFound();
        }
        [HttpPost]
        public async Task<IActionResult> Edit(Employee employee)
        {
            if (ModelState.IsValid && employee != null)
            {
                await _db.Update(employee, employee.Id);
                return RedirectToAction("Index");
            }
            return View(employee);
        }
        /// <summary>
        /// Удаление работника из базы
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [ActionName("Delete")]
        public async Task<IActionResult> ConfirmDelete(int? id)
        {
            if (id != null)
            {
                Employee empl = await _db.TakeFirstEmployee(id);
                if (empl != null)
                    return View(empl);
            }
            return NotFound("Сотрудник не найден!");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id != null)
            {
                Employee empl = await _db.TakeFirstEmployee(id);
                if (empl != null)
                {
                    await _db.Delete(id);
                    return RedirectToAction("Index");
                }
            }
            return NotFound("Сотрудник не найден!");
        }


        /// <summary>
        /// Удаление работника из базы
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [ActionName("Fire")]
        public async Task<IActionResult> ConfirmFire(int? id)
        {
            if (id != null)
            {
                var pos = _db.GetAllPositionsStoreProc().Result;
                var dep = _db.GetAllDepStoreProc().Result;
                ViewBag.Positions = new SelectList(pos, "Id", "Name");
                ViewBag.Departaments = new SelectList(dep, "Id", "Name");

                Employee empl = await _db.TakeFirstEmployee(id);

                if (empl != null)
                    return View(empl);
            }
            return NotFound("Сотрудник не найден!");
        }

        [HttpPost]
        public async Task<IActionResult> Fire(Employee employee)
        {
            if(employee == null) return NotFound("Сотрудник не найден!");

            if(employee.Dismissalday == null || 
               employee.Employmentday > employee.Dismissalday ||
               employee.Dismissalday < new DateTime(1950,1,1)||
               employee.Dismissalday > DateTime.Today)
                     ModelState.AddModelError("Dismissalday", "Не верно указана дата увольнения.");
            if(employee.Fired == false) ModelState.AddModelError("Fired", "Не отмечено полу 'Уволен'.");



            if (ModelState.IsValid)
            {
                await _db.Update(employee, employee.Id);
                return RedirectToAction("Index");
            }
            var pos = _db.GetAllPositionsStoreProc().Result;
            var dep = _db.GetAllDepStoreProc().Result;
            ViewBag.Positions = new SelectList(pos, "Id", "Name");
            ViewBag.Departaments = new SelectList(dep, "Id", "Name");

            return View(employee);
        }

        #region Standart

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        
        #endregion



    }
}
