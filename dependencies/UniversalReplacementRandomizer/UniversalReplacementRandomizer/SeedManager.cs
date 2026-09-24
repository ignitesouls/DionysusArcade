// SPDX-License-Identifier: GPL-3.0-only
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace UniversalReplacementRandomizer;

/// <summary>
/// The purpose of the seed manager is to enable deterministic, order-independent randomization.
/// </summary>
public class SeedManager
{
    // base seed is the singular value that will be shared with users to enable deterministic randomization
    private readonly int _baseSeed;
    private readonly string? _prefix;

    // keep track of each namespaced random number generator
    private readonly ConcurrentDictionary<string, Random> _namespaceStrRngs = new();
    private readonly ConcurrentDictionary<int, Random> _namespaceIntRngs = new();

    public SeedManager(string? prefix = null, int? baseSeed = null)
    {
        if (prefix != null)
        {
            _prefix = prefix;
        }

        _baseSeed = baseSeed ?? GenerateSecureSeed();
    }

    // necessary for deterministic randomization
    public int GetBaseSeed() { return _baseSeed; }

    // cryptographically secure randomness
    private static int GenerateSecureSeed()
    {
        byte[] bytes = new byte[4];
        RandomNumberGenerator.Fill(bytes);
        return BitConverter.ToInt32(bytes, 0) & int.MaxValue;
    }

    private int DeriveSeedFromKey(int baseSeed, string context)
    {
        // seeds are generated from hashing: seed = Sha256(prefixBytes + baseSeedBytes + contextBytes)
        //
        // baseSeed              : 7777777
        // DLC Randomizer        : prefix "dlc"
        // BaseGame Randomizer   : prefix "base"
        //
        // int dlcSeed      = DeriveSeedFromKey(7777777, "common");
        // int baseGameSeed = DeriveSeedFromKey(7777777, "common");
        //
        // we are guaranteed that the following property holds:
        //     baseGameSeed != dlcSeed
        // it is possible, by coincidence, that downstream randomized groupings are identical
        // but it's fine, because that's not predictable.
        using var sha256 = SHA256.Create();

        byte[] baseBytes = BitConverter.GetBytes(baseSeed);
        if (_prefix != null)
        {
            byte[] prefixBytes = Encoding.UTF8.GetBytes(_prefix);
            baseBytes = prefixBytes.Concat(baseBytes).ToArray();
        }
        else
        {
            baseBytes = BitConverter.GetBytes(baseSeed);
        }
        byte[] contextBytes = Encoding.UTF8.GetBytes(context);
        byte[] combined = baseBytes.Concat(contextBytes).ToArray();
        byte[] hash = sha256.ComputeHash(combined);
        return BitConverter.ToInt32(hash, 0) & int.MaxValue;
    }

    public Random GetRandomByKey(string key)
    {
        // if the random number generator exists, return it. else, create it and then return it.
        return _namespaceStrRngs.GetOrAdd(key, k =>
        {
            int derivedSeed = DeriveSeedFromKey(_baseSeed, k);
            return new Random(derivedSeed);
        });
    }

    private int DeriveSeedFromId(int baseSeed, int context)
    {
        // seeds are generated from hashing: seed = Sha256(baseSeedBytes + contextBytes)
        using var sha256 = SHA256.Create();
        byte[] baseBytes = BitConverter.GetBytes(baseSeed);
        if (_prefix != null)
        {
            byte[] prefixBytes = Encoding.UTF8.GetBytes(_prefix);
            baseBytes = prefixBytes.Concat(baseBytes).ToArray();
        }
        else
        {
            baseBytes = BitConverter.GetBytes(baseSeed);
        }
        byte[] contextBytes = BitConverter.GetBytes(context);
        byte[] combined = baseBytes.Concat(contextBytes).ToArray();
        byte[] hash = sha256.ComputeHash(combined);

        return BitConverter.ToInt32(hash, 0) & int.MaxValue;
    }

    public Random GetRandomById(int id)
    {
        // if the random number generator exists, return it. else, create it and then return it.
        return _namespaceIntRngs.GetOrAdd(id, i =>
        {
            int derivedSeed = DeriveSeedFromId(_baseSeed, i);
            return new Random(derivedSeed);
        });
    }
}
