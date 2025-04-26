using WhiteLagoon.Application.Common.Interfaces;
using WhiteLagoon.Domain.Entities;
using WhiteLagoon.Infrastructure.Data;
using WhiteLagoon.Infrastructure.Repository;

namespace WhiteLagoon.Infrastructure.Repository;

public class VillaNumberRepository : Repository<VillaNumber>, IVillaNumberRepository
{
    private readonly ApplicationDbContext _db;
    public VillaNumberRepository(ApplicationDbContext db) : base(db)
    {
        _db = db ;
    }

    public void Update(VillaNumber entity)
    {
        _db.VillaNumbers.Update(entity);
    }
}
