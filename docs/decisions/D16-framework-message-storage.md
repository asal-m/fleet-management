# D16 — framework message storage with messaging none

Status: unresolved challenge interpretation; verified framework behavior.
Framework/template/CLI: MP Core 0.9.3. Manifest: messaging none.

The challenge excludes product Outbox/Inbox, but the generated MP Core execution
foundation registers durable local message storage even without an external broker.
The installed 0.9.3 package XML requires PersistenceConnectionString and describes
UseMPCoreWolverine as the durable PostgreSQL foundation. It exposes no opt-out flag.
The [official foundation source](https://github.com/panahister/mpcore/blob/main/src/MPCore.Messaging.Wolverine/WolverineFoundation.cs)
unconditionally calls PersistMessagesWithPostgresql and UseDurableLocalQueues.
This agrees with the generated skill's messaging-none guidance; that choice excludes
external brokers, not framework storage.

The product calls handlers inline through InvokeAsync. It implements no product
message publisher, consumer, Outbox or Inbox workflow. No Kafka/RabbitMQ connection,
package or service is added. Unused broker placeholders were removed from appsettings.
Keeping the MP Core foundation preserves transaction ownership, failed-result
rollback, audit commit behavior and the current-actor/tenant middleware.

Rejected changes: deleting existing Wolverine tables, replacing the message store
through DI internals, disabling transactions, or patching MP Core package source.
None is an officially documented 0.9.3 opt-out, and each can invalidate guarantees.

Acceptance requires the evaluator to confirm that unused framework-owned storage
is permitted. If the exclusion also forbids those tables, an officially supported
MP Core configuration/version is needed. This document is evidence and a concrete
decision record; it does not substitute for that confirmation.
