# NeoBeard.Crypto Changelog

## 0.0.0-alpha.2

- Replace Pbkdf2Hash with HashType, matching Go implementation IDs.
- Replace AesEncryptionProvider, AesEncryptionHeader, and AesEncryptionProviderOptions
  with new AesCbcEncryptionProvider that uses Span<byte> and BinaryPrimitives.
- Binary format is now compatible with Go aescbc implementation.
- Support for SHA224, SHA3-224, and BLAKE2B hash algorithms for HMAC and PBKDF2.
- Add metadata support to AesCbcEncryptionProvider.

## 0.0.0-alpha.0

- Aes 256 CBC Encryption Provider which encrypts then MACS.
- ChaCha20 SymmetricAlgorithm
- Blake 2b
- Salsa20 SymmetricAlgorithm

## 0.0.0-alpha.1

- Add Pbkdf2Hash which acts as a enum for SHA names and HMACs.
- Add a kdf to AesGcm and additional metadata so that the provider can
  decrypt so long as the key is the same.
- Rename Aes256EncryptionProvider to AesEncryptionProvider, move to using Spans
  and BinaryPrimitive methods rather than using the Binary reader.
- Add tests for AesGcm and Aes Encryption providers.
