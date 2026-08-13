// Summary: Controls how a thrown projectile orients itself in flight.
public enum ThrowableOrientation
{
    VelocityAligned,    // Faces travel direction each frame (knives, arrows, spears)
    Tumble,             // Spins with angular velocity (grenades, bottles, rocks)
    Fixed               // Maintains launch rotation (magic projectiles, energy balls)
}
