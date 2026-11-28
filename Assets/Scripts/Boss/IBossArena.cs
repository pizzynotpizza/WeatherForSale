using System.Collections;
using UnityEngine;

namespace WeatherForSale.Boss
{
    /// <summary>
    /// What an attack pattern may ask of the arena: where things are, a place to spawn warning
    /// shapes, and a way to hurt the player. Implemented by <see cref="BossFightController"/>;
    /// patterns never reach for scene objects themselves, so a new pattern needs no change to the boss.
    /// </summary>
    public interface IBossArena
    {
        /// <summary>Middle of the arena floor (floor surface height).</summary>
        Vector3 Center { get; }

        /// <summary>Walkable radius of the arena floor, metres.</summary>
        float Radius { get; }

        /// <summary>Height above <see cref="Center"/> where warning shapes lie, clear of the floor inlays (metres).</summary>
        float WarningHeight { get; }

        /// <summary>The player's transform (feet position).</summary>
        Transform Player { get; }

        /// <summary>The boss's transform (its body centre). Patterns may move it sideways; it comes back to the middle on its own.</summary>
        Transform Boss { get; }

        /// <summary>The boss's resting height (body centre above the floor) from the start of the fight; every attack keeps it.</summary>
        float BossRestHeight { get; }

        /// <summary>Sets the height of the boss body centre above the floor, metres (the boss glides there).</summary>
        void SetBossHeight(float centreHeight);

        /// <summary>Tells the boss that the running pattern moves it sideways itself (the charge), so it must not drift back to the middle meanwhile.</summary>
        void SetBossMoving(bool moving);

        /// <summary>Runs <paramref name="routine"/> alongside the calling pattern; it stops when the fight ends.</summary>
        Coroutine StartRoutine(IEnumerator routine);

        /// <summary>A flat coloured disc lying on the floor (warning or hit shape). Removed with <see cref="Release"/> or when the fight ends.</summary>
        GameObject SpawnDisc(Vector3 center, float radius, Color color);

        /// <summary>A coloured box (a wind wall or a lightning bolt). Removed with <see cref="Release"/> or when the fight ends.</summary>
        GameObject SpawnBox(Vector3 center, Vector3 size, Quaternion rotation, Color color);

        /// <summary>A shape with its own mesh (the hail ring). The mesh is destroyed with the shape.</summary>
        GameObject SpawnMesh(Mesh mesh, Vector3 position, Color color);

        /// <summary>Removes a shape made by <see cref="SpawnDisc"/>, <see cref="SpawnBox"/> or <see cref="SpawnMesh"/>.</summary>
        void Release(GameObject shape);

        /// <summary>
        /// Throws the player away from a hit. There is no health: being thrown off the edge of the arena is how the
        /// fight is lost. Ignored for a short moment after a throw so overlapping hits do not chain.
        /// </summary>
        /// <param name="pushDirection">Horizontal direction the player is thrown in (does not need to be normalised).</param>
        /// <param name="pushSpeed">Speed of the throw, metres per second.</param>
        void HitPlayer(Vector3 pushDirection, float pushSpeed);

        /// <summary>A quick white screen flash (lightning).</summary>
        void Flash();
    }
}
