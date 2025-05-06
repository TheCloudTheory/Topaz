# azure-local
Local Azure environment emulation for development

## Differences

The differences between the emulator and the actual service.

### Table Service
* `Update` and `Merge` operations are performed in the same way, i.e. emulator always replaces an entity, even if `Merge` is requested