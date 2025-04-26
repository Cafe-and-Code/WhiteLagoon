using WhiteLagoon.Application.Common.Interfaces;
using WhiteLagoon.Domain.Entities;
using WhiteLagoon.Infrastructure.Data;
using WhiteLagoon.Infrastructure.Repository;

namespace WhiteLagoon.Infrastructure.Repository;

public class AmentityRepository : Repository<Amenity>, IAmentityRepository
{
    private readonly ApplicationDbContext _db;
    public AmentityRepository(ApplicationDbContext db) : base(db)
    {
        _db = db ;
    }

    public void Update(Amenity entity)
    {
        _db.Amenities.Update(entity);
    }
}
