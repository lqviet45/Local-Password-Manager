using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using PasswordManager.Domain.Exceptions;
using PasswordManager.Domain.Interfaces;
using PasswordManager.Domain.ValueObjects;

namespace PasswordManager.Infrastructure.Cryptography;

/// <summary>
/// Production-grade cryptography provider using:
/// - Argon2id for key derivation (OWASP recommended)
/// - AES-256-GCM for authenticated encryption
/// </summary>
public sealed class CryptoProvider : ICryptoProvider
{
    // Argon2id parameters (OWASP recommendations for 2024)
    private const int Argon2DegreeOfParallelism = 4;
    private const int Argon2MemorySize = 65536; // 64 MB
    private const int Argon2Iterations = 3;
    private const int SaltSize = 32; // 256 bits
    private const int KeySize = 32; // 256 bits for AES-256
    
    // AES-GCM parameters
    private const int NonceSize = 12; // 96 bits (recommended for GCM)
    private const int TagSize = 16; // 128 bits

    public async Task<(byte[] Key, byte[] Salt)> DeriveKeyAsync(string password, byte[]? salt = null, int keySize = KeySize)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        
        salt ??= GenerateRandomKey(SaltSize);
        
        try
        {
            using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                DegreeOfParallelism = Argon2DegreeOfParallelism,
                MemorySize = Argon2MemorySize,
                Iterations = Argon2Iterations
            };

            var key = await Task.Run(() => argon2.GetBytes(keySize));
            return (key, salt);
        }
        catch (Exception ex)
        {
            throw new EncryptionFailedException("Failed to derive key using Argon2id", ex);
        }
    }

    public async Task<EncryptedData> EncryptAsync(string plaintext, byte[] key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintext);
        ArgumentNullException.ThrowIfNull(key);
        
        if (key.Length != KeySize)
        {
            throw new ArgumentException($"Key must be {KeySize} bytes (256 bits)", nameof(key));
        }

        try
        {
            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            var nonce = GenerateRandomKey(NonceSize);
            var tag = new byte[TagSize];
            var ciphertext = new byte[plaintextBytes.Length];

            using var aes = new AesGcm(key, TagSize);
            await Task.Run(() => aes.Encrypt(nonce, plaintextBytes, ciphertext, tag));

            return new EncryptedData
            {
                Ciphertext = Convert.ToBase64String(ciphertext),
                IV = Convert.ToBase64String(nonce),
                Tag = Convert.ToBase64String(tag)
            };
        }
        catch (Exception ex)
        {
            throw new EncryptionFailedException("Failed to encrypt data using AES-256-GCM", ex);
        }
    }

    public async Task<string> DecryptAsync(EncryptedData encryptedData, byte[] key)
    {
        ArgumentNullException.ThrowIfNull(encryptedData);
        ArgumentNullException.ThrowIfNull(key);
        
        if (!encryptedData.IsValid())
        {
            throw new DecryptionFailedException("Invalid encrypted data format");
        }
        
        if (key.Length != KeySize)
        {
            throw new ArgumentException($"Key must be {KeySize} bytes (256 bits)", nameof(key));
        }

        try
        {
            var ciphertext = Convert.FromBase64String(encryptedData.Ciphertext);
            var nonce = Convert.FromBase64String(encryptedData.IV);
            var tag = Convert.FromBase64String(encryptedData.Tag);
            var plaintext = new byte[ciphertext.Length];

            using var aes = new AesGcm(key, TagSize);
            await Task.Run(() => aes.Decrypt(nonce, ciphertext, tag, plaintext));

            return Encoding.UTF8.GetString(plaintext);
        }
        catch (CryptographicException ex)
        {
            throw new DecryptionFailedException("Failed to decrypt data. Invalid key or corrupted data.", ex);
        }
        catch (Exception ex)
        {
            throw new DecryptionFailedException("Failed to decrypt data using AES-256-GCM", ex);
        }
    }

    public async Task<string> HashPasswordAsync(string password, byte[]? salt = null)
    {
        var (key, usedSalt) = await DeriveKeyAsync(password, salt);
        
        // Format: $argon2id$v=19$m=65536,t=3,p=4$salt$hash
        return $"$argon2id$v=19$m={Argon2MemorySize},t={Argon2Iterations},p={Argon2DegreeOfParallelism}$" +
               $"{Convert.ToBase64String(usedSalt)}${Convert.ToBase64String(key)}";
    }

    public async Task<bool> VerifyPasswordAsync(string password, string hash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);

        try
        {
            // Parse hash format: $argon2id$v=19$m=65536,t=3,p=4$salt$hash
            var parts = hash.Split('$');
            if (parts.Length != 6 || parts[1] != "argon2id")
            {
                throw new ArgumentException("Invalid hash format", nameof(hash));
            }

            var salt = Convert.FromBase64String(parts[4]);
            var expectedHash = Convert.FromBase64String(parts[5]);

            var (computedHash, _) = await DeriveKeyAsync(password, salt, expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(computedHash, expectedHash);
        }
        catch (Exception ex)
        {
            throw new DecryptionFailedException("Failed to verify password hash", ex);
        }
    }

    public byte[] GenerateRandomKey(int keySize = KeySize)
    {
        var key = new byte[keySize];
        RandomNumberGenerator.Fill(key);
        return key;
    }

    public string ComputeHash(string data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(data);
        
        var bytes = Encoding.UTF8.GetBytes(data);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}