using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Ali25_V10.Data.Modelos;
using Ali25_V10.Data.Sistema;

namespace Ali25_V10.Data;
public class ReadDbContext(DbContextOptions<ReadDbContext> options) : DbContext(options)
    {
    }
