using System;
using NSec.Cryptography;
using Xunit;

namespace NSec.Tests.Base
{
    public static class AeadAlgorithmTests
    {
        public static readonly TheoryData<AeadAlgorithm> AeadAlgorithms = Registry.AeadAlgorithms;

        private const int L = 547;

        #region Properties

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void Properties(AeadAlgorithm a)
        {
            Assert.True(a.KeySize > 0);
            Assert.InRange(a.NonceSize, 0, 24);
            Assert.InRange(a.TagSize, 0, 255);
        }

        #endregion

        #region Encrypt #1

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void EncryptWithNullKey(AeadAlgorithm a)
        {
            Assert.Throws<ArgumentNullException>("key", () => a.Encrypt(null!, Utilities.RandomBytes.Slice(0, a.NonceSize), ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty));
        }

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void EncryptWithDisposedKey(AeadAlgorithm a)
        {
            var k = new Key(a);
            k.Dispose();
            Assert.Throws<ObjectDisposedException>(() => a.Encrypt(k, Utilities.RandomBytes.Slice(0, a.NonceSize), ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty));
        }

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void EncryptWithWrongKey(AeadAlgorithm a)
        {
            using var k = new Key(SignatureAlgorithm.Ed25519);

            Assert.Throws<ArgumentException>("key", () => a.Encrypt(k, Utilities.RandomBytes.Slice(0, a.NonceSize), ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty));
        }

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void EncryptWithNonceTooSmall(AeadAlgorithm a)
        {
            using var k = new Key(a);

            Assert.Throws<ArgumentException>("nonce", () => a.Encrypt(k, Utilities.RandomBytes.Slice(0, a.NonceSize - 1), ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty));
        }

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void EncryptWithNonceTooLarge(AeadAlgorithm a)
        {
            using var k = new Key(a);

            Assert.Throws<ArgumentException>("nonce", () => a.Encrypt(k, Utilities.RandomBytes.Slice(0, a.NonceSize + 1), ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty));
        }

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void EncryptEmptySuccess(AeadAlgorithm a)
        {
            using var k = new Key(a);

            var b = a.Encrypt(k, Utilities.RandomBytes.Slice(0, a.NonceSize), ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty);
            Assert.NotNull(b);
            Assert.Equal(a.TagSize, b.Length);
        }

        #endregion

        #region Encrypt #2

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void EncryptWithSpanWithNullKey(AeadAlgorithm a)
        {
            Assert.Throws<ArgumentNullException>("key", () => a.Encrypt(null!, Utilities.RandomBytes.Slice(0, a.NonceSize), ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, Span<byte>.Empty));
        }

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void EncryptWithSpanWithDisposedKey(AeadAlgorithm a)
        {
            var k = new Key(a);
            k.Dispose();
            Assert.Throws<ObjectDisposedException>(() => a.Encrypt(k, Utilities.RandomBytes.Slice(0, a.NonceSize), ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, new byte[a.TagSize]));
        }

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void EncryptWithSpanWithWrongKey(AeadAlgorithm a)
        {
            using var k = new Key(SignatureAlgorithm.Ed25519);

            Assert.Throws<ArgumentException>("key", () => a.Encrypt(k, Utilities.RandomBytes.Slice(0, a.NonceSize), ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, Span<byte>.Empty));
        }

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void EncryptWithSpanWithNonceTooSmall(AeadAlgorithm a)
        {
            using var k = new Key(a);

            Assert.Throws<ArgumentException>("nonce", () => a.Encrypt(k, Utilities.RandomBytes.Slice(0, a.NonceSize - 1), ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, Span<byte>.Empty));
        }

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void EncryptWithSpanWithNonceTooLarge(AeadAlgorithm a)
        {
            using var k = new Key(a);

            Assert.Throws<ArgumentException>("nonce", () => a.Encrypt(k, Utilities.RandomBytes.Slice(0, a.NonceSize + 1), ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, Span<byte>.Empty));
        }

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void EncryptEmptyWithSpanTooSmall(AeadAlgorithm a)
        {
            using var k = new Key(a);

            Assert.Throws<ArgumentException>("ciphertext", () => a.Encrypt(k, Utilities.RandomBytes.Slice(0, a.NonceSize), ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, new byte[a.TagSize - 1]));
        }

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void EncryptEmptyWithSpanTooLarge(AeadAlgorithm a)
        {
            using var k = new Key(a);

            Assert.Throws<ArgumentException>("ciphertext", () => a.Encrypt(k, Utilities.RandomBytes.Slice(0, a.NonceSize), ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, new byte[a.TagSize + 1]));
        }

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void EncryptWithNonceOverlapping(AeadAlgorithm a)
        {
            using var k = new Key(a);
            var ad = Utilities.RandomBytes.Slice(0, 100);
            var b = Utilities.RandomBytes.Slice(0, L);

            var expected = new byte[b.Length + a.TagSize];
            var actual = new byte[b.Length + a.TagSize];
            Utilities.RandomBytes.Slice(200, actual.Length).CopyTo(actual);

            a.Encrypt(k, actual.AsSpan(10, a.NonceSize), ad, b, expected);
            a.Encrypt(k, actual.AsSpan(10, a.NonceSize), ad, b, actual);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void EncryptWithAdOverlapping(AeadAlgorithm a)
        {
            using var k = new Key(a);
            var n = Utilities.RandomBytes.Slice(0, a.NonceSize);
            var b = Utilities.RandomBytes.Slice(0, L);

            var expected = new byte[b.Length + a.TagSize];
            var actual = new byte[b.Length + a.TagSize];
            Utilities.RandomBytes.Slice(200, actual.Length).CopyTo(actual);

            a.Encrypt(k, n, actual, b, expected);
            a.Encrypt(k, n, actual, b, actual);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void EncryptWithPlaintextOverlapping(AeadAlgorithm a)
        {
            using var k = new Key(a);
            var n = Utilities.RandomBytes.Slice(0, a.NonceSize).ToArray();
            var ad = Utilities.RandomBytes.Slice(0, 100).ToArray();
            var b = Utilities.RandomBytes.Slice(200, 200).ToArray();

            Assert.Throws<ArgumentException>("ciphertext", () => a.Encrypt(k, n, ad, b.AsSpan(10, 100), b.AsSpan(60, 100 + a.TagSize)));
            Assert.Throws<ArgumentException>("ciphertext", () => a.Encrypt(k, n, ad, b.AsSpan(60, 100), b.AsSpan(10, 100 + a.TagSize)));
        }

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void EncryptWithSpanOutOfPlace(AeadAlgorithm a)
        {
            using var k = new Key(a);
            var n = Utilities.RandomBytes.Slice(0, a.NonceSize);
            var ad = Utilities.RandomBytes.Slice(0, 100);

            var expected = new byte[L + a.TagSize];
            var actual = new byte[L + a.TagSize];

            var plaintext = Utilities.RandomBytes.Slice(0, L);

            a.Encrypt(k, n, ad, plaintext, expected);
            a.Encrypt(k, n, ad, plaintext, actual);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void EncryptWithSpanInPlace(AeadAlgorithm a)
        {
            using var k = new Key(a);
            var n = Utilities.RandomBytes.Slice(0, a.NonceSize);
            var ad = Utilities.RandomBytes.Slice(0, 100);

            var expected = new byte[L + a.TagSize];
            var actual = new byte[L + a.TagSize];
            Utilities.RandomBytes.Slice(0, L).CopyTo(actual);

            a.Encrypt(k, n, ad, actual.AsSpan(0, L), expected);
            a.Encrypt(k, n, ad, actual.AsSpan(0, L), actual);

            Assert.Equal(expected, actual);
        }

        #endregion

        #region Decrypt #1

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void DecryptWithNullKey(AeadAlgorithm a)
        {
            Assert.Throws<ArgumentNullException>("key", () => a.Decrypt(null!, Utilities.RandomBytes.Slice(0, a.NonceSize), ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty));
        }

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void DecryptWithDisposedKey(AeadAlgorithm a)
        {
            var k = new Key(a);
            k.Dispose();
            Assert.Throws<ObjectDisposedException>(() => a.Decrypt(k, Utilities.RandomBytes.Slice(0, a.NonceSize), ReadOnlySpan<byte>.Empty, new byte[a.TagSize]));
        }

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void DecryptWithWrongKey(AeadAlgorithm a)
        {
            using var k = new Key(SignatureAlgorithm.Ed25519);

            Assert.Throws<ArgumentException>("key", () => a.Decrypt(k, Utilities.RandomBytes.Slice(0, a.NonceSize), ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty));
        }

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void DecryptWithNonceTooSmall(AeadAlgorithm a)
        {
            using var k = new Key(a);

            Assert.Null(a.Decrypt(k, Utilities.RandomBytes.Slice(0, a.NonceSize - 1), ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty));
        }

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void DecryptWithNonceTooLarge(AeadAlgorithm a)
        {
            using var k = new Key(a);

            Assert.Null(a.Decrypt(k, Utilities.RandomBytes.Slice(0, a.NonceSize + 1), ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty));
        }

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void DecryptWithCiphertextTooSmall(AeadAlgorithm a)
        {
            using var k = new Key(a);

            Assert.Null(a.Decrypt(k, Utilities.RandomBytes.Slice(0, a.NonceSize), ReadOnlySpan<byte>.Empty, new byte[a.TagSize - 1]));
        }

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void DecryptEmptySuccess(AeadAlgorithm a)
        {
            using var k = new Key(a);
            var n = Utilities.RandomBytes.Slice(0, a.NonceSize);
            var ad = ReadOnlySpan<byte>.Empty;

            var ct = a.Encrypt(k, n, ad, ReadOnlySpan<byte>.Empty);
            Assert.NotNull(ct);
            Assert.Equal(a.TagSize, ct.Length);

            var pt = a.Decrypt(k, n, ad, ct);
            Assert.NotNull(pt);
            Assert.Empty(pt);
        }

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void DecryptSuccess(AeadAlgorithm a)
        {
            using var k = new Key(a);
            var n = Utilities.RandomBytes.Slice(0, a.NonceSize);
            var ad = Utilities.RandomBytes.Slice(0, 100);

            var expected = Utilities.RandomBytes.Slice(0, L).ToArray();

            var ct = a.Encrypt(k, n, ad, expected);
            Assert.NotNull(ct);
            Assert.Equal(expected.Length + a.TagSize, ct.Length);

            var pt = a.Decrypt(k, n, ad, ct);
            Assert.Equal(expected, pt);
        }

        #endregion

        #region Decrypt #2

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void DecryptWithSpanWithNullKey(AeadAlgorithm a)
        {
            Assert.Throws<ArgumentNullException>("key", () => a.Decrypt(null!, Utilities.RandomBytes.Slice(0, a.NonceSize), ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, Span<byte>.Empty));
        }

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void DecryptWithSpanWithDisposedKey(AeadAlgorithm a)
        {
            var k = new Key(a);
            k.Dispose();
            Assert.Throws<ObjectDisposedException>(() => a.Decrypt(k, Utilities.RandomBytes.Slice(0, a.NonceSize), ReadOnlySpan<byte>.Empty, new byte[a.TagSize], Span<byte>.Empty));
        }

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void DecryptWithSpanWithWrongKey(AeadAlgorithm a)
        {
            using var k = new Key(SignatureAlgorithm.Ed25519);

            Assert.Throws<ArgumentException>("key", () => a.Decrypt(k, Utilities.RandomBytes.Slice(0, a.NonceSize), ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, Span<byte>.Empty));
        }

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void DecryptWithSpanWithNonceTooSmall(AeadAlgorithm a)
        {
            using var k = new Key(a);

            Assert.False(a.Decrypt(k, Utilities.RandomBytes.Slice(0, a.NonceSize - 1), ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, Span<byte>.Empty));
        }

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void DecryptWithSpanWithNonceTooLarge(AeadAlgorithm a)
        {
            using var k = new Key(a);

            Assert.False(a.Decrypt(k, Utilities.RandomBytes.Slice(0, a.NonceSize + 1), ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, Span<byte>.Empty));
        }

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void DecryptWithSpanWithCiphertextTooSmall(AeadAlgorithm a)
        {
            using var k = new Key(a);

            Assert.False(a.Decrypt(k, Utilities.RandomBytes.Slice(0, a.NonceSize), ReadOnlySpan<byte>.Empty, new byte[a.TagSize - 1], Span<byte>.Empty));
        }

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void DecryptWithSpanTooLarge(AeadAlgorithm a)
        {
            using var k = new Key(a);
            var n = Utilities.RandomBytes.Slice(0, a.NonceSize).ToArray();

            var ct = a.Encrypt(k, n, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty);
            Assert.NotNull(ct);
            Assert.Equal(a.TagSize, ct.Length);

            Assert.Throws<ArgumentException>("plaintext", () => a.Decrypt(k, n, ReadOnlySpan<byte>.Empty, ct, new byte[1]));
        }

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void DecryptWithAdOverlappingSuccess(AeadAlgorithm a)
        {
            using var k = new Key(a);
            var n = Utilities.RandomBytes.Slice(0, a.NonceSize);
            var b = Utilities.RandomBytes.Slice(0, L);

            var expected = b.ToArray();
            var actual = Utilities.RandomBytes.Slice(200, L).ToArray();

            var ciphertext = a.Encrypt(k, n, actual.AsSpan(10, 100), expected);

            Assert.True(a.Decrypt(k, n, actual.AsSpan(10, 100), ciphertext, actual));
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void DecryptWithCiphertextOverlapping(AeadAlgorithm a)
        {
            using var k = new Key(a);
            var n = Utilities.RandomBytes.Slice(0, a.NonceSize).ToArray();
            var ad = Utilities.RandomBytes.Slice(0, 100).ToArray();
            var b = Utilities.RandomBytes.Slice(200, 200).ToArray();

            Assert.Throws<ArgumentException>("plaintext", () => a.Decrypt(k, n, ad, b.AsSpan(10, 100 + a.TagSize), b.AsSpan(60, 100)));
            Assert.Throws<ArgumentException>("plaintext", () => a.Decrypt(k, n, ad, b.AsSpan(60, 100 + a.TagSize), b.AsSpan(10, 100)));
        }

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void DecryptWithSpanSuccess(AeadAlgorithm a)
        {
            using var k = new Key(a);
            var n = Utilities.RandomBytes.Slice(0, a.NonceSize);
            var ad = Utilities.RandomBytes.Slice(0, 100);

            var expected = Utilities.RandomBytes.Slice(0, L).ToArray();
            var actual = new byte[L];

            var ciphertext = a.Encrypt(k, n, ad, expected);

            Assert.True(a.Decrypt(k, n, ad, ciphertext, actual));
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void DecryptWithSpanInPlaceSuccess(AeadAlgorithm a)
        {
            using var k = new Key(a);
            var n = Utilities.RandomBytes.Slice(0, a.NonceSize);
            var ad = Utilities.RandomBytes.Slice(0, 100);

            var actual = new byte[L + a.TagSize];
            var expected = new byte[L + a.TagSize];

            a.Encrypt(k, n, ad, Utilities.RandomBytes.Slice(0, L), actual);
            a.Encrypt(k, n, ad, Utilities.RandomBytes.Slice(0, L), expected);

            Assert.True(a.Decrypt(k, n, ad, actual, expected.AsSpan(0, L)));
            Assert.True(a.Decrypt(k, n, ad, actual, actual.AsSpan(0, L)));
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void DecryptFailClearsPlaintext(AeadAlgorithm a)
        {
            using var k = new Key(a);
            var n = Utilities.RandomBytes.Slice(0, a.NonceSize);
            var ad = Utilities.RandomBytes.Slice(0, 100);

            var actual = Utilities.RandomBytes.Slice(0, L).ToArray();

            var ciphertext = new byte[actual.Length + a.TagSize];

            Assert.False(a.Decrypt(k, n, ad, ciphertext, actual));
            Assert.Equal(Utilities.FillArray(actual.Length, actual[0]), actual);
        }

        #endregion

        #region CreateKey

        [Theory]
        [MemberData(nameof(AeadAlgorithms))]
        public static void CreateKey(AeadAlgorithm a)
        {
            using var k = new Key(a, new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextArchiving });
            Assert.Same(a, k.Algorithm);
            Assert.False(k.HasPublicKey);
            Assert.Throws<InvalidOperationException>(() => k.PublicKey);
            Assert.Equal(a.KeySize, k.Size);

            var actual = k.Export(KeyBlobFormat.RawSymmetricKey);
            var unexpected = Utilities.FillArray(actual.Length, actual[0]);

            Assert.NotEqual(unexpected, actual);
        }

        #endregion
    }
}
