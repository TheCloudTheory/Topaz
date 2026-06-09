// Phase 1 AMQP baseline smoke test for Topaz — Go client
//
// Sends one message to a Service Bus queue and receives it back, then sends
// one event to an Event Hub and reads it back from partition 0.  Exits with
// code 0 on success, code 1 on any failure.
//
// This program documents whether the Go Azure SDK (which uses go-amqp as its
// AMQP transport) is affected by the two AMQPNetLite protocol deviations:
//   1. Trailing null fields omitted in performatives.
//   2. Error composites encoded with 2 fields instead of 3.
//
// Prerequisites:
//   - A running Topaz host and pre-created namespace + queue/hub.
//   - DNS entries for AMQP hostnames in /etc/hosts:
//       127.0.0.1  sb-test.servicebus.topaz.local.dev
//       127.0.0.1  test.eventhub.topaz.local.dev
//
// Environment variables (all optional, defaults match the standard E2E setup):
//   TOPAZ_SB_CONNECTION_STRING   — Service Bus AMQP connection string
//   TOPAZ_SB_QUEUE               — queue name (default: queue-test)
//   TOPAZ_EH_CONNECTION_STRING   — Event Hub AMQP connection string
//   TOPAZ_EH_NAME                — event hub name (default: test)
//   TOPAZ_SMOKE_MODE             — "servicebus", "eventhub", or "all" (default: "all")
//
// Run:
//   go run .
//   TOPAZ_SMOKE_MODE=servicebus go run .
package main

import (
	"context"
	"fmt"
	"os"
	"time"

	"github.com/Azure/azure-sdk-for-go/sdk/messaging/azeventhubs"
	"github.com/Azure/azure-sdk-for-go/sdk/messaging/azservicebus"
)

func main() {
	mode := getEnv("TOPAZ_SMOKE_MODE", "all")

	var sbErr, ehErr error
	if mode == "servicebus" || mode == "all" {
		sbErr = runServiceBusSmoke()
	}
	if mode == "eventhub" || mode == "all" {
		ehErr = runEventHubSmoke()
	}

	if sbErr != nil || ehErr != nil {
		os.Exit(1)
	}
}

// ---------------------------------------------------------------------------
// Service Bus smoke
// ---------------------------------------------------------------------------

func runServiceBusSmoke() error {
	connStr := getEnv(
		"TOPAZ_SB_CONNECTION_STRING",
		"Endpoint=sb://sb-test.servicebus.topaz.local.dev:8889;"+
			"SharedAccessKeyName=RootManageSharedAccessKey;"+
			"SharedAccessKey=SAS_KEY_VALUE;"+
			"UseDevelopmentEmulator=true;",
	)
	queueName := getEnv("TOPAZ_SB_QUEUE", "queue-test")

	client, err := azservicebus.NewClientFromConnectionString(connStr, nil)
	if err != nil {
		return fail("servicebus: create client: %v", err)
	}
	defer client.Close(context.Background())

	sender, err := client.NewSender(queueName, nil)
	if err != nil {
		return fail("servicebus: create sender: %v", err)
	}
	defer sender.Close(context.Background())

	if err = sender.SendMessage(context.Background(), &azservicebus.Message{
		Body: []byte("smoke-test"),
	}, nil); err != nil {
		return fail("servicebus: send: %v", err)
	}
	fmt.Println("  [servicebus] -> message sent")

	receiver, err := client.NewReceiverForQueue(queueName, nil)
	if err != nil {
		return fail("servicebus: create receiver: %v", err)
	}
	defer receiver.Close(context.Background())

	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	messages, err := receiver.ReceiveMessages(ctx, 1, nil)
	if err != nil {
		return fail("servicebus: receive: %v", err)
	}
	if len(messages) == 0 {
		return fail("servicebus: no messages received within timeout")
	}

	body := string(messages[0].Body)
	if body != "smoke-test" {
		return fail("servicebus: expected 'smoke-test' got '%s'", body)
	}

	if err = receiver.CompleteMessage(context.Background(), messages[0], nil); err != nil {
		return fail("servicebus: complete: %v", err)
	}

	fmt.Println("PASS: Service Bus AMQP smoke test succeeded")
	return nil
}

// ---------------------------------------------------------------------------
// Event Hub smoke
// ---------------------------------------------------------------------------

func runEventHubSmoke() error {
	connStr := getEnv(
		"TOPAZ_EH_CONNECTION_STRING",
		"Endpoint=sb://test.eventhub.topaz.local.dev:8888;"+
			"SharedAccessKeyName=RootManageSharedAccessKey;"+
			"SharedAccessKey=SAS_KEY_VALUE;"+
			"UseDevelopmentEmulator=true;",
	)
	hubName := getEnv("TOPAZ_EH_NAME", "test")

	producer, err := azeventhubs.NewProducerClientFromConnectionString(connStr, hubName, nil)
	if err != nil {
		return fail("eventhub: create producer: %v", err)
	}
	defer producer.Close(context.Background())

	consumer, err := azeventhubs.NewConsumerClientFromConnectionString(connStr, hubName, "$Default", nil)
	if err != nil {
		return fail("eventhub: create consumer: %v", err)
	}
	defer consumer.Close(context.Background())

	// Record end-of-stream before sending so we only read the new event.
	partProps, err := consumer.GetPartitionProperties(context.Background(), "0", nil)
	if err != nil {
		return fail("eventhub: get partition properties: %v", err)
	}

	var startPos azeventhubs.StartPosition
	if partProps.IsEmpty {
		startPos = azeventhubs.StartPosition{Earliest: toPtr(true)}
	} else {
		startPos = azeventhubs.StartPosition{SequenceNumber: &partProps.LastEnqueuedSequenceNumber}
	}

	batch, err := producer.NewEventDataBatch(context.Background(), &azeventhubs.EventDataBatchOptions{
		PartitionID: toPtr("0"),
	})
	if err != nil {
		return fail("eventhub: create batch: %v", err)
	}
	if err = batch.AddEventData(&azeventhubs.EventData{Body: []byte("smoke-test")}, nil); err != nil {
		return fail("eventhub: add event: %v", err)
	}
	if err = producer.SendEventDataBatch(context.Background(), batch, nil); err != nil {
		return fail("eventhub: send: %v", err)
	}
	fmt.Println("  [eventhub] -> event sent")

	partClient, err := consumer.NewPartitionClient("0", &azeventhubs.PartitionClientOptions{
		StartPosition: startPos,
	})
	if err != nil {
		return fail("eventhub: create partition client: %v", err)
	}
	defer partClient.Close(context.Background())

	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	events, err := partClient.ReceiveEvents(ctx, 1, nil)
	if err != nil {
		return fail("eventhub: receive: %v", err)
	}
	if len(events) == 0 {
		return fail("eventhub: no events received within timeout")
	}

	body := string(events[0].Body)
	if body != "smoke-test" {
		return fail("eventhub: expected 'smoke-test' got '%s'", body)
	}

	fmt.Println("PASS: Event Hub AMQP smoke test succeeded")
	return nil
}

// ---------------------------------------------------------------------------
// helpers
// ---------------------------------------------------------------------------

func getEnv(key, defaultValue string) string {
	if v, ok := os.LookupEnv(key); ok {
		return v
	}
	return defaultValue
}

func fail(format string, args ...any) error {
	err := fmt.Errorf(format, args...)
	fmt.Fprintf(os.Stderr, "FAIL: %v\n", err)
	return err
}

func toPtr[T any](v T) *T { return &v }
