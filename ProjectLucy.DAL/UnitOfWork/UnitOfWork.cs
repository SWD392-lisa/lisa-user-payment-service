using ProjectLucy.DAL.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectLucy.DAL.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly NeondbContext _context;


        public UnitOfWork(NeondbContext context)
        {
            _context = context;
        }

        public void Dispose()
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
