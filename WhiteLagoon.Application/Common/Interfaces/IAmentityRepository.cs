using WhiteLagoon.Domain.Entities;

namespace WhiteLagoon.Application.Common.Interfaces;

public interface IAmentityRepository : IRepository<Amenity>
{
    void Update(Amenity entity);
}
