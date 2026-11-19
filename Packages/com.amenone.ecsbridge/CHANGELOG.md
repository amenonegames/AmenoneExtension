# Changelog

## [0.1.0] - 2026-11-17
### Changes
- Initial release
- DefaultWorldSingleton / DefaultWorldSingletonBuffer / DefaultWorldQuery: follow the recreated default world, throw InvalidOperationException when the default world is null or disposed
- DefaultWorldReset: recreate the default world
- WorldClockDriver: drive the default world time from IWorldDeltaTimeSource
- PublishSystem: publish system base class that waits for the publisher injection
- LastPublished / LastPublishedValue: change detection helpers
- Minimal sample (counter, MVP -> ECS -> MVP)
