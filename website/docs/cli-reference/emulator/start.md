---
sidebar_position: 1
---

# start

Starts the emulator.

## Options
* `-l|--log-level` - Sets the log level. Available values are: Debug, Information, Warning, Error
* `--tenant-id` - Configures the tenant ID used when providing metadata endpoints
* `--certificate-file` - Allows you to bring your own certificate (BYOC). Must be an RFC 7468 PEM-encoded certificate.
* `--certificate-key` - Allows you to bring your own certificate (BYOC)
* `--skip-dns-registration` - Allows you to skip DNS entries registration in the `hosts` file so you can run Topaz without elevated permissions

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