using WhiteLagoon.Application.Common.Interfaces;
using WhiteLagoon.Domain.Entities;
using WhiteLagoon.Infrastructure.Data;

namespace WhiteLagoon.Infrastructure.Repository;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _db;
    public IVillaRepository Villa { get; private set;}
    public IVillaNumberRepository VillaNumber { get; private set;}

    public IAmentityRepository Amenity { get; private set;}
    public IBookingRepository Booking { get; private set;}
    public IApplicationUserRepository User { get; private set;}
    public UnitOfWork(ApplicationDbContext db)
    {
        _db = db;
        Amenity = new AmentityRepository(_db);
        Villa = new VillaRepository(_db);
        VillaNumber = new VillaNumberRepository(_db);
        Booking = new BookingRepository(_db);
        User = new ApplicationUserRepository(_db);
    }

    public void Save()
    {
        _db.SaveChanges();
    }
}
