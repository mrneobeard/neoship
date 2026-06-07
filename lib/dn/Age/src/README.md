# NeoBeard.Age

`NeoBeard.Age` provides an idiomatic .NET implementation of the age v1 file format for stream and file encryption.

Supported recipients:

- Native age X25519 public/private keys.
- SSH RSA public/private keys using age's `ssh-rsa` recipient stanza format.
- SSH Ed25519 public/private keys using age's `ssh-ed25519` recipient stanza format.

```csharp
using NeoBeard.Age;

var key = AgeKey.Create();
using var input = File.OpenRead("plain.txt");
using var encrypted = File.Create("plain.txt.age");
AgeFile.Encrypt(input, encrypted, [AgeRecipient.FromPublicKey(key.PublicKey)]);

using var cipher = File.OpenRead("plain.txt.age");
using var output = File.Create("plain.txt.out");
AgeFile.Decrypt(cipher, output, [AgeIdentity.FromPrivateKey(key.PrivateKey)]);
```

## Attribution

This library implements the age v1 format described by the age project at <https://github.com/FiloSottile/age> and the C2SP specification at <https://age-encryption.org/v1>.

OpenSSH private-key parsing and SSH key wire-format handling were informed by SSH.NET (<https://github.com/sshnet/SSH.NET>). Ed25519 public key derivation uses Bouncy Castle for C# (<https://github.com/bcgit/bc-csharp>).
