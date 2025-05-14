#!/bin/bash

./generate.sh

sudo cp localhost.crt /usr/local/share/ca-certificates/
sudo update-ca-certificates