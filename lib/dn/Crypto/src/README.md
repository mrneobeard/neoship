# NeoBeard.Crypto

## Overview

Cryptography related functionality such as

- Aes 256 CBC Encryption Provider which encrypts then MACS.
- ChaCha20 SymmetricAlgorithm
- Blake 2b
- Salsa20 SymmetricAlgorithm

## Usage

```csharp
using NeoBeard.Crypto;
using NeoBeard.Extras

var options = new Aes256EncryptionProviderOptions
{
    Key = System.Text.Encoding.UTF8NoBom.GetBytes("unsafe password")
};
var aes = new Aes256EncryptionProvider(options);

var encrypted = aes.Encrypt("my data");
Console.WriteLine(encrypted);

var decrypted = aes.Decrypt(encrypted);
Console.WriteLine(decrypted);

```
