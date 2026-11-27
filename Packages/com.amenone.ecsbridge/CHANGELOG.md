# Changelog

## [0.3.0] - 2026-11-27
### Breaking changes
- PublishSystem: OnPublisherInjected() is now abstract, so every publish system states how it treats the last published values when the publisher is injected. A system that did not override it no longer compiles; override it with an empty body and a one line comment to keep the 0.2.x behavior
### Changes
- README: OnPublisherInjected lists four choices (publish the current value again, treat a value as published, keep the last published values, no last published values), and how to upgrade from 0.2.x
- README: PublishSystem lists in one place that OnUpdate is sealed in the base and PublishChanges() is the entry point, that the base Construct takes the injection (no [Inject] method or constructor in the derived system), and that PublishChanges() is not called before the publisher is injected
- README: a struct message through IBufferedPublisher works, with the caution that IBufferedSubscriber hands default on subscribe (use a class or tell default apart in the subscriber), instead of reading as a recommendation to use a class
- README: the first example calls RequireForUpdate for the singleton it reads. MultiplePublishers sample README: points at the right section for the verification project
- README: a publisher bundle class can be written with a constructor generating source generator. The package does not depend on one
- README: when OnPublisherInjected keeps the last published values, write an empty body and a one line comment so that the intent reads apart from a system that has no last published values

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
