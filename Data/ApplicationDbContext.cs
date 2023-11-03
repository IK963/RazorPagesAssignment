using DotNetCore_Task3.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DotNetCore_Task3.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public DbSet<ToDo> ToDo { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {

        }
    }
}