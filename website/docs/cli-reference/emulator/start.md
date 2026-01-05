---
sidebar_position: 1
---

# start
Starts the emulator.

## Options
* `-l, --log-level` - Sets the log level. Available values are: Debug, Information, Warning, Error
* `--tenant-id` - Configures the tenant ID used when providing metadata endpoints
* `--certificate-file` - Allows you to bring your own certificate (BYOC). Must be an RFC 7468 PEM-encoded certificate.
* `--certificate-key` - Allows you to bring your own certificate (BYOC).
* `--enable-logging-to-file` - Tells the emulator to save logs to a file.
* `--refresh-log` - Clears the logs file upon starting the emulator.
* `--default-subscription` - Creates a default subscription with the provided subscription ID
* `--emulator-ip-address` - Defines the IP address used by the emulator to listen to incoming requests. Not that this address is only relevant if running the emulator directly on a host machine.

## Examples

### Start the emulator with default settings
```bash
$ topaz start
```

### Start the emulator maximum verbosity
```bash
$ topaz start --log-level Debug
```

### Start the emulator with your own certificates
```bash
$ topaz start --certificate-file "topaz.crt" --certificate-key "topaz.key"
```
