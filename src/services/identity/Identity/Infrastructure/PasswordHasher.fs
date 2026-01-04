namespace Identity.Infrastructure

open System
open System.Security.Cryptography
open System.Text
open Konscious.Security.Cryptography

module PasswordHasher =

    let private saltSize = 16
    let private hashSize = 32
    let private iterations = 3
    let private memorySize = 65536 // 64 MB
    let private parallelism = 4

    let hash (password: string) =
        let salt = RandomNumberGenerator.GetBytes(saltSize)
        use argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        argon2.Salt <- salt
        argon2.DegreeOfParallelism <- parallelism
        argon2.MemorySize <- memorySize
        argon2.Iterations <- iterations
        let hash = argon2.GetBytes(hashSize)
        Convert.ToBase64String(Array.concat [| salt; hash |])

    let verify (password: string) (storedHash: string) =
        try
            let combined = Convert.FromBase64String(storedHash)
            let salt = combined.[0..saltSize-1]
            let expectedHash = combined.[saltSize..]
            use argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            argon2.Salt <- salt
            argon2.DegreeOfParallelism <- parallelism
            argon2.MemorySize <- memorySize
            argon2.Iterations <- iterations
            let computedHash = argon2.GetBytes(hashSize)
            CryptographicOperations.FixedTimeEquals(
                ReadOnlySpan<byte>(computedHash),
                ReadOnlySpan<byte>(expectedHash))
        with _ -> false
