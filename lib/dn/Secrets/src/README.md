# NeoBeard.Secrets

## Overview

Generates cryptographically secure passwords/secrets
and provides a secret masker.

## Usage

```csharp
using NeoBeard.Secrets;

var sg = new SecretGenerator().AddDefaults();
var pw = sg.Generate(16);
var ss = sg.GenerateAsSecretString(16);
var pwb = sg.GenerateAsBytes();

sg = new SecretGenerator().Add(SecretCharacterSets.Digits);
sg.SetValidator(_ => true);

Console.WriteLine(sg.Generate(4)); // new pin

var masker = new SecretMasker();
masker.Add(pw);

Console.WriteLine(masker.Mask($"My password is {pw}")); // My password is *****

```
