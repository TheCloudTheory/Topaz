/**
 * Node.js AMQP smoke test — Event Hub
 *
 * Sends one event to a hub and reads it back from partition 0.
 * Exits 0 on success, 1 on any failure.
 *
 * Phase 1 AMQP baseline: run against AMQPNetLite 2.5.1 to document
 * whether @azure/event-hubs (rhea transport) handles shortened
 * performatives and 2-field Error composites cleanly.
 *
 * Prerequisites:
 *   1. Topaz host running
 *   2. Event Hub namespace and hub already created (e.g. via topaz CLI)
 *   3. DNS entry:
 *      echo "127.0.0.1 <NAMESPACE>.eventhub.topaz.local.dev" >> /etc/hosts
 *   4. npm install
 *
 * Environment variables:
 *   TOPAZ_EH_CONNECTION_STRING  — full AMQP connection string
 *   TOPAZ_EH_NAME               — event hub name (default: test)
 */

import { EventHubProducerClient, EventHubConsumerClient } from "@azure/event-hubs";

const CONNECTION_STRING =
  process.env.TOPAZ_EH_CONNECTION_STRING ??
  "Endpoint=sb://test.eventhub.topaz.local.dev:8888;" +
    "SharedAccessKeyName=RootManageSharedAccessKey;" +
    "SharedAccessKey=SAS_KEY_VALUE;" +
    "UseDevelopmentEmulator=true;";

const HUB_NAME = process.env.TOPAZ_EH_NAME ?? "test";
const CONSUMER_GROUP = "$Default";

const producer = new EventHubProducerClient(CONNECTION_STRING, HUB_NAME);
const consumer = new EventHubConsumerClient(CONSUMER_GROUP, CONNECTION_STRING, HUB_NAME);

try {
  // Record current end-of-stream position on partition 0 before sending.
  const partitionProps = await consumer.getPartitionProperties("0");
  
  // Start from the last enqueued sequence number, or offset 0 if partition is empty
  const startingPosition = partitionProps.isEmpty
    ? { offset: "0" }  // Use offset instead of earliest
    : { sequenceNumber: partitionProps.lastEnqueuedSequenceNumber };

  const batch = await producer.createBatch({ partitionId: "0" });
  batch.tryAdd({ body: "smoke-test" });
  await producer.sendBatch(batch);
  console.log("  -> event sent");

  const received = [];
  const subscription = consumer.subscribe(
    "0",
    {
      processEvents: async (events) => {
        for (const event of events) {
          received.push(event.body);
        }
      },
      processError: async (err) => {
        console.error(`FAIL (processError): ${err.message}`);
        process.exit(1);
      },
    },
    { startPosition: startingPosition }
  );

  // Give the consumer up to 8 s to receive the event.
  await new Promise((resolve) => setTimeout(resolve, 8000));
  await subscription.close();

  if (received.length === 0) {
    console.error("FAIL: no events received within timeout");
    process.exit(1);
  }

  const body = received[0];
  if (body !== "smoke-test") {
    console.error(`FAIL: expected "smoke-test" but received "${body}"`);
    process.exit(1);
  }

  console.log("PASS: Event Hub AMQP smoke test succeeded");
} catch (err) {
  console.error(`FAIL: ${err.message}`);
  console.error(err);
  process.exit(1);
} finally {
  await producer.close();
  await consumer.close();
}
