using RazorPagesAssignment.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace RazorPagesAssignment.Data
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