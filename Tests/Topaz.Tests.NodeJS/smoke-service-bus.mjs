/**
 * Node.js AMQP smoke test — Service Bus
 *
 * Sends one message to a queue and receives it back.
 * Exits 0 on success, 1 on any failure.
 *
 * Phase 1 AMQP baseline: run against AMQPNetLite 2.5.1 to document
 * whether the Node.js @azure/service-bus client (which uses rhea, not
 * pyamqp) is affected by the trailing-null-field and 2-field Error
 * deviations.
 *
 * Prerequisites:
 *   1. Topaz host running (dotnet run --project Topaz.Host)
 *   2. DNS entry:
 *      echo "127.0.0.1 <NAMESPACE>.servicebus.topaz.local.dev" >> /etc/hosts
 *   3. npm install
 *
 * Environment variables (all have defaults for the standard E2E setup):
 *   TOPAZ_SB_CONNECTION_STRING  — full AMQP connection string
 *   TOPAZ_SB_QUEUE              — queue name (default: queue-test)
 */

import { ServiceBusClient } from "@azure/service-bus";

const CONNECTION_STRING =
  process.env.TOPAZ_SB_CONNECTION_STRING ??
  "Endpoint=sb://sb-test.servicebus.topaz.local.dev:8889;" +
    "SharedAccessKeyName=RootManageSharedAccessKey;" +
    "SharedAccessKey=SAS_KEY_VALUE;" +
    "UseDevelopmentEmulator=true;";

const QUEUE_NAME = process.env.TOPAZ_SB_QUEUE ?? "queue-test";

const client = new ServiceBusClient(CONNECTION_STRING);
const sender = client.createSender(QUEUE_NAME);
const receiver = client.createReceiver(QUEUE_NAME);

try {
  await sender.sendMessages({ body: "smoke-test" });
  console.log("  -> message sent");

  const messages = await receiver.receiveMessages(1, { maxWaitTimeInMs: 8000 });

  if (messages.length === 0) {
    console.error("FAIL: no messages received within timeout");
    process.exit(1);
  }

  const body = messages[0].body;
  if (body !== "smoke-test") {
    console.error(`FAIL: expected "smoke-test" but received "${body}"`);
    process.exit(1);
  }

  await receiver.completeMessage(messages[0]);
  console.log("PASS: Service Bus AMQP smoke test succeeded");
} catch (err) {
  console.error(`FAIL: ${err.message}`);
  console.error(err);
  process.exit(1);
} finally {
  await sender.close();
  await receiver.close();
  await client.close();
}
