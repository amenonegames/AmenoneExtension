# Changelog

## [0.1.0] - 2023-12-08
### Changes
- Initial release


## [0.1.1] - 2023-12-09
### Changes
- RegisterComponentWithoutSelf release.
- add asSelf and asImplementedInterfaces arg to UtilMethods
- change namespace to VcontainerViewExtensions


## [0.2.0] - 2026-11-27
### Changes
- Breaking: the null object source generator (NullObjectGeneratorForNewerThan2022.2.dll) is no longer bundled. It moved to its own package, com.amenone.nullobjectgenerator (Unity 6 or later). Install that package separately to keep using [InterfaceToNullObj] / [InheritsToNullObj] and the generated *AsNullObj types, for example with RegisterComponentOrNullObFromArray.
- Breaking: the register sample moved from Sample to Samples~/Sample, so it is no longer compiled with the package. Import it from the Package Manager window (Samples). The sample uses IFooAsNullObj, so it needs com.amenone.nullobjectgenerator installed.
