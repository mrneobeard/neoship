# NeoBeard.Core Changelog

## 0.0.0-alpha.0

Initial feature set:

- Result type
- Extension methods and members under the Extras namespaces for strings,
  spans, string builder, arrays, tasks, etc.
- The static FileSystem class which provides an fs module similar
  to other std libraries including functions missing posix functions
  like copy, chown, chmod, stat, etc.
- Enhanced logic for working with environments such as expanding
  bash style variables, appending/prepending paths to the environment
  path, etc.

## 0.0.0-alpha.1

- Add commonly used Generic types that inherit from Dictionary, List, and Dictionary.
  - `Map<TKey, TValue>`, `Map<TValue>`, and `Map` which is `Dictionary<string, object?>`
  - Add overloads for key,value tuples to enable `new Map([(key1, value1), (key2, value2)]);`
  - Add StringList which is a common list type along with a ContainsFold and IndexOfFolder methods.
  - Add OrderedMaps.
  