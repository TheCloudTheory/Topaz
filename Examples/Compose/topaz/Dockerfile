FROM --platform=linux/amd64 thecloudtheory/topaz-host:latest

# Copy the cert files directly into the image, mirroring how the test fixtures
# inject them via WithResourceMapping. This avoids bind-mount permission issues
# with the chiseled base image on macOS / external drives.
COPY topaz.crt /app/topaz.crt
COPY topaz.key /app/topaz.key
