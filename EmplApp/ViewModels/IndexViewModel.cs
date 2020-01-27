using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmplApp.Models;

namespace EmplApp.ViewModels
{
    public class IndexViewModel
    {
        public IEnumerable<PositionViewModel> Positions { get; set; }
        public IEnumerable<DepViewModel> Deps { get; set; }
        public IQueryable<Employee> Employers { get; set; }
    }
}
