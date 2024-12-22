using CarManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace CarManagement.Persistence;

public class CarDbContext(DbContextOptions<CarDbContext> options) : DbContext(options)
{
    public DbSet<Car> Cars => Set<Car>();
}