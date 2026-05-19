using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ProjectLucy.DAL.UnitOfWork
{
    public interface IUnitOfWork : IDisposable // quan ly bo nho
    {
        Task<int> SaveChangesAsync();


    }
}
