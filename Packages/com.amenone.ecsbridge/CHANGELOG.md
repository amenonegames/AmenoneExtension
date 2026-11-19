# Changelog

## [0.2.0] - 2026-11-19
### Changes
- LastPublishedValue: add TreatAsPublished(value) to treat a value as already published without publishing it
- LastPublished: add TreatEveryKeyAsPublished(value) to treat every key, including keys not seen yet, as already published with the value. Clear() also forgets this value
- LastPublishedValue: add TryGetPublished(out value) to read the last published value
- LastPublished: add TryGetPublished(key, out value) (returns the TreatEveryKeyAsPublished value for keys not seen yet) and TreatAsPublished(key, value), for systems that decide whether to publish from the last published value itself
- README: the change detection helpers are optional, and IBufferedSubscriber hands default to new subscribers when the message is a struct
- PublishSystem: a system that publishes several kinds of messages takes a publisher bundle class as TPublisher (README). Tests for the bundle injected by VContainer
- Minimal sample README: explain the choice made in OnPublisherInjected (Clear)
- MultiplePublishers sample (one publish system, two kinds of messages through a publisher bundle)

## [0.1.0] - 2026-11-17
### Changes
- Initial release
- DefaultWorldSingleton / DefaultWorldSingletonBuffer / DefaultWorldQuery: follow the recreated default world, throw InvalidOperationException when the default world is null or disposed
- DefaultWorldReset: recreate the default world
- WorldClockDriver: drive the default world time from IWorldDeltaTimeSource
- PublishSystem: publish system base class that waits for the publisher injection
- LastPublished / LastPublishedValue: change detection helpers
- Minimal sample (counter, MVP -> ECS -> MVP)
