#!/bin/sh
set -eu

cd "$(dirname "$0")"

pandoc \
  01-front-matter.md \
  02-introduction.md \
  03-prerequisites.md \
  --standalone \
  --syntax-highlighting=none \
  --css=book.css \
  --metadata title="Local Azure Development with Topaz" \
  --metadata lang=en-US \
  --output book.html

weasyprint book.html book.pdf