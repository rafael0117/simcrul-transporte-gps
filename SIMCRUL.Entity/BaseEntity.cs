namespace SIMCRUL.Entity;

public abstract class BaseEntity
{
    // Mapped as bool/bit in EF Core.
    // Some tables use 'activo' (bit), some use 'estado' (int or bit). We will map them specifically in configurations.
    // But we can keep BaseEntity simple.
}
